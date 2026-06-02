using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using Domain;
using GameAiAndControls.Controls.Weather;
using System;
using System.Collections.Generic;
using System.Linq;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Controls
{
    public sealed class LeafDriftControls : IObjectMovement
    {
        public const int VisibleLeafTarget = 90;
        private const int OffscreenLeafReserve = 190;
        public const int TargetLeafCount = VisibleLeafTarget + OffscreenLeafReserve;
        public const float StartGuideYOffset = -900f;
        public const float DepthSpread = 1200f;

        private static readonly string[] FallbackLeafColors =
        {
            "6FA33A",
            "8DB746",
            "C5B84A",
            "D79738",
            "B86E2C"
        };

        private const float DepthStartZ = 700f;
        private const float DepthBehindSpread = 1800f;
        private const float DepthAheadSpread = 3400f;
        private const float MinSize = 4.2f;
        private const float MaxSize = 9.2f;
        private const float MinFallSpeed = 0.25f;
        private const float MaxFallSpeed = 1.15f;
        private const float HorizontalDrift = 0.62f;
        private const float DepthDrift = 0.20f;
        private const float BaseWindX = 0.55f;
        private const float WindPulseX = 1.20f;
        private const float TopRespawnJitter = 260f;

        private static readonly WeatherFieldSettings FieldSettings = new(
            DepthStartZ: DepthStartZ,
            VisibleDepthSpread: DepthSpread,
            BehindSpread: DepthBehindSpread,
            AheadSpread: DepthAheadSpread,
            OffscreenMargin: 260f,
            DirectionalSpawnAheadMin: 650f,
            DirectionalSpawnAheadMax: 3600f,
            DirectionalLateralSpreadFactor: 0.90f,
            TravelBehindRecycleDistance: 2200f,
            TravelAheadRecycleDistance: 3900f,
            DirectionalSpawnModulo: 6,
            VisibleSpreadScreenMultiplier: 0.62f,
            WorldSpreadScreenMultiplier: 2.4f,
            OutsideSpawnChance: 0.70d);

        private readonly Random _random = new();
        private readonly WorldWeatherField _weatherField;
        private readonly List<FallingLeaf> _leaves = new(TargetLeafCount);
        private float _windPhase;
        private float _windX = BaseWindX;

        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public I3dObject? ParentObject { get; set; }
        public IPhysics Physics { get; set; } = new Physics.Physics();

        public LeafDriftControls()
        {
            _weatherField = new WorldWeatherField(_random, FieldSettings);
        }

        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            ParentObject = theObject;

            var mapPosition = GameState.SurfaceState.GlobalMapPosition;
            WorldWeatherField.SyncEmitterToGround(theObject, mapPosition);
            _weatherField.UpdateTravelDirection(mapPosition);
            UpdateWind();

            float endOffsetY = GetEndGuideYOffset();
            EnsureLeaves(mapPosition, endOffsetY);
            EnsureTriangleBuffer(theObject);

            var triangles = GetLeafPart(theObject).Triangles;
            float objectZ = theObject.ObjectOffsets?.z ?? 0f;

            for (int i = 0; i < _leaves.Count; i++)
            {
                var leaf = _leaves[i];
                MoveLeaf(leaf);

                if (ShouldRecycle(leaf, mapPosition, endOffsetY))
                    ResetLeaf(leaf, mapPosition, _weatherField.ShouldSpawnAhead());

                WriteTriangle(triangles[i], leaf, mapPosition, objectZ);
            }

            return theObject;
        }

        private void EnsureLeaves(IVector3 mapPosition, float endOffsetY)
        {
            if (_leaves.Count == TargetLeafCount)
                return;

            _leaves.Clear();
            float halfVisibleSpread = _weatherField.HalfVisibleSpread;
            for (int i = 0; i < VisibleLeafTarget; i++)
            {
                _leaves.Add(CreateVisibleLeaf(
                    mapPosition,
                    RandomRange(-halfVisibleSpread, halfVisibleSpread),
                    RandomRange(StartGuideYOffset, endOffsetY)));
            }

            for (int i = VisibleLeafTarget; i < TargetLeafCount; i++)
                _leaves.Add(CreateReserveLeaf(mapPosition));
        }

        private void EnsureTriangleBuffer(I3dObject theObject)
        {
            var part = GetLeafPart(theObject);
            if (part.Triangles.Count == TargetLeafCount)
                return;

            part.Triangles.Clear();
            for (int i = 0; i < TargetLeafCount; i++)
                part.Triangles.Add(CreateLeafTriangle(i));
        }

        private static I3dObjectPart GetLeafPart(I3dObject theObject)
        {
            var part = theObject.ObjectParts.FirstOrDefault(p => p.PartName == "Leaves");
            if (part != null)
                return part;

            part = new _3dObjectPart
            {
                PartName = "Leaves",
                IsVisible = true
            };
            theObject.ObjectParts.Add(part);
            return part;
        }

        private void UpdateWind()
        {
            _windPhase += 0.014f;
            float gust = MathF.Max(0f, MathF.Sin(_windPhase * 0.23f));
            _windX = BaseWindX + MathF.Sin(_windPhase) * WindPulseX + gust * 0.9f;
        }

        private void MoveLeaf(FallingLeaf leaf)
        {
            leaf.Phase += leaf.SwaySpeed;
            leaf.Angle += leaf.SpinSpeed;
            leaf.WorldX += _windX * leaf.WindWeight + leaf.DriftX + MathF.Sin(leaf.Phase) * leaf.SwayAmount;
            leaf.OffsetY += leaf.FallSpeed + MathF.Sin(leaf.Phase * 0.55f) * leaf.VerticalSway;
            leaf.WorldZ += leaf.DriftZ;
        }

        private bool ShouldRecycle(FallingLeaf leaf, IVector3 mapPosition, float endOffsetY)
        {
            if (leaf.OffsetY > endOffsetY + 90f)
                return true;
            if (leaf.OffsetY < StartGuideYOffset - TopRespawnJitter - 120f)
                return true;

            return _weatherField.ShouldRecycle(leaf.WorldX, leaf.WorldZ, mapPosition);
        }

        private void ResetLeaf(FallingLeaf leaf, IVector3 mapPosition, bool preferAhead)
        {
            var spawn = preferAhead && _weatherField.HasTravelDirection
                ? _weatherField.GetDirectionalSpawnOffset()
                : _weatherField.GetAmbientSpawnOffset(preferOutside: true);

            leaf.WorldX = mapPosition.x + spawn.x;
            leaf.OffsetY = StartGuideYOffset - RandomRange(0f, TopRespawnJitter);
            leaf.WorldZ = mapPosition.z + spawn.z;
            leaf.Size = RandomRange(MinSize, MaxSize);
            leaf.FallSpeed = RandomRange(MinFallSpeed, MaxFallSpeed);
            leaf.DriftX = RandomRange(-HorizontalDrift, HorizontalDrift);
            leaf.DriftZ = RandomRange(-DepthDrift, DepthDrift);
            leaf.WindWeight = RandomRange(0.55f, 1.55f);
            leaf.SwayAmount = RandomRange(0.35f, 1.60f);
            leaf.SwaySpeed = RandomRange(0.012f, 0.032f);
            leaf.VerticalSway = RandomRange(0.04f, 0.24f);
            leaf.SpinSpeed = RandomRange(-0.030f, 0.030f);
            leaf.Angle = RandomRange(0f, MathF.PI * 2f);
            leaf.Phase = RandomRange(0f, MathF.PI * 2f);
        }

        private FallingLeaf CreateVisibleLeaf(IVector3 mapPosition, float offsetX, float offsetY)
        {
            var leaf = new FallingLeaf();
            ResetLeaf(leaf, mapPosition, preferAhead: false);
            leaf.WorldX = mapPosition.x + offsetX;
            leaf.OffsetY = offsetY;
            leaf.WorldZ = mapPosition.z + RandomRange(DepthStartZ, DepthStartZ + DepthSpread);
            return leaf;
        }

        private FallingLeaf CreateReserveLeaf(IVector3 mapPosition)
        {
            var leaf = new FallingLeaf();
            ResetLeaf(leaf, mapPosition, preferAhead: false);
            return leaf;
        }

        private static void WriteTriangle(ITriangleMeshWithColor triangle, FallingLeaf leaf, IVector3 mapPosition, float objectZ)
        {
            float relativeX = leaf.WorldX - mapPosition.x;
            float relativeZ = leaf.WorldZ - mapPosition.z;
            float scale = WorldWeatherField.GetProjectionScale(relativeZ, objectZ);
            float centerX = relativeX / scale;
            float centerY = leaf.OffsetY / scale;
            float size = leaf.Size;
            float angle = leaf.Angle + MathF.Sin(leaf.Phase) * 0.42f;
            float cos = MathF.Cos(angle);
            float sin = MathF.Sin(angle);

            float width = size * 0.56f;
            float length = size * 1.25f;

            triangle.noHidden = true;
            triangle.angle = 1f;
            if (string.IsNullOrWhiteSpace(triangle.Color))
                triangle.Color = FallbackLeafColors[0];

            triangle.vert1.x = centerX - cos * width * 0.5f - sin * length * 0.25f;
            triangle.vert1.y = centerY - sin * width * 0.5f + cos * length * 0.25f;
            triangle.vert1.z = relativeZ;

            triangle.vert2.x = centerX + cos * width * 0.5f - sin * length * 0.25f;
            triangle.vert2.y = centerY + sin * width * 0.5f + cos * length * 0.25f;
            triangle.vert2.z = relativeZ;

            triangle.vert3.x = centerX + sin * length * 0.70f;
            triangle.vert3.y = centerY - cos * length * 0.70f;
            triangle.vert3.z = relativeZ;
        }

        private static ITriangleMeshWithColor CreateLeafTriangle(int index)
        {
            return new TriangleMeshWithColor
            {
                Color = FallbackLeafColors[index % FallbackLeafColors.Length],
                noHidden = true,
                angle = 1f,
                vert1 = new Vector3(),
                vert2 = new Vector3(),
                vert3 = new Vector3()
            };
        }

        private static float GetEndGuideYOffset()
        {
            var surfaceY = GameState.SurfaceState.SurfaceViewportObject?.ObjectOffsets?.y;
            if (surfaceY.HasValue)
                return surfaceY.Value - 15f;

            return ScreenSetup.screenSizeY * 0.46f;
        }

        private float RandomRange(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }

        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry) { }

        public void Dispose()
        {
            _leaves.Clear();
            _weatherField.Reset();
            _windPhase = 0f;
            _windX = BaseWindX;
        }

        public void ReleaseParticles(I3dObject theObject) { }
        public void SetParticleGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord) { }
        public void SetRearEngineGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord) { }
        public void SetWeaponGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord) { }

        private sealed class FallingLeaf
        {
            public float WorldX;
            public float OffsetY;
            public float WorldZ;
            public float Size;
            public float FallSpeed;
            public float DriftX;
            public float DriftZ;
            public float WindWeight;
            public float Phase;
            public float SwayAmount;
            public float SwaySpeed;
            public float VerticalSway;
            public float SpinSpeed;
            public float Angle;
        }
    }
}
