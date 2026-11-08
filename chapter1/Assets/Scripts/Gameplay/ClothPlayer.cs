using UnityEngine;
using UnityEngine.InputSystem;
using YiChiLanRan.Configuration;

namespace YiChiLanRan
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ClothPlayer : MonoBehaviour
    {
        public ChapterWorld world;
        public Vector2 Velocity { get; private set; }
        public bool Grounded { get; private set; }
        public bool Diving { get; private set; }
        public bool Blue => !world.Mirrored;
        public Vector2 Foot => (Vector2)transform.position - Vector2.up * Half.y;
        // Non-Unity plain objects are not retained across an editor hot reload.
        private GameParameters loadedConfig;
        private GameParameters config => loadedConfig ?? (loadedConfig = GameParameters.Load());
        private Vector2 Half => config.PlayerColliderSize / 2;
        private float axisInput, jumpUntil = -1, lastGround = -10, nextDive, recoveryUntil;
        private bool divePressed, diveUsed;
        private SpriteRenderer visual;
        private bool validationInput;
        public bool ControlsLocked { get; private set; }
        public void SetControlLock(bool locked)
        {
            ControlsLocked=locked; axisInput=0; jumpUntil=-1; divePressed=false;
            Velocity=Vector2.zero; Diving=false; lastGround=-10;
        }

        private void Awake()
        {
            GetComponent<BoxCollider2D>().size = config.PlayerColliderSize;
            visual = GetComponent<SpriteRenderer>();
        }
        private void OnEnable() { visual = GetComponent<SpriteRenderer>(); }
        private void Update()
        {
            if (validationInput || ControlsLocked) return;
            var k = Keyboard.current;
            if (k == null) return;
            axisInput = ((k.dKey.isPressed || k.rightArrowKey.isPressed) ? 1 : 0)
                      - ((k.aKey.isPressed || k.leftArrowKey.isPressed) ? 1 : 0);
            if (k.spaceKey.wasPressedThisFrame) jumpUntil = Time.time + config.JumpBufferSeconds;
            if (k.sKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame) divePressed = true;
            if (k.rKey.wasPressedThisFrame) world.Restart();
        }
        private void FixedUpdate() => Step(Time.fixedDeltaTime);

        // Same entry point used by the single automated smoke playthrough; no alternate physics.
        public void SetInputForValidation(float horizontal, bool jump, bool dive)
        {
            validationInput = true;
            axisInput = horizontal;
            if (jump) jumpUntil = Time.time + config.JumpBufferSeconds;
            divePressed = dive;
        }
        public void ReleaseValidationInput()
        {
            validationInput = false; axisInput = 0; divePressed = false; jumpUntil = -1;
        }
        public void Step(float dt)
        {
            if (!world || !world.Ready || ControlsLocked) return;
            Grounded = world.IsGrounded(Foot, Half.x);
            if (Grounded && !Diving) { lastGround = Time.time; diveUsed = false; }
            var v = Velocity;
            if (Time.time < recoveryUntil) v.x = 0;
            else v.x = axisInput * config.HorizontalSpeed;
            if (jumpUntil >= Time.time && Time.time - lastGround <= config.CoyoteTimeSeconds && !Diving && Time.time >= recoveryUntil)
            {
                v.y = Blue ? config.ElasticJumpSpeed : config.RigidJumpSpeed;
                jumpUntil = -1; lastGround = -10; Grounded = false;
            }
            if (divePressed && !Grounded && !diveUsed && Time.time >= nextDive && Time.time >= recoveryUntil)
            {
                Diving = true; diveUsed = true; v.y = config.DiveVelocityY;
                nextDive = Time.time + config.DiveCooldownSeconds;
            }
            divePressed = false;
            v.y = Diving ? config.DiveVelocityY : Mathf.Max(v.y + config.GravityY * dt, -config.FallSpeedLimit);

            Vector2 position = transform.position;
            float dx = world.ClampHorizontal(position, Half, v.x * dt);
            position.x += dx;
            transform.position = position;
            if (Diving && world.TryCross(this, v.y * dt)) return;
            bool landed;
            float dy = world.ClampVertical(position, Half, v.y * dt, out landed);
            position.y += dy;
            transform.position = position;
            if (landed)
            {
                if (Diving && Blue) v.y = config.DiveBounceSpeed;
                else { v.y = 0; recoveryUntil = Diving ? Time.time + config.LandingRecoverySeconds : recoveryUntil; }
                Diving = false; diveUsed = false; Grounded = v.y == 0;
            }
            else if (Mathf.Abs(dy - v.y * dt) > 0.00001f) v.y = 0;
            Velocity = v;
            world.CheckProgress(this);
        }
        public void PlaceFoot(Vector2 foot, bool afterCross)
        {
            transform.position = foot + Vector2.up * Half.y;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            Velocity = Vector2.zero; Diving = false; Grounded = true;
            diveUsed = afterCross; divePressed = false; jumpUntil = -1; lastGround = -10;
            recoveryUntil = Time.time + (afterCross ? config.LandingRecoverySeconds : 0);
            visual.color = Blue ? ClothArt.Blue : ClothArt.Cream;
        }
    }
}
