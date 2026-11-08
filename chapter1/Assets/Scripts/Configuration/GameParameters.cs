using System;
using UnityEngine;

namespace YiChiLanRan.Configuration
{
    /// <summary>Single parameter source. JSON keeps reference pixels; consumers use world units.</summary>
    public sealed class GameParameters
    {
        [Serializable]
        private sealed class Data
        {
            public int schemaVersion, pixelsPerUnit, referenceWidthPx, referenceHeightPx, physicsTicksPerSecond;
            public Vector2 playerVisualSizePx, playerColliderSizePx, playerColliderOffsetPx;
            public float horizontalSpeedPxPerSecond, gravityMagnitudePxPerSecondSquared;
            public float rigidJumpSpeedPxPerSecond, elasticJumpSpeedPxPerSecond;
            public float diveBounceSpeedPxPerSecond, fallSpeedLimitPxPerSecond, diveSpeedPxPerSecond;
            public float diveCooldownMs, landingRecoveryMs, jumpBufferMs, coyoteTimeMs;
            public Vector2 boxSizePx;
            public float boxFallSpeedLimitPxPerSecond, boxPushProximityPx;
            public float whiteSurfaceThicknessPx, whiteSurfaceMaxChunkWidthPx;
            public float whiteSupportDropReferencePx, whiteBreakDownwardSpeedPxPerSecond;
        }

        private readonly Data data;
        private GameParameters(Data source) { data = source; }

        // Explicit loading only: does not alter Physics2D, cameras, scenes or import settings.
        public static GameParameters Load()
        {
            var asset = Resources.Load<TextAsset>("Configuration/GameParameters");
            if (asset == null) throw new InvalidOperationException("Missing GameParameters.json.");
            var source = JsonUtility.FromJson<Data>(asset.text);
            if (source == null || source.schemaVersion != 1 || source.pixelsPerUnit <= 0 || source.physicsTicksPerSecond <= 0 ||
                source.referenceWidthPx <= 0 || source.referenceHeightPx <= 0 ||
                source.playerColliderSizePx.x <= 0 || source.playerColliderSizePx.y <= 0 ||
                source.gravityMagnitudePxPerSecondSquared <= 0)
                throw new InvalidOperationException("Invalid GameParameters configuration.");
            return new GameParameters(source);
        }

        public int PixelsPerUnit => data.pixelsPerUnit;
        public float FixedDeltaTime => 1f / data.physicsTicksPerSecond;
        public Vector2Int ReferenceResolution => new Vector2Int(data.referenceWidthPx, data.referenceHeightPx);
        public float ToWorld(float pixels) => pixels / PixelsPerUnit;
        public Vector2 ToWorld(Vector2 pixels) => pixels / PixelsPerUnit;
        public Vector2 ReferenceViewportSize => ToWorld((Vector2)ReferenceResolution);
        public float ReferenceOrthographicSize => ReferenceViewportSize.y / 2f;
        // Source origin (0,0) maps to world (0,0); Unity y points upward.
        public Vector2 SourcePointToWorld(Vector2 pointPx) => new Vector2(ToWorld(pointPx.x), -ToWorld(pointPx.y));
        public Rect SourceRectToWorld(Rect rectPx) => new Rect(ToWorld(rectPx.x), -ToWorld(rectPx.y + rectPx.height), ToWorld(rectPx.width), ToWorld(rectPx.height));
        public Vector2 PlayerVisualSize => ToWorld(data.playerVisualSizePx);
        public Vector2 PlayerColliderSize => ToWorld(data.playerColliderSizePx);
        public Vector2 PlayerColliderOffset => SourcePointToWorld(data.playerColliderOffsetPx);
        public float HorizontalSpeed => ToWorld(data.horizontalSpeedPxPerSecond);
        public float GravityY => -ToWorld(data.gravityMagnitudePxPerSecondSquared);
        public float RigidJumpSpeed => ToWorld(data.rigidJumpSpeedPxPerSecond);
        public float ElasticJumpSpeed => ToWorld(data.elasticJumpSpeedPxPerSecond);
        public float DiveBounceSpeed => ToWorld(data.diveBounceSpeedPxPerSecond);
        public float FallSpeedLimit => ToWorld(data.fallSpeedLimitPxPerSecond);
        public float DiveVelocityY => -ToWorld(data.diveSpeedPxPerSecond);
        public float DiveCooldownSeconds => data.diveCooldownMs / 1000f;
        public float LandingRecoverySeconds => data.landingRecoveryMs / 1000f;
        public float JumpBufferSeconds => data.jumpBufferMs / 1000f;
        public float CoyoteTimeSeconds => data.coyoteTimeMs / 1000f;
        public Vector2 BoxSize => ToWorld(data.boxSizePx);
        public float BoxPushSpeed => HorizontalSpeed;
        public float BoxFallSpeedLimit => ToWorld(data.boxFallSpeedLimitPxPerSecond);
        public float BoxPushProximity => ToWorld(data.boxPushProximityPx);
        public float WhiteSurfaceThickness => ToWorld(data.whiteSurfaceThicknessPx);
        public float WhiteSurfaceMaxChunkWidth => ToWorld(data.whiteSurfaceMaxChunkWidthPx);
        public float WhiteSupportDropReference => ToWorld(data.whiteSupportDropReferencePx);
        public float WhiteBreakVelocityY => -ToWorld(data.whiteBreakDownwardSpeedPxPerSecond);
        public float RigidJumpHeight => RigidJumpSpeed * RigidJumpSpeed / (-2f * GravityY);
        public float ElasticJumpHeight => ElasticJumpSpeed * ElasticJumpSpeed / (-2f * GravityY);
        public float DiveBounceHeight => DiveBounceSpeed * DiveBounceSpeed / (-2f * GravityY);
    }
}
