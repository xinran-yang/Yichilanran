using System.Collections.Generic;
using UnityEngine;
namespace YiChiLanRan.Editor
{
    // Build the white realm's solids and ink-space from ONE floor profile, not decorative replacement roads.
    public static class WhiteRealmGeometry
    {
        public static void Add(LevelDefinition d,string id,float x,float width,float top,IEnumerable<Rect> inkHoles)
        {
            var pieces=new List<Rect>{new Rect(x,.1f,width,top-.1f)};
            foreach(var hole in inkHoles)
            {
                var next=new List<Rect>();
                foreach(var r in pieces) Subtract(r,hole,next);
                pieces=next;
            }
            int n=0;
            foreach(var r in pieces) d.terrain.Add(new ShapeSpec(id+" "+n++,Rules.Mirror(r,d.mirrorAxis),Phase.White));
        }
        static void Subtract(Rect r,Rect hole,List<Rect> result)
        {
            float x0=Mathf.Max(r.xMin,hole.xMin),x1=Mathf.Min(r.xMax,hole.xMax),y0=Mathf.Max(r.yMin,hole.yMin),y1=Mathf.Min(r.yMax,hole.yMax);
            if(x1<=x0 || y1<=y0) { result.Add(r); return; }
            Keep(result,new Rect(r.xMin,r.yMin,x0-r.xMin,r.height));
            Keep(result,new Rect(x1,r.yMin,r.xMax-x1,r.height));
            Keep(result,new Rect(x0,r.yMin,x1-x0,y0-r.yMin));
            Keep(result,new Rect(x0,y1,x1-x0,r.yMax-y1));
        }
        static void Keep(List<Rect> list,Rect r) { if(r.width>.0001f && r.height>.0001f) list.Add(r); }
    }
}
