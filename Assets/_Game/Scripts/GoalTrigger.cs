using UnityEngine;
namespace YiChiLanRan
{
    public static class GoalTrigger
    {
        public static bool Reached(LevelRuntime world)
        {
            if(!world.Goal || world.Player.Entity.phase!=Phase.Blue) return false;
            return Rules.Overlap(world.Player.Entity.Bounds,Rules.RectAt(world.Goal.position,new Vector2(.65f,1.2f)));
        }
    }
}
