using System.Collections.Generic;
using UnityEngine;
using YiChiLanRan.Configuration;

namespace YiChiLanRan
{
    public sealed class ChapterWorld : MonoBehaviour
    {
        public ClothPlayer player;
        public Camera gameCamera;
        public TerrainPiece[] terrain;
        public CrossingPoint[] gates;
        public bool Mirrored { get; private set; }
        public bool Ready { get; private set; }
        public bool Complete { get; private set; }
        public bool Level02Complete { get; private set; }
        public int CurrentRegion => player.transform.position.x >= Parameters.ToWorld(Data.regions[0].endPx) ? 1 : 0;
        [Tooltip("Development overlay only; disabled in the shipped scene")]
        public bool showDebugOverlay;
        public int CrossingCount { get; private set; }
        private ChapterData loadedData;
        private GameParameters loadedParameters;
        public ChapterData Data => loadedData ?? (loadedData = ChapterData.Load());
        public GameParameters Parameters => loadedParameters ?? (loadedParameters = GameParameters.Load());
        public float Axis => -Parameters.ToWorld(Data.mirrorAxisPx);
        private Vector2 checkpoint;
        private bool checkpointWhite, checkpointReached;
        private bool[] activatedCheckpoints;
        private int checkpointIndex = -1;
        public Vector2 RecoveryFoot => checkpoint;
        public bool RecoveryWhite => checkpointWhite;
        public int CheckpointIndex => checkpointIndex;
        private GUIStyle titleStyle, hintStyle;

