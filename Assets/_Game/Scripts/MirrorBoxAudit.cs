using System;
using UnityEngine;
namespace YiChiLanRan
{
    [Serializable]
    public sealed class MirrorBoxAudit
    {
        public string transaction, zone, moment, box;
        public float axis;
        public bool participated, applied, formulaVerified;
        public Vector2 before, expected, after;
        public Vector2 colliderBefore, colliderExpected, colliderAfter;
        public Phase colorBefore, colorTarget, colorAfter;
        public float positionError, colliderError;
        public string reason;
        public bool simulated,colliderEnabled,solidForPlayer;
        public float gravityScale,customGravityY;
        public string supportId; public int supportSide;
    }
}

