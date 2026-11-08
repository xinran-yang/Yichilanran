namespace YiChiLanRan
{
    public static class PhaseCollisionSystem
    {
        public static bool Collides(PhaseObject a,PhaseObject b) => Rules.Collides(a.kind,a.phase,b.kind,b.phase);
        public static void Refresh(LevelRuntime world)
        {
            for(int i=0;i<world.Objects.Count;i++)
                for(int j=i+1;j<world.Objects.Count;j++)
                {
                    var a=world.Objects[i]; var b=world.Objects[j];
                    if(!a.gameObject.activeInHierarchy || !b.gameObject.activeInHierarchy || !a.Shape.enabled || !b.Shape.enabled || (a.Body && !a.Body.simulated) || (b.Body && !b.Body.simulated)) continue;
                    UnityEngine.Physics2D.IgnoreCollision(a.Shape,b.Shape,!Collides(a,b));
                }
        }
    }
}

