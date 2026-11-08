using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YiChiLanRan
{
    public struct TerrainRectangle { public Rect pixels; public bool blue; }
    public static class BaseTerrainGeometry
    {
        // One blue orthogonal shape; white is its exact complement in the level bounds.
        public static List<TerrainRectangle> Build(ChapterData data)
        {
            var result=new List<TerrainRectangle>();
            if(data.blueRectangles==null)
            {
                foreach(var s in data.segments)
                {
                    result.Add(new TerrainRectangle{blue=true,pixels=new Rect(s.xPx,s.surfacePx,s.widthPx,data.bottomPx-s.surfacePx)});
                    result.Add(new TerrainRectangle{blue=false,pixels=new Rect(s.xPx,data.topPx,s.widthPx,s.surfacePx-data.topPx)});
                }
                return result;
            }
            var edges=data.blueRectangles.SelectMany(r=>new[]{r.xPx,r.xPx+r.widthPx}).Concat(new[]{data.leftPx,data.widthPx}).Distinct().OrderBy(x=>x).ToArray();
            for(int i=0;i<edges.Length-1;i++)
            {
                float left=edges[i],right=edges[i+1],mid=(left+right)/2;
                var bands=data.blueRectangles.Where(r=>mid>=r.xPx && mid<r.xPx+r.widthPx).OrderBy(r=>r.yPx).ToArray();
                var merged=new List<Vector2>();
                foreach(var band in bands)
                {
                    float end=band.yPx+band.heightPx;
                    if(band.yPx<data.topPx || end>data.bottomPx || band.heightPx<=0)throw new InvalidOperationException("Invalid blue rectangle at "+left);
                    if(merged.Count>0 && band.yPx<=merged[merged.Count-1].y)
                    {var last=merged[merged.Count-1];last.y=Mathf.Max(last.y,end);merged[merged.Count-1]=last;}
                    else merged.Add(new Vector2(band.yPx,end));
                }
                float cursor=data.topPx;
                foreach(var band in merged)
                {
                    Add(result,false,left,cursor,right-left,band.x-cursor);
                    Add(result,true,left,band.x,right-left,band.y-band.x);cursor=band.y;
                }
                Add(result,false,left,cursor,right-left,data.bottomPx-cursor);
            }
            return result;
        }
        private static void Add(List<TerrainRectangle> list,bool blue,float x,float y,float w,float h)
        {
            if(h>0)list.Add(new TerrainRectangle{blue=blue,pixels=new Rect(x,y,w,h)});
        }
    }
}
