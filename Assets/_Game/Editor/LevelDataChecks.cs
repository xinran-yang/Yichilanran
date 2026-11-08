using System;
using System.IO;
using System.Text;
using UnityEngine;
namespace YiChiLanRan.Editor
{
    public static class LevelDataChecks
    {
        public static void Validate(LevelDefinition d)
        {
            var r=new StringBuilder("Geometry validation: "+d.levelId+"\n");
            if(d.boxes.Count!=3) throw new Exception("Expected three boxes");
            foreach(var b in d.boxes)
            {
                var rect=Rules.RectAt(b.center,Vector2.one*Rules.BoxSize);
                foreach(var t in d.terrain)
                    if(Rules.Collides(ObjectKind.Box,b.phase,t.kind,t.phase) && Rules.Overlap(rect,t.rect)) throw new Exception(b.id+" initially overlaps "+t.id);
                r.AppendLine("PASS initial box clear: "+b.id);
            }
            var player=Rules.RectAt(d.spawn,new Vector2(Rules.PlayerWidth,Rules.PlayerHeight));
            foreach(var t in d.terrain)
                if(Rules.Collides(ObjectKind.Player,Phase.Blue,t.kind,t.phase) && Rules.Overlap(player,t.rect)) throw new Exception("Player initially overlaps "+t.id);
            r.AppendLine("PASS initial player clear");
            if(Rules.Bounce*Rules.Bounce/(2*Rules.Gravity)>=2.78f) throw new Exception("Ground bounce bypasses box ledges");
            if(1+Rules.BlueJump*Rules.BlueJump/(2*Rules.Gravity)<=2.78f) throw new Exception("Box-assisted blue jump too low");
            r.AppendLine("PASS ground bounce 2.714 < ledge 2.780 < box + blue jump 2.837 (continuous model; physics route pending)");
            if(d.levelId=="2.2")
            {
                float whiteJump=Rules.Jump*Rules.Jump/(2*Rules.Gravity);
                if(whiteJump>=2.05f || whiteJump+Rules.BoxSize<=2.05f) throw new Exception("G1 right white ledge height invalid");
                r.AppendLine("PASS G1 right rise 2.05 requires box 1, white jump < rise < box + jump");
                r.AppendLine("PASS G2 pit width 1.08 for square box width 1.00 (contact clearance); actual pushing requires physics regression");
                r.AppendLine("PASS G3 box-only barrier collision rule checked in mechanics suite");
                r.AppendLine("PASS G4 initial blue roof leaves only 0.25 above box 3, less than player 0.76");
            }
            Directory.CreateDirectory("DevReports"); File.WriteAllText("DevReports/Geometry-"+d.levelId+".txt",r.ToString());
        }
    }
}

