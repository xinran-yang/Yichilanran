using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
namespace YiChiLanRan
{
    public sealed class LevelRuntime : MonoBehaviour
    {
        public LevelDefinition definition;
        public List<PhaseObject> Objects=new List<PhaseObject>();
        public List<TransferZone2D> Zones=new List<TransferZone2D>();
        public PlayerController2D Player;
        public Transform Goal;
        public Camera ViewCamera;
        public bool Mirrored {get;internal set;}
        public int Transfers {get;internal set;}
        public float Clock {get;private set;}
        public bool Busy {get;internal set;}
        public bool Complete {get;private set;}
        public bool Harness;
        public string LastEvent="Ready",Failure="";
        public List<string> EventHistory=new List<string>();
        public float Fade {get;private set;}
        public WorldMirrorSystem Mirror {get;private set;}
        public bool MirrorCommittedThisTick {get;internal set;}
        public bool logMirrorTransactions=true;
        public List<MirrorBoxAudit> LastMirrorAudit=new List<MirrorBoxAudit>();
        readonly List<TransferBox2D> boxes=new List<TransferBox2D>();
        readonly Dictionary<PhaseObject,Vector2> startPositions=new Dictionary<PhaseObject,Vector2>();
        readonly Dictionary<PhaseObject,Phase> startPhases=new Dictionary<PhaseObject,Phase>();
        readonly Dictionary<PhaseObject,Vector2> startOffsets=new Dictionary<PhaseObject,Vector2>();
        readonly Dictionary<PhaseObject,bool> startSimulated=new Dictionary<PhaseObject,bool>();
        Vector2[] startZones;
        Vector2 startGoal;
        Vector3 startGoalScale;
        readonly Dictionary<Transform,Vector3> startVisualPositions=new Dictionary<Transform,Vector3>();
        readonly Dictionary<Transform,Quaternion> startVisualRotations=new Dictionary<Transform,Quaternion>();
        public void Initialize(bool harness=false)
        {
            Harness=harness; Objects=new List<PhaseObject>(GetComponentsInChildren<PhaseObject>(true));
            Zones=new List<TransferZone2D>(GetComponentsInChildren<TransferZone2D>());
            Player=GetComponentInChildren<PlayerController2D>();
            boxes.Clear(); boxes.AddRange(GetComponentsInChildren<TransferBox2D>());
            Mirror=new WorldMirrorSystem(this); PhaseCollisionSystem.Refresh(this);
            foreach(var o in Objects) o.SetPhase(o.phase);
            if(Player) Player.ExternalInput=harness || definition.mirrorInspection;
            if(definition.mirrorInspection) Player.Entity.Body.simulated=false;
            startPositions.Clear(); startPhases.Clear(); startOffsets.Clear(); startSimulated.Clear();
            foreach(var o in Objects) { startPositions[o]=o.Position; startPhases[o]=o.phase; startOffsets[o]=o.Shape.offset; if(o.Body) startSimulated[o]=o.Body.simulated; }
            startZones=new Vector2[Zones.Count]; for(int i=0;i<Zones.Count;i++) startZones[i]=Zones[i].transform.position;
            if(Goal) { startGoal=Goal.position; startGoalScale=Goal.localScale; }
            startVisualPositions.Clear(); startVisualRotations.Clear();
            foreach(var o in Objects) if(o.kind==ObjectKind.Terrain || o.kind==ObjectKind.BoxBarrier) foreach(Transform child in o.transform)
            { startVisualPositions[child]=child.localPosition; startVisualRotations[child]=child.localRotation; }
            foreach(var box in boxes) { box.CaptureReferenceSupport(this); box.ApplyPhaseState(this); }
            PhaseCollisionSystem.Refresh(this); ApplyRealmVisuals();
        }
        public void ApplyRealmVisuals() { var renderer=GetComponent<RealmVisuals>(); if(renderer) renderer.Apply(); }
        public void HarnessLock()
        {
            if(!Harness) throw new System.InvalidOperationException("Harness only");
            Busy=true; SetSimulation(false);
        }
        public void ResetLevel()
        {
            StopAllCoroutines(); Mirror.CancelForReset();
            foreach(var o in Objects)
            {
                o.gameObject.SetActive(true); o.Shape.offset=startOffsets[o]; o.Place(startPositions[o]); o.SetPhase(startPhases[o]);
                if(o.Body) { o.Body.simulated=startSimulated[o]; o.Body.constraints=RigidbodyConstraints2D.FreezeRotation | (o.kind==ObjectKind.Box ? RigidbodyConstraints2D.FreezePositionX : RigidbodyConstraints2D.None); }
            }
            for(int i=0;i<Zones.Count;i++) Zones[i].transform.position=startZones[i];
            if(Goal) { Goal.position=startGoal; Goal.localScale=startGoalScale; }
            foreach(var pair in startVisualPositions) if(pair.Key) { pair.Key.localPosition=pair.Value; pair.Key.localRotation=startVisualRotations[pair.Key]; }
            MirrorCommittedThisTick=false; LastMirrorAudit.Clear();
            Clock=0; Mirrored=false; Transfers=0; Busy=false; Complete=false; Fade=0;
            ApplyRealmVisuals();
            LastEvent="Reset"; Failure=""; EventHistory.Clear(); Player.ResetController();
            foreach(var box in boxes) { box.referenceSupportId=""; box.referenceSupportSide=1; box.CaptureReferenceSupport(this); box.ApplyPhaseState(this); }
            PhaseCollisionSystem.Refresh(this); Physics2D.SyncTransforms();
        }
        void Start() { if(!Harness) Initialize(); }
        void Update()
        {
            if(Harness) return;
            if(Keyboard.current!=null && Keyboard.current.f3Key.wasPressedThisFrame) Mirror.DumpLastAudit();
            if(Keyboard.current!=null && Keyboard.current.rKey.wasPressedThisFrame) { LevelReset.Restart(this); return; }
            if(!Busy&&!Complete) Player.ReadInput();
            FitCamera();
        }
        void FixedUpdate() { if(!Harness) Tick(Time.fixedDeltaTime); }
        public void Tick(float dt)
        {
            if(Busy||Complete) return;
            MirrorCommittedThisTick=false; Clock+=dt; Player.Tick(this,dt);
            if(Busy || MirrorCommittedThisTick) return;
            foreach(var box in boxes) box.Tick(this,dt);
            if(GoalTrigger.Reached(this))
            {
                Complete=true; LastEvent="Level complete";
                if(!Harness) StartCoroutine(Finish());
            }
            if(!Harness)
                foreach(var o in Objects)
                    if(o.Body && (o.Bounds.yMax<definition.bounds.yMin-.5f || o.Bounds.xMax<-.5f || o.Bounds.xMin>24.5f)) { LevelReset.Restart(this); break; }
        }
        public PhaseObject Support(PhaseObject actor)
        {
            Rect r=actor.Bounds;
            foreach(var o in Objects)
            {
                if(o==actor || !o.gameObject.activeSelf || !PhaseCollisionSystem.Collides(actor,o)) continue;
                Rect s=o.Bounds;
                if(r.xMax>s.xMin+.035f && r.xMin<s.xMax-.035f && r.yMin>=s.yMax-.025f && r.yMin<=s.yMax+.055f) return o;
            }
            return null;
        }
        // Resolve only tiny contact seams (<= 4 cm), never gameplay-height steps or gaps.
        public void TrySmallStep(PhaseObject actor,float direction,float dt)
        {
            var r=actor.Bounds;
            foreach(var o in Objects)
            {
                if(o==actor || !o.gameObject.activeSelf || !PhaseCollisionSystem.Collides(actor,o)) continue;
                var b=o.Bounds; float rise=b.yMax-r.yMin;
                bool ahead=direction>0 ? r.xMax<=b.xMin+.06f && r.xMax+Rules.Speed*dt+.03f>=b.xMin : r.xMin>=b.xMax-.06f && r.xMin-Rules.Speed*dt-.03f<=b.xMax;
                if(!ahead || rise<-.015f || rise>.04f) continue;
                Vector2 next=actor.Position+Vector2.up*Mathf.Max(.02f,rise+.02f);
                var test=Rules.RectAt(next,actor.Shape.size); bool clear=true;
                foreach(var other in Objects)
                    if(other!=actor && other.gameObject.activeSelf && PhaseCollisionSystem.Collides(actor,other) && Rules.Overlap(test,other.Bounds)) { clear=false; break; }
                if(clear) { actor.Place(next); Physics2D.SyncTransforms(); }
                return;
            }
        }
        public void ResolveDive(float dt)
        {
            if(!Player.Diving || Player.Entity.Body.linearVelocity.y>0) return;
            var p=Player.Entity; Rect r=p.Bounds;
            float distance=Mathf.Max(.06f,-p.Body.linearVelocity.y*dt+.045f);
            PhaseObject hit=null; float nearest=float.MaxValue;
            foreach(var o in Objects)
            {
                if(o==p || !o.gameObject.activeSelf || !PhaseCollisionSystem.Collides(p,o)) continue;
                Rect b=o.Bounds; float gap=r.yMin-b.yMax;
                if(r.xMax>b.xMin+.015f && r.xMin<b.xMax-.015f && gap>=-.035f && gap<=distance && gap<nearest) {hit=o; nearest=gap;}
            }
            if(hit!=null && hit.kind==ObjectKind.Box)
            {
                foreach(var zone in Zones)
                    if(zone.Accepts(hit)) { Mirror.TryTransfer(zone,hit); return; }
            }
            TransferZone2D solo=null; float soloGap=float.MaxValue;
            foreach(var zone in Zones)
            {
                float gap=r.yMin-zone.Floor;
                if(zone.data.allowSolo && zone.Contains(r) && gap>=-.035f && gap<=distance && gap<soloGap && gap<=nearest+.04f)
                { solo=zone; soloGap=gap; }
            }
            if(solo!=null) { Mirror.TryTransfer(solo,null); return; }
            if(hit==null) return;
            if(hit.elastic)
            {
                if(nearest<=.025f)
                {
                    if(Rules.EnableDiveBounce) Player.Bounce(Clock);
                    else Player.EndDive();
                }
            }
            else if(hit.breakable && hit.kind==ObjectKind.Terrain)
            { hit.gameObject.SetActive(false); Player.EndDive(); LastEvent="Brittle crust broken"; }
            else Player.EndDive();
        }
        public bool Transition(WorldMirrorSystem.Plan plan)
        {
            if(!Mirror.Begin(plan)) return false;
            if(Harness || !Application.isPlaying)
            {
                if(!Mirror.Commit(plan)) return false;
                return Mirror.Finish(plan);
            }
            StartCoroutine(Animate(plan)); return true;
        }
        IEnumerator Animate(WorldMirrorSystem.Plan plan)
        {
            for(float t=0;t<.13f;t+=Time.unscaledDeltaTime) { Fade=t/.13f; yield return null; }
            if(!Mirror.Commit(plan)) { Fade=0; yield break; }
            for(float t=.16f;t>0;t-=Time.unscaledDeltaTime) { Fade=t/.16f; yield return null; }
            Fade=0; Mirror.Finish(plan);
        }
        void SetSimulation(bool on) { foreach(var o in Objects) if(o.Body) o.Body.simulated=on; }
        IEnumerator Finish()
        {
            SetSimulation(false);
            if(string.IsNullOrEmpty(definition.nextScene)) yield break;
            yield return new WaitForSecondsRealtime(.35f);
            for(float t=0;t<.25f;t+=Time.unscaledDeltaTime) { Fade=t/.25f; yield return null; }
            Fade=1;
            SceneManager.LoadScene(definition.nextScene);
        }
        public static Rect ViewportFor(int width,int height)
        {
            float aspect=(float)width/Mathf.Max(1,height), target=16f/9f;
            return aspect>=target ? new Rect((1-target/aspect)/2,0,target/aspect,1) : new Rect(0,(1-aspect/target)/2,1,aspect/target);
        }
        public void FitCamera()
        {
            if(!ViewCamera || Screen.height==0) return;
            ViewCamera.rect=ViewportFor(Screen.width,Screen.height);
            ViewCamera.orthographicSize=7.6f;
        }
    }
}









