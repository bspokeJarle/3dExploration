using CommonUtilities._3DHelpers;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Controls
{
    public sealed class SnowfallControls : IObjectMovement
    {
        public const int VisibleFlakeTarget = 150;
        private const int OffscreenFlakeReserve = 250;
        public const int TargetFlakeCount = VisibleFlakeTarget + OffscreenFlakeReserve;
        public const float StartGuideYOffset = -1000f;
        public const float DepthSpread = 1000f;

        private const string SnowColor = "ffffff";
        private const float DepthStartZ = 750f;
        private const float DepthBehindSpread = 1800f;
        private const float DepthAheadSpread = 3400f;
        private const float MinSize = 1.5f;
        private const float MaxSize = 3.3f;
        private const float MinFallSpeed = 1.2f;
        private const float MaxFallSpeed = 3.0f;
        private const float HorizontalDrift = 0.28f;
        private const float DepthDrift = 0.18f;
        private const float TopRespawnJitter = 180f;
        private const float OffscreenMargin = 240f;
        private const float DirectionalSpawnAheadMin = 900f;
        private const float DirectionalSpawnAheadMax = 3000f;
        private const float DirectionalLateralSpreadFactor = 0.88f;
        private const float TravelBehindRecycleDistance = 2200f;
        private const float TravelAheadRecycleDistance = 3800f;
        private const int DirectionalSpawnModulo = 5;

        private readonly Random _random = new();
        private readonly List<Snowflake> _flakes = new(TargetFlakeCount);
        private bool _hasLastMapPosition;
        private bool _isMoving;
        private bool _hasTravelDirection;
        private float _lastMapX;
        private float _lastMapZ;
        private float _travelX;
        private float _travelZ;
        private int _spawnSequence;

        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public I3dObject? ParentObject { get; set; }
        public IPhysics Physics { get; set; } = new Physics.Physics();

        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            ParentObject = theObject;

            var mapPosition = GameState.SurfaceState.GlobalMapPosition;
            SyncEmitterToGround(theObject, mapPosition);
            UpdateTravelDirection(mapPosition);
            float endOffsetY = GetEndGuideYOffset();
            EnsureFlakes(mapPosition, endOffsetY);
            EnsureTriangleBuffer(theObject);

            var triangles = GetSnowflakePart(theObject).Triangles;
            float halfWorldSpread = GetHalfWorldSpread();
            float objectZ = theObject.ObjectOffsets?.z ?? 0f;

            for (int i = 0; i < _flakes.Count; i++)
            {
                var flake = _flakes[i];
                MoveFlake(flake);

                if (ShouldRecycle(flake, mapPosition, endOffsetY, halfWorldSpread))
                    ResetFlakeAtTop(flake, mapPosition, halfWorldSpread, ShouldSpawnAhead());

                WriteTriangle(triangles[i], flake, mapPosition, objectZ);
            }

            return theObject;
        }

        private void EnsureFlakes(IVector3 mapPosition, float endOffsetY)
        {
            if (_flakes.Count == TargetFlakeCount)
                return;

            _flakes.Clear();
            float halfVisibleSpread = GetHalfScreenSpread();
            float halfWorldSpread = GetHalfWorldSpread();
            for (int i = 0; i < VisibleFlakeTarget; i++)
            {
                _flakes.Add(CreateVisibleFlake(
                    mapPosition,
                    RandomRange(-halfVisibleSpread, halfVisibleSpread),
                    RandomRange(StartGuideYOffset, endOffsetY)));
            }

            for (int i = VisibleFlakeTarget; i < TargetFlakeCount; i++)
                _flakes.Add(CreateReserveFlake(mapPosition, halfWorldSpread));
        }

        private void EnsureTriangleBuffer(I3dObject theObject)
        {
            var snowflakePart = GetSnowflakePart(theObject);
            if (snowflakePart.Triangles.Count == TargetFlakeCount)
                return;

            snowflakePart.Triangles.Clear();
            for (int i = 0; i < TargetFlakeCount; i++)
                snowflakePart.Triangles.Add(CreateSnowTriangle());
        }

        private static I3dObjectPart GetSnowflakePart(I3dObject theObject)
        {
            var part = theObject.ObjectParts.FirstOrDefault(p => p.PartName == "Snowflakes");
            if (part != null)
                return part;

            part = new _3dObjectPart
            {
                PartName = "Snowflakes",
                IsVisible = true
            };
            theObject.ObjectParts.Add(part);
            return part;
        }

        private static void MoveFlake(Snowflake flake)
        {
            flake.Phase += flake.SwaySpeed;
            flake.WorldX += flake.DriftX + MathF.Sin(flake.Phase) * flake.SwayAmount;
            flake.OffsetY += flake.FallSpeed;
            flake.WorldZ += flake.DriftZ;
        }

        private bool ShouldRecycle(Snowflake flake, IVector3 mapPosition, float endOffsetY, float halfWorldSpread)
        {
            float relativeX = flake.WorldX - mapPosition.x;
            float relativeZ = flake.WorldZ - mapPosition.z;

            if (flake.OffsetY > endOffsetY)
                return true;
            if (MathF.Abs(relativeX) > halfWorldSpread + OffscreenMargin)
                return true;
            if (relativeZ < -DepthBehindSpread - OffscreenMargin)
                return true;
            if (relativeZ > DepthStartZ + DepthSpread + DepthAheadSpread + OffscreenMargin)
                return true;

            if (!_isMoving || !_hasTravelDirection)
                return false;

            float alongTravel = relativeX * _travelX + relativeZ * _travelZ;
            float lateral = MathF.Abs(relativeX * -_travelZ + relativeZ * _travelX);

            return alongTravel < -TravelBehindRecycleDistance
                || alongTravel > TravelAheadRecycleDistance
                || lateral > halfWorldSpread + OffscreenMargin;
        }

        private void ResetFlakeAtTop(Snowflake flake, IVector3 mapPosition, float halfWorldSpread, bool preferAhead)
        {
            var spawn = preferAhead && _hasTravelDirection
                ? GetDirectionalSpawnOffset(halfWorldSpread)
                : GetAmbientSpawnOffset(halfWorldSpread, preferOutside: true);

            flake.WorldX = mapPosition.x + spawn.x;
            flake.OffsetY = StartGuideYOffset - RandomRange(0f, TopRespawnJitter);
            flake.WorldZ = mapPosition.z + spawn.z;
            flake.Size = RandomRange(MinSize, MaxSize);
            flake.FallSpeed = RandomRange(MinFallSpeed, MaxFallSpeed);
            flake.DriftX = RandomRange(-HorizontalDrift, HorizontalDrift);
            flake.DriftZ = RandomRange(-DepthDrift, DepthDrift);
            flake.SwayAmount = RandomRange(0.05f, 0.22f);
            flake.SwaySpeed = RandomRange(0.012f, 0.035f);
            flake.Phase = RandomRange(0f, MathF.PI * 2f);
        }

        private Snowflake CreateVisibleFlake(IVector3 mapPosition, float offsetX, float offsetY)
        {
            var flake = new Snowflake();
            ResetFlakeAtTop(flake, mapPosition, GetHalfWorldSpread(), preferAhead: false);
            flake.WorldX = mapPosition.x + offsetX;
            flake.OffsetY = offsetY;
            flake.WorldZ = mapPosition.z + RandomRange(DepthStartZ, DepthStartZ + DepthSpread);
            return flake;
        }

        private Snowflake CreateReserveFlake(IVector3 mapPosition, float halfWorldSpread)
        {
            var flake = new Snowflake();
            ResetFlakeAtTop(flake, mapPosition, halfWorldSpread, preferAhead: false);
            flake.OffsetY = RandomRange(StartGuideYOffset, GetEndGuideYOffset());
            return flake;
        }

        private (float x, float z) GetAmbientSpawnOffset(float halfWorldSpread, bool preferOutside)
        {
            if (preferOutside && _random.NextDouble() < 0.65d)
            {
                if (_random.NextDouble() < 0.5d)
                {
                    float sign = _random.NextDouble() < 0.5d ? -1f : 1f;
                    return (
                        sign * RandomRange(GetHalfScreenSpread(), halfWorldSpread),
                        RandomRange(-DepthBehindSpread, DepthStartZ + DepthSpread + DepthAheadSpread));
                }

                float z = _random.NextDouble() < 0.5d
                    ? RandomRange(-DepthBehindSpread, DepthStartZ)
                    : RandomRange(DepthStartZ + DepthSpread, DepthStartZ + DepthSpread + DepthAheadSpread);

                return (RandomRange(-halfWorldSpread, halfWorldSpread), z);
            }

            return (
                RandomRange(-GetHalfScreenSpread(), GetHalfScreenSpread()),
                RandomRange(DepthStartZ, DepthStartZ + DepthSpread));
        }

        private (float x, float z) GetDirectionalSpawnOffset(float halfWorldSpread)
        {
            float forward = RandomRange(DirectionalSpawnAheadMin, DirectionalSpawnAheadMax);
            float lateral = RandomRange(-halfWorldSpread * DirectionalLateralSpreadFactor, halfWorldSpread * DirectionalLateralSpreadFactor);

            float x = _travelX * forward + -_travelZ * lateral;
            float z = _travelZ * forward + _travelX * lateral;

            return (
                x,
                Math.Clamp(z, -DepthBehindSpread, DepthStartZ + DepthSpread + DepthAheadSpread));
        }

        private static void WriteTriangle(ITriangleMeshWithColor triangle, Snowflake flake, IVector3 mapPosition, float objectZ)
        {
            float relativeX = flake.WorldX - mapPosition.x;
            float relativeZ = flake.WorldZ - mapPosition.z;

            float scale = GetProjectionScale(relativeZ, objectZ);
            float centerX = relativeX / scale;
            float centerY = flake.OffsetY / scale;
            float halfSize = flake.Size;

            triangle.Color = SnowColor;
            triangle.noHidden = true;
            triangle.angle = 1f;

            triangle.vert1.x = centerX - halfSize;
            triangle.vert1.y = centerY - halfSize;
            triangle.vert1.z = relativeZ;

            triangle.vert2.x = centerX + halfSize;
            triangle.vert2.y = centerY;
            triangle.vert2.z = relativeZ;

            triangle.vert3.x = centerX - halfSize * 0.2f;
            triangle.vert3.y = centerY + halfSize;
            triangle.vert3.z = relativeZ;
        }

        private static ITriangleMeshWithColor CreateSnowTriangle()
        {
            return new TriangleMeshWithColor
            {
                Color = SnowColor,
                noHidden = true,
                angle = 1f,
                vert1 = new Vector3(),
                vert2 = new Vector3(),
                vert3 = new Vector3()
            };
        }

        private static float GetProjectionScale(float z, float objectZ)
        {
            float denominator = -z + objectZ + ScreenSetup.perspectiveAdjustment;
            if (denominator <= 1f)
                denominator = 1f;

            return ScreenSetup.perspectiveAdjustment / denominator * ScreenSetup.defaultObjectZoom;
        }

        private static float GetHalfScreenSpread() => ScreenSetup.screenSizeX * 0.58f;

        private static float GetHalfWorldSpread() => ScreenSetup.screenSizeX * 2.4f;

        private static float GetEndGuideYOffset()
        {
            var surfaceY = GameState.SurfaceState.SurfaceViewportObject?.ObjectOffsets?.y;
            if (surfaceY.HasValue)
                return surfaceY.Value - 30f;

            return ScreenSetup.screenSizeY * 0.46f;
        }

        private float RandomRange(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }

        private static void SyncEmitterToGround(I3dObject theObject, IVector3 mapPosition)
        {
            var offsets = theObject.ObjectOffsets ?? new Vector3();
            theObject.ObjectOffsets = new Vector3
            {
                x = offsets.x,
                y = mapPosition.y * SurfacePositionSyncHelpers.DefaultEnemySurfaceSyncFactorY,
                z = offsets.z
            };
        }

        private bool ShouldSpawnAhead()
        {
            if (!_isMoving || !_hasTravelDirection)
                return false;

            _spawnSequence++;
            return _spawnSequence % DirectionalSpawnModulo != 0;
        }

        private void UpdateTravelDirection(IVector3 mapPosition)
        {
            if (!_hasLastMapPosition)
            {
                _lastMapX = mapPosition.x;
                _lastMapZ = mapPosition.z;
                _hasLastMapPosition = true;
                return;
            }

            float dx = mapPosition.x - _lastMapX;
            float dz = mapPosition.z - _lastMapZ;
            _lastMapX = mapPosition.x;
            _lastMapZ = mapPosition.z;

            float distance = MathF.Sqrt(dx * dx + dz * dz);
            if (distance <= 0.05f)
            {
                _isMoving = false;
                return;
            }

            _travelX = dx / distance;
            _travelZ = dz / distance;
            _isMoving = true;
            _hasTravelDirection = true;
        }

        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry) { }
        public void Dispose()
        {
            _flakes.Clear();
            _hasLastMapPosition = false;
            _isMoving = false;
            _hasTravelDirection = false;
            _spawnSequence = 0;
        }
        public void ReleaseParticles(I3dObject theObject) { }
        public void SetParticleGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord) { }
        public void SetRearEngineGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord) { }
        public void SetWeaponGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord) { }

        private sealed class Snowflake
        {
            public float WorldX;
            public float OffsetY;
            public float WorldZ;
            public float Size;
            public float FallSpeed;
            public float DriftX;
            public float DriftZ;
            public float Phase;
            public float SwayAmount;
            public float SwaySpeed;
        }
    }
}
