using System;
using System.Linq;
using UnityEngine;

namespace YiChiLanRan
{
    [Serializable] public sealed class ContourSegment { public float xPx, widthPx, surfacePx; }
    [Serializable] public sealed class GateDefinition { public float leftPx, rightPx, surfacePx; }
    [Serializable] public sealed class CheckpointDefinition { public string id; public float xPx, surfacePx; public bool blue; }
    [Serializable] public sealed class RegionDefinition { public string id; public float startPx, endPx, exitPx; public bool exitBlue; }
    [Serializable] public sealed class RecoveryZone { public float leftPx, rightPx, belowPx; public bool blue; }
    [Serializable] public sealed class ChapterExtension
    {
        public float widthPx;
        public ContourSegment[] segments;
        public GateDefinition[] gates;
        public CheckpointDefinition[] checkpoints;
        public RegionDefinition[] regions;
        public RecoveryZone[] recoveryZones;
    }
    [Serializable] public sealed class ChapterData
    {
        public string chapterTitle, regionTitle;
        public float widthPx, topPx, bottomPx, mirrorAxisPx, killBelowPx, whiteValleyKillPx, groundTolerancePx;
        public Vector2 spawnCenterPx, checkpointFootPx, exitFootPx;
        public ContourSegment[] segments;
        public GateDefinition[] gates;
        public CheckpointDefinition[] checkpoints;
        public RegionDefinition[] regions;
        public RecoveryZone[] recoveryZones;
        public static ChapterData Load(bool includeExtension=true)
        {
            var asset = Resources.Load<TextAsset>("Configuration/Chapter01");
            if (!asset) throw new InvalidOperationException("Missing Chapter01 data");
            var data = JsonUtility.FromJson<ChapterData>(asset.text);
            if (data.segments == null || data.gates == null || data.widthPx <= 0)
                throw new InvalidOperationException("Invalid Chapter01 geometry");
            data.checkpoints = new[] { new CheckpointDefinition { id="level01-checkpoint", xPx=data.checkpointFootPx.x,
                surfacePx=2*data.mirrorAxisPx-data.checkpointFootPx.y, blue=false } };
            data.regions = new[] { new RegionDefinition { id="level01",startPx=data.segments[0].xPx,endPx=data.widthPx,exitPx=data.exitFootPx.x,exitBlue=false } };
            data.recoveryZones = Array.Empty<RecoveryZone>();
            var extensionAsset=Resources.Load<TextAsset>("Configuration/Chapter01Level02");
            if(extensionAsset && includeExtension)
            {
                var extension=JsonUtility.FromJson<ChapterExtension>(extensionAsset.text);
                data.segments=data.segments.Concat(extension.segments).ToArray();
                data.gates=data.gates.Concat(extension.gates).ToArray();
                data.checkpoints=data.checkpoints.Concat(extension.checkpoints).ToArray();
                data.regions=data.regions.Concat(extension.regions).ToArray();
                data.recoveryZones=extension.recoveryZones;
                data.widthPx=extension.widthPx;
            }
            for(int i=1;i<data.segments.Length;i++)
                if(Mathf.Abs(data.segments[i-1].xPx+data.segments[i-1].widthPx-data.segments[i].xPx)>.001f)
                    throw new InvalidOperationException("Contour seam gap or overlap at " + data.segments[i].xPx);
            return data;
        }
    }
}
