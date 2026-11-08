using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YiChiLanRan.EditorTools
{
    public static class LayeredReachability
    {
        // Conservative geometric connectivity, not a claim of a unique input sequence.
        public static string Analyze(ChapterWorld world)
        {
            var p=world.Parameters;
            bool initialToUpper=Connected(world,false,new Vector2(100,531),new Vector2(200,261));
            bool whiteToB=Connected(world,true,new Vector2(280,71),new Vector2(200,341));
            bool upperToExit=Connected(world,false,new Vector2(200,261),new Vector2(1740,601));
            if(initialToUpper || !whiteToB || !upperToExit)throw new InvalidOperationException("Configuration-space connectivity failed");
            float g=-p.GravityY*p.PixelsPerUnit,v=p.RigidJumpSpeed*p.PixelsPerUnit,u=p.HorizontalSpeed*p.PixelsPerUnit,c=p.FallSpeedLimit*p.PixelsPerUnit;
            float height=v*v/(2*g),descentHeight=height+130;
            float descent=descentHeight<=c*c/(2*g)?Mathf.Sqrt(2*descentHeight/g):c/g+(descentHeight-c*c/(2*g))/c;
            float conservativeRange=u*(v/g+descent)+p.PlayerColliderSize.x*p.PixelsPerUnit+u*p.CoyoteTimeSeconds;
            if(conservativeRange>=420 || height>=120 || p.DiveBounceHeight*p.PixelsPerUnit>=250)
                throw new InvalidOperationException("Mandatory-portal height/range budget failed");
            return "4px eroded configuration-space grid: initial blue chamber disconnected from upper blue route; white A connected to B; blue B connected to exit.\n"+
                "White forward barrier 120px > jump "+height+"px; conservative flight from cap including body/coyote "+conservativeRange+"px < 420px bridge.\n"+
                "Blue bridge 250px > max rebound "+p.DiveBounceHeight*p.PixelsPerUnit+"px. No exhaustive proof of unique input sequence.";
        }
        private static bool Connected(ChapterWorld world,bool mirrored,Vector2 sourcePx,Vector2 targetPx)
        {
            const int step=4;
            var p=world.Parameters;var data=world.Data;Vector2 half=p.PlayerColliderSize*p.PixelsPerUnit/2;
            int cols=Mathf.FloorToInt((data.widthPx-data.leftPx-2*half.x)/step)+1,rows=181;
            float minX=data.leftPx+half.x,minY=-40;
            var blocks=world.terrain.Where(t=>t.blue!=mirrored).Select(t=>
            {
                var center=mirrored?ChapterWorld.Reflect(t.baseCenter,world.Axis):t.baseCenter;
                var r=new Rect(center-t.size/2,t.size);
                return new Rect(r.xMin*p.PixelsPerUnit,-r.yMax*p.PixelsPerUnit,r.width*p.PixelsPerUnit,r.height*p.PixelsPerUnit);
            }).ToArray();
            var open=new bool[cols*rows];
            for(int y=0;y<rows;y++)for(int x=0;x<cols;x++)
            {
                var body=new Rect(minX+x*step-half.x,minY+y*step-half.y,half.x*2,half.y*2);
                open[y*cols+x]=!blocks.Any(r=>body.xMin<r.xMax-.001f && body.xMax>r.xMin+.001f && body.yMin<r.yMax-.001f && body.yMax>r.yMin+.001f);
            }
            Func<Vector2,int> nearest=point=>
            {
                int cx=Mathf.Clamp(Mathf.RoundToInt((point.x-minX)/step),0,cols-1),cy=Mathf.Clamp(Mathf.RoundToInt((point.y-minY)/step),0,rows-1);
                if(open[cy*cols+cx])return cy*cols+cx;
                for(int d=1;d<=3;d++)if(cy-d>=0 && open[(cy-d)*cols+cx])return(cy-d)*cols+cx;
                throw new InvalidOperationException("Analysis anchor inside terrain");
            };
            int start=nearest(sourcePx),target=nearest(targetPx);var visited=new bool[open.Length];var queue=new Queue<int>();queue.Enqueue(start);visited[start]=true;
            while(queue.Count>0)
            {
                int node=queue.Dequeue();if(node==target)return true;
                int x=node%cols,y=node/cols;
                foreach(int next in new[]{x>0?node-1:-1,x+1<cols?node+1:-1,y>0?node-cols:-1,y+1<rows?node+cols:-1})
                    if(next>=0 && open[next] && !visited[next]){visited[next]=true;queue.Enqueue(next);}
            }
            return false;
        }
    }
}
