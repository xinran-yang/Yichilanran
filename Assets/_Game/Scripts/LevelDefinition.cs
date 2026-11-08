using System;
using System.Collections.Generic;
using UnityEngine;

namespace YiChiLanRan
{
    public enum Phase { Blue, White, Neutral }
    public enum ObjectKind { Terrain, Player, Box, BoxBarrier }
    [Serializable] public struct ShapeSpec
    {
        public string id;
        public Rect rect;
        public Phase phase;
        public bool elastic, breakable;
        public ObjectKind kind;
        public ShapeSpec(string id, Rect rect, Phase phase, bool elastic = false, ObjectKind kind = ObjectKind.Terrain)
        { this.id=id; this.rect=rect; this.phase=phase; this.elastic=elastic; this.kind=kind; breakable=false; }
    }
    [Serializable] public struct BoxSpec
    {
        public string id; public Vector2 center; public Phase phase;
        public BoxSpec(string id, float x, float bottom, Phase phase)
        { this.id=id; center=new Vector2(x,bottom+Rules.BoxSize/2); this.phase=phase; }
    }
    [Serializable] public struct ZoneSpec
    {
        public string id; public float x, width, floor; public bool green, allowSolo, allowBox;
        public bool hasMirroredFloor;
        public float mirroredFloor;
        public ZoneSpec(string id,float x,float width,float floor,bool green,bool solo=true)
        { this.id=id; this.x=x; this.width=width; this.floor=floor; this.green=green; allowSolo=solo; allowBox=green; hasMirroredFloor=false; mirroredFloor=0; }
    }
    [CreateAssetMenu(menuName="YiChiLanRan/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        public string levelId, nextScene;
        public float mirrorAxis=6;
        public bool mirrorInspection;
        public Rect bounds=new Rect(0,0,24,12);
        public Vector2 spawn;
        public Vector2 goal;
        public List<ShapeSpec> terrain=new List<ShapeSpec>();
        public List<BoxSpec> boxes=new List<BoxSpec>();
        public List<ZoneSpec> zones=new List<ZoneSpec>();
        [TextArea] public string routeNotes;
    }
    public static class Rules
    {
        public const float PlayerWidth=.56f, PlayerHeight=.76f, BoxSize=1f;
        public const float Speed=5.2f, Gravity=36f, Jump=9.2f, BlueJump=11.5f, Bounce=13.98f, Dive=21f, MaxFall=12.4f;
        // Playability demo: S is for diving/transferring, without automatic elastic rebound.
        // Keep the original tuning so this optional material behaviour can return later.
        public static readonly bool EnableDiveBounce=false;
        public const float Step=1f/120f, Skin=.02f;
        public static Phase Opposite(Phase phase) => phase==Phase.Blue ? Phase.White : Phase.Blue;
        public static Rect RectAt(Vector2 p, Vector2 size) => new Rect(p-size/2,size);
        public static Rect Mirror(Rect r,float axis) => new Rect(r.x,2*axis-r.yMax,r.width,r.height);
        public static Vector2 Mirror(Vector2 p,float axis) => new Vector2(p.x,2*axis-p.y);
        public static bool Overlap(Rect a,Rect b,float inset=.002f) => a.xMin < b.xMax-inset && a.xMax > b.xMin+inset && a.yMin < b.yMax-inset && a.yMax > b.yMin+inset;
        public static bool Collides(ObjectKind a,Phase ap,ObjectKind b,Phase bp)
        {
            if(a==ObjectKind.BoxBarrier || b==ObjectKind.BoxBarrier)
                return a==ObjectKind.Box || b==ObjectKind.Box;
            return ap==Phase.Neutral || bp==Phase.Neutral || ap==bp;
        }
        public static float ViewportHeight(float aspect) => aspect>=16f/9f ? 1 : aspect/(16f/9f);
    }
}