        private void Awake()
        {
            loadedParameters = GameParameters.Load(); loadedData = ChapterData.Load();
            Time.fixedDeltaTime = Parameters.FixedDeltaTime;
            checkpoint = Parameters.SourcePointToWorld(Data.spawnCenterPx) - Vector2.up * Parameters.PlayerColliderSize.y / 2;
            Ready = true;
            activatedCheckpoints=new bool[Data.checkpoints.Length];
            ApplyGeometry();
        }
        private void Start() { player.PlaceFoot(checkpoint, false); }
        public static Vector2 Reflect(Vector2 p, float axis) => new Vector2(p.x, 2 * axis - p.y);
        private void ApplyGeometry()
        {
            foreach (var piece in terrain) piece.Apply(Mirrored, Axis);
            foreach (var gate in gates) gate.Apply(Mirrored, Axis);
            ApplyMarker("Checkpoint", Parameters.SourcePointToWorld(Data.checkpointFootPx));
            ApplyMarker("Exit · continue right", Parameters.SourcePointToWorld(Data.exitFootPx));
            foreach(var marker in GetComponentsInChildren<ProgressMarker>())
            {
                int index=System.Array.FindIndex(Data.checkpoints,c=>c.id==marker.checkpointId);
                marker.Apply(Mirrored,Axis,index>=0 && activatedCheckpoints!=null && activatedCheckpoints[index]);
            }
            Physics2D.SyncTransforms();
        }
        private void ApplyMarker(string name, Vector2 whiteFoot)
        {
            var marker = transform.Find(name);
            if (!marker) return;
            Vector2 foot = Mirrored ? whiteFoot : Reflect(whiteFoot, Axis);
            marker.position = foot + Vector2.up * .275f;
            foreach (var sr in marker.GetComponentsInChildren<SpriteRenderer>()) sr.color = ClothArt.Pattern;
        }
        public bool IsGrounded(Vector2 foot, float halfWidth)
        {
            float tolerance = Parameters.ToWorld(Data.groundTolerancePx);
            foreach (var p in terrain)
            {
                if (!p.Shape.enabled) continue;
                Rect r = p.WorldRect;
                if (foot.x + halfWidth > r.xMin && foot.x - halfWidth < r.xMax && Mathf.Abs(foot.y - r.yMax) <= tolerance) return true;
            }
            return false;
        }
        public float ClampHorizontal(Vector2 center, Vector2 half, float delta)
        {
            foreach (var p in terrain)
            {
                if (!p.Shape.enabled) continue;
                Rect r = p.WorldRect;
                if (center.y + half.y <= r.yMin + 0.00001f || center.y - half.y >= r.yMax - 0.00001f) continue;
                if (delta > 0 && center.x + half.x <= r.xMin + 0.00001f) delta = Mathf.Min(delta, r.xMin - center.x - half.x);
                if (delta < 0 && center.x - half.x >= r.xMax - 0.00001f) delta = Mathf.Max(delta, r.xMax - center.x + half.x);
            }
            return Mathf.Clamp(center.x + delta, Parameters.ToWorld(Data.segments[0].xPx) + half.x, Parameters.ToWorld(Data.widthPx) - half.x) - center.x;
        }
        public float ClampVertical(Vector2 center, Vector2 half, float delta, out bool landed)
        {
            float original = delta; landed = false;
            foreach (var p in terrain)
            {
                if (!p.Shape.enabled) continue;
                Rect r = p.WorldRect;
                if (center.x + half.x <= r.xMin || center.x - half.x >= r.xMax) continue;
                if (delta < 0 && center.y - half.y >= r.yMax - 0.00001f)
                    delta = Mathf.Max(delta, r.yMax - center.y + half.y);
                if (delta > 0 && center.y + half.y <= r.yMin + 0.00001f)
                    delta = Mathf.Min(delta, r.yMin - center.y - half.y);
            }
            landed = original < 0 && delta > original + 0.000001f;
            return delta;
        }
        public bool TryCross(ClothPlayer actor, float downwardDelta)
        {
            if(!actor.Diving || downwardDelta>=0) return false;
            Vector2 foot = actor.Foot;
            foreach (var gate in gates)
            {
                float surface = gate.Surface(Mirrored, Axis);
                if (!gate.Contains(foot.x, Parameters.PlayerColliderSize.x / 2) || foot.y < surface - 0.00001f || foot.y + downwardDelta > surface) continue;
                // Solve exact swept contact time. Reflect the FOOT at contact, never snap to an unrelated exit.
                Vector2 contact = new Vector2(foot.x, surface);
                Vector2 target = Reflect(contact, Axis);
                bool targetPhase = !Mirrored;
                Vector2 targetCenter = target + Vector2.up * Parameters.PlayerColliderSize.y / 2;
                foreach (var p in terrain)
                {
                    if (p.blue == targetPhase) continue;
                    Vector2 c = targetPhase ? Reflect(p.baseCenter, Axis) : p.baseCenter;
                    var r = new Rect(c - p.size / 2, p.size);
                    var body = new Rect(targetCenter - Parameters.PlayerColliderSize / 2, Parameters.PlayerColliderSize);
                    if (body.xMin < r.xMax - 0.00001f && body.xMax > r.xMin + 0.00001f && body.yMin < r.yMax - 0.00001f && body.yMax > r.yMin + 0.00001f)
                    {
                        Debug.LogError("Unsafe crossing geometry; exact reflection refused."); return false;
                    }
                }
                Mirrored = targetPhase; ApplyGeometry(); actor.PlaceFoot(target, true); CrossingCount++;
                Debug.Assert(Mathf.Abs(actor.Foot.y - (2 * Axis - contact.y)) < 0.00001f && Mathf.Abs(actor.Foot.x - contact.x) < 0.00001f, "Foot reflection invariant");
                // First arrival is a local recovery point, later replaced by the visible checkpoint.
                checkpoint = target; checkpointWhite = Mirrored;
                return true;
            }
            return false;
        }
        public void CheckProgress(ClothPlayer actor)
        {
            var foot = actor.Foot;
            float x = foot.x;
            var wall = Data.segments[2];
            if (foot.y < -Parameters.ToWorld(Data.killBelowPx) ||
                (Mirrored && x > Parameters.ToWorld(wall.xPx) && x < Parameters.ToWorld(wall.xPx + wall.widthPx) && foot.y < -Parameters.ToWorld(Data.whiteValleyKillPx)))
            { Respawn(); return; }
            foreach(var zone in Data.recoveryZones)
            {
                if(actor.Blue==zone.blue && x>Parameters.ToWorld(zone.leftPx) && x<Parameters.ToWorld(zone.rightPx) && foot.y < -Parameters.ToWorld(zone.belowPx))
                { Respawn(); return; }
            }
            if(activatedCheckpoints==null) activatedCheckpoints=new bool[Data.checkpoints.Length];
            for(int i=0;i<Data.checkpoints.Length;i++)
            {
                var node=Data.checkpoints[i];
                if(i<=checkpointIndex || actor.Blue!=node.blue || !actor.Grounded || x<Parameters.ToWorld(node.xPx) || x>Parameters.ToWorld(node.xPx+100)) continue;
                checkpointIndex=i; activatedCheckpoints[i]=true; checkpointReached=true; checkpointWhite=Mirrored;
                Vector2 baseFoot=Parameters.SourcePointToWorld(new Vector2(node.xPx,node.surfacePx));
                checkpoint=Mirrored?Reflect(baseFoot,Axis):baseFoot;
                ApplyGeometry();
            }
            if(Mirrored && actor.Grounded && x>=Parameters.ToWorld(Data.regions[0].exitPx)) Complete=true;
            if(Data.regions.Length>1 && !Mirrored && actor.Grounded && x>=Parameters.ToWorld(Data.regions[1].exitPx)) Level02Complete=true;
        }
        public void Respawn()
        {
            Mirrored = checkpointWhite; ApplyGeometry(); player.PlaceFoot(checkpoint, false);
        }
        public void Restart()
        {
            Complete = false; checkpointReached = false; checkpointWhite = false; CrossingCount = 0;
            Level02Complete=false; checkpointIndex=-1; activatedCheckpoints=new bool[Data.checkpoints.Length];
            checkpoint = Parameters.SourcePointToWorld(Data.spawnCenterPx) - Vector2.up * Parameters.PlayerColliderSize.y / 2;
            Respawn();
        }
        private void LateUpdate()
        {
            if (!Ready || !gameCamera) return;
            // Letterbox preserves reference viewport on any window, camera never mirrors.
            float targetAspect = (float)Parameters.ReferenceResolution.x / Parameters.ReferenceResolution.y;
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            gameCamera.rect = aspect > targetAspect ? new Rect((1 - targetAspect / aspect) / 2, 0, targetAspect / aspect, 1)
                : new Rect(0, (1 - aspect / targetAspect) / 2, 1, aspect / targetAspect);
            float halfWidth = Parameters.ReferenceViewportSize.x / 2;
            float cx = Mathf.Clamp(player.transform.position.x, halfWidth, Parameters.ToWorld(Data.widthPx) - halfWidth);
            gameCamera.transform.position = new Vector3(cx, -Parameters.ReferenceViewportSize.y / 2, -10);
        }
        private void OnGUI()
        {
            if (!Ready) return;
            // Clear letterbox areas explicitly; a camera's sub-rect cannot clear prior full-frame pixels.
            Rect viewport = gameCamera.pixelRect;
            GUI.DrawTexture(new Rect(0, 0, viewport.xMin, Screen.height), Texture2D.blackTexture);
            GUI.DrawTexture(new Rect(viewport.xMax, 0, Screen.width - viewport.xMax, Screen.height), Texture2D.blackTexture);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height - viewport.yMax), Texture2D.blackTexture);
            GUI.DrawTexture(new Rect(0, Screen.height - viewport.yMin, Screen.width, viewport.yMin), Texture2D.blackTexture);
            if(!showDebugOverlay) return;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2, (Screen.height - 720 * scale) / 2), Quaternion.identity, Vector3.one * scale);
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
                titleStyle.normal.textColor = ClothArt.Blue;
                hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 17 }; hintStyle.normal.textColor = ClothArt.Blue;
            }
            GUI.Label(new Rect(36, 22, 900, 45), Data.chapterTitle + "     /     " + Data.regionTitle, titleStyle);
            GUI.Label(new Rect(36, 66, 1160, 35), "A / D 或 ← → 移动    Space 跳跃    S / ↓ 空中冲跃    R 重来", hintStyle);
            GUI.Label(new Rect(36, 108, 1150, 36), Complete ? "首段完成 · 留白成路    → 下一段待续" : checkpointReached ? "检查点已记录 · 越过前方凹处，向右抵达出口" : Mirrored ? "白态 · 轻跃越过下伸墙形成的凹谷" : "蓝态 · 在莲花上方跃起，向下冲跃穿界", hintStyle);
            GUI.matrix = Matrix4x4.identity;
        }
    }
}
