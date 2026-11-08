using UnityEngine;
using UnityEngine.InputSystem;
namespace YiChiLanRan
{
    public sealed class PlayerController2D : MonoBehaviour
    {
        public PhaseObject Entity => GetComponent<PhaseObject>();
        public bool Grounded {get;private set;}
        public bool Diving {get;private set;}
        public float Axis {get;private set;}
        public bool ExternalInput;
        bool jumpPending,divePending,usedDive;
        float jumpUntil=-100,lastGround=-100,lastDive=-100,recoveryUntil;
        PhaseObject ground;
        public void Command(float axis,bool jump=false,bool dive=false)
        { Axis=Mathf.Clamp(axis,-1,1); jumpPending|=jump; divePending|=dive; }
        public void ReadInput()
        {
            if(ExternalInput || Keyboard.current==null) return;
            var k=Keyboard.current;
            Command((k.dKey.isPressed||k.rightArrowKey.isPressed ? 1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed ? 1:0),k.spaceKey.wasPressedThisFrame,k.sKey.wasPressedThisFrame||k.downArrowKey.wasPressedThisFrame);
        }
        public void ResetController()
        {
            ClearInput(); Grounded=false; Diving=false; usedDive=false; ground=null;
            jumpUntil=lastGround=lastDive=-100; recoveryUntil=0;
        }
        public void ClearInput() { jumpPending=divePending=false; Axis=0; }
        public void AfterTransfer(float now) { Diving=false; usedDive=true; lastGround=-100; recoveryUntil=now+.12f; ClearInput(); }
        public void Bounce(float now) { Entity.Body.linearVelocity=new Vector2(Entity.Body.linearVelocity.x,Rules.Bounce); Diving=false; usedDive=true; lastGround=-100; recoveryUntil=now+.025f; }
        public void EndDive() { Diving=false; }
        public void Tick(LevelRuntime world,float dt)
        {
            var body=Entity.Body; float now=world.Clock;
            world.ResolveDive(dt);
            if(world.Busy || world.MirrorCommittedThisTick) return;
            ground=world.Support(Entity);
            Grounded=ground!=null && body.linearVelocity.y<=.15f && (!Diving || Entity.Bounds.yMin-ground.Bounds.yMax<=.025f);
            if(Grounded)
            {
                lastGround=now; usedDive=false;
                if(Diving) { Diving=false; recoveryUntil=now+.12f; }
            }
            if(Grounded && Mathf.Abs(Axis)>.01f && !jumpPending) world.TrySmallStep(Entity,Axis,dt);
            if(jumpPending) jumpUntil=now+.11f;
            if(now<recoveryUntil) jumpUntil=-100;
            float vy=body.linearVelocity.y;
            if(jumpUntil>=now && now-lastGround<=.1f && now>=recoveryUntil)
            {
                vy=ground!=null && ground.elastic ? Rules.BlueJump : Rules.Jump;
                jumpUntil=-100; lastGround=-100; Grounded=false;
            }
            if(divePending && !Grounded && !usedDive && now-lastDive>=.24f && now>=recoveryUntil)
                        {
                Diving=true; usedDive=true; lastDive=now; vy=-Rules.Dive;
                // Sweep the FIRST dive step too; a nearby interface may be crossed before the next FixedUpdate.
                body.linearVelocity=new Vector2(body.linearVelocity.x,vy); world.ResolveDive(dt);
                if(world.Busy || world.MirrorCommittedThisTick) return;
                vy=body.linearVelocity.y;
            }
            vy-=Rules.Gravity*dt;
            body.linearVelocity=new Vector2(now<recoveryUntil ? 0 : Axis*Rules.Speed,Mathf.Max(Diving ? -Rules.Dive : -Rules.MaxFall,vy));
            body.rotation=0;
            jumpPending=divePending=false;
        }
    }
}





