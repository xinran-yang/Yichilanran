using UnityEngine;
using UnityEngine.InputSystem;
namespace YiChiLanRan
{
    [DefaultExecutionOrder(1000)]
    public sealed class MirrorInspection : MonoBehaviour
    {
        public LevelRuntime world;
        void Start()
        {
            if(!world) world=GetComponent<LevelRuntime>();
            if(!world.definition.mirrorInspection) { enabled=false; return; }
            world.Player.ExternalInput=true;
            // The inspection player is a stationary observer, not a second support platform.
            world.Player.Entity.Body.simulated=false;
            world.Mirror.DumpLiveState();
        }
        void Update()
        {
            if(Keyboard.current==null) return;
            var box=world.GetComponentInChildren<TransferBox2D>();
            box.inspectionPush=world.Busy ? 0 : (Keyboard.current.dKey.isPressed ? 1:0)-(Keyboard.current.aKey.isPressed ? 1:0);
            if(world.Busy) return;
            if(Keyboard.current.cKey.wasPressedThisFrame)
            { world.Player.Entity.SetPhase(Rules.Opposite(world.Player.Entity.phase)); box.ApplyPhaseState(world); PhaseCollisionSystem.Refresh(world); world.Mirror.DumpLiveState(); }
            if(Keyboard.current.tKey.wasPressedThisFrame) world.Mirror.TryInspectionTransfer(world.Zones[0]);
            if(Keyboard.current.f3Key.wasPressedThisFrame) world.Mirror.DumpLiveState();
        }
        void OnGUI()
        {
            if(!world || !world.definition.mirrorInspection) return;
            GUI.Box(new Rect(16,175,720,80),"MIRROR INSPECTION - T: mirror  /  C: observer color  /  A,D: active-box push  /  F3: log  /  R: reset\nReference support is retained on BOTH sides. C is an inspection-only phase switch.\nOnly this inspection scene permits T; gameplay still requires Dive.");
        }
    }
}

