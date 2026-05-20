using CommonUtilities._3DHelpers;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using Domain;
using System;
using System.Collections.Generic;
using NumericsVector3 = System.Numerics.Vector3;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Controls
{
    public class OutroShipControls : IObjectMovement
    {
        private const float StartDepth = 260f;
        private const float TargetDepth = 1000f; // Depth creates perspective shrink; ZSortBias keeps the ship rendered in front.
        private const float TargetVisualScale = 0.25f;
        private const float ApproachSeconds = 3.0f;
        private const float DiveSeconds = 2.0f;
        private const float JourneySeconds = ApproachSeconds + DiveSeconds;
        private const float BasePitchX = 70f;
        private const float ApproachBankY = -24f;
        private const float DiveHeadingZ = 0f;
        private const float ImpactHoldSeconds = 0.5f;
        private const float ParticleEmissionIntervalSeconds = 2.0f;
        private const float StartScreenXRatio = 0.45f;
        private const float StartScreenYRatio = -0.10f;
        private const int EngineThrustPerNozzle = 5;
        private const float FrontOfEarthZSortBias = 500f;
        private const float OutroFadeOutSeconds = 1.5f;

        private static readonly Random FlickerRng = new(901);

        private readonly Func<DateTime> _utcNow;
        private DateTime _lastUpdateTime;
        private DateTime _lastParticleReleaseTime = DateTime.MinValue;
        private float _elapsedSeconds;
        private bool _hasInitialized;
        private bool _finished;
        private bool _audioConfigured;
        private bool _fadeRequested;

        private IAudioPlayer? _audio;
        private SoundDefinition? _rocketSound;
        private IAudioInstance? _rocketInstance;
        private List<List<BaselineTriangle>>? _baselineGeometry;
        private Vector3 _baselineCenter;

        public I3dObject? ParentObject { get; set; }
        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public ITriangleMeshWithColor? RearStartCoordinates { get; set; }
        public ITriangleMeshWithColor? RearGuideCoordinates { get; set; }
        public IPhysics Physics { get; set; } = new Physics.Physics();

        public OutroShipControls(Func<DateTime>? utcNow = null)
        {
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            _lastUpdateTime = DateTime.MinValue;
        }

        public static Vector3 CreateInitialOffset() => GetStartOffset();

        public static Vector3 CreateInitialRotation()
        {
            var start = GetStartOffset();
            var approachTarget = GetApproachTarget();

            return new Vector3
            {
                x = BasePitchX,
                y = ApproachBankY,
                z = GetShipNoseHeadingDegrees(approachTarget.x - start.x, approachTarget.y - start.y)
            };
        }

        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            ConfigureAudio(audioPlayer, soundRegistry);
            ParentObject = theObject;

            if (!_hasInitialized)
                InitializeShip(theObject);

            if (_finished)
            {
                HoldAtEarth(theObject);
                return theObject;
            }

            var now = _utcNow();
            float deltaTime = (float)(now - _lastUpdateTime).TotalSeconds;
            _lastUpdateTime = now;
            if (deltaTime <= 0f)
                deltaTime = 1f / 60f;

            _elapsedSeconds += deltaTime;

            var current = GetCurrentOffset();
            theObject.ObjectOffsets = current;
            theObject.WorldPosition = new Vector3 { x = 0f, y = 0f, z = 0f };
            theObject.Rotation = GetRotationForCurrentPhase();
            theObject.ZSortBias = FrontOfEarthZSortBias;
            ApplyCinematicScale(theObject);

            UpdateEngineGlow(theObject);
            EnsureRocketLoop(theObject);
            ReleaseParticles(theObject);

            if (!_fadeRequested && _elapsedSeconds >= JourneySeconds)
            {
                GameState.WorldFade.RequestFadeOut(OutroFadeOutSeconds, "OutroShipReachedEarth");
                _fadeRequested = true;
            }

            if (_elapsedSeconds >= JourneySeconds + ImpactHoldSeconds)
            {
                StopRocket(playEndSegment: true);
                _finished = true;
            }

            return theObject;
        }

        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            if (_audioConfigured)
                return;

            if (audioPlayer == null || soundRegistry == null)
                return;

            _audio = audioPlayer;
            if (soundRegistry.TryGet("rocket_main", out var rocketSound))
                _rocketSound = rocketSound;

            _audioConfigured = true;
        }

        public void ReleaseParticles(I3dObject theObject)
        {
            if (theObject.Particles == null)
                return;

            ConfigureParticleStyle(theObject);

            if (StartCoordinates == null || GuideCoordinates == null ||
                RearStartCoordinates == null || RearGuideCoordinates == null)
            {
                theObject.Particles.MoveParticles();
                return;
            }

            var now = _lastUpdateTime == DateTime.MinValue ? _utcNow() : _lastUpdateTime;
            bool shouldEmit = _lastParticleReleaseTime == DateTime.MinValue ||
                (now - _lastParticleReleaseTime).TotalSeconds >= ParticleEmissionIntervalSeconds;

            if (shouldEmit)
            {
                var worldPosition = new Vector3 { x = 0f, y = 0f, z = 0f };
                theObject.Particles.ReleaseParticles(
                    GuideCoordinates,
                    StartCoordinates,
                    worldPosition,
                    this,
                    EngineThrustPerNozzle,
                    false);
                theObject.Particles.ReleaseParticles(
                    RearGuideCoordinates,
                    RearStartCoordinates,
                    worldPosition,
                    this,
                    EngineThrustPerNozzle,
                    false);
                _lastParticleReleaseTime = now;
            }

            theObject.Particles.MoveParticles();
        }

        public void SetParticleGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            if (StartCoord != null) StartCoordinates = StartCoord;
            if (GuideCoord != null) GuideCoordinates = GuideCoord;
        }

        public void SetRearEngineGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            if (StartCoord != null) RearStartCoordinates = StartCoord;
            if (GuideCoord != null) RearGuideCoordinates = GuideCoord;
        }

        public void SetWeaponGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord) { }

        public void Dispose()
        {
            StopRocket(playEndSegment: false);
            _finished = true;
        }

        private void InitializeShip(I3dObject theObject)
        {
            _hasInitialized = true;
            _lastUpdateTime = _utcNow();
            ConfigureParticleStyle(theObject);
            CaptureBaselineGeometry(theObject);
            theObject.IsActive = true;
            theObject.ObjectOffsets = CreateInitialOffset();
            theObject.WorldPosition = new Vector3 { x = 0f, y = 0f, z = 0f };
            theObject.Rotation = CreateInitialRotation();
            theObject.ZSortBias = FrontOfEarthZSortBias;
            ApplyCinematicScale(theObject);
            UpdateEngineGlow(theObject);
        }

        private static void ConfigureParticleStyle(I3dObject theObject)
        {
            if (theObject.Particles is not ParticlesAI particles)
                return;

            particles.MaxParticlesOverride = 120;
            particles.LifeMultiplier = 0.85f;
            particles.ColorStartOverride = "fff6a0";
            particles.ColorMidOverride = "ff7a18";
            particles.ColorEndOverride = "6b1600";
        }

        private Vector3 GetCurrentOffset()
        {
            var start = GetStartOffset();
            var approachTarget = GetApproachTarget();

            if (_elapsedSeconds <= ApproachSeconds)
            {
                float approachProgress = SmoothStep(Math.Clamp(_elapsedSeconds / ApproachSeconds, 0f, 1f));
                return new Vector3
                {
                    x = Lerp(start.x, approachTarget.x, approachProgress),
                    y = Lerp(start.y, approachTarget.y, approachProgress),
                    z = StartDepth
                };
            }

            var diveTarget = GetDiveTarget();
            float diveProgress = GetDiveProgress();

            return new Vector3
            {
                x = approachTarget.x,
                y = approachTarget.y,
                z = Lerp(StartDepth, diveTarget.z, diveProgress)
            };
        }

        private static Vector3 GetStartOffset()
        {
            return new Vector3
            {
                x = ScreenSetup.screenSizeX * StartScreenXRatio,
                y = ScreenSetup.screenSizeY * StartScreenYRatio,
                z = StartDepth
            };
        }

        private static Vector3 GetDiveTarget()
        {
            return new Vector3
            {
                x = 0f,
                y = 0f,
                z = TargetDepth
            };
        }

        private static Vector3 GetApproachTarget()
        {
            return new Vector3
            {
                x = 0f,
                y = 0f,
                z = StartDepth
            };
        }

        private Vector3 GetRotationForCurrentPhase(bool forceInitialTurn = false)
        {
            var initial = CreateInitialRotation();

            if (forceInitialTurn || _elapsedSeconds <= ApproachSeconds)
                return initial;

            float diveProgress = GetDiveProgress();
            return new Vector3
            {
                x = BasePitchX,
                y = Lerp(ApproachBankY, 0f, diveProgress),
                z = Lerp(initial.z, DiveHeadingZ, diveProgress)
            };
        }

        private static float GetShipNoseHeadingDegrees(float dx, float dy)
        {
            if (MathF.Abs(dx) < 0.001f && MathF.Abs(dy) < 0.001f)
                return 0f;

            return MathF.Atan2(dx, -dy) * 180f / MathF.PI;
        }

        private void HoldAtEarth(I3dObject theObject)
        {
            theObject.ObjectOffsets = GetDiveTarget();
            theObject.WorldPosition = new Vector3 { x = 0f, y = 0f, z = 0f };
            theObject.Rotation = GetRotationForCurrentPhase();
            theObject.ZSortBias = FrontOfEarthZSortBias;
            ApplyCinematicScale(theObject);
            UpdateEngineGlow(theObject);
            theObject.Particles?.MoveParticles();
        }

        private void EnsureRocketLoop(I3dObject theObject)
        {
            if (_audio == null || _rocketSound == null || theObject is not _3dObject concrete)
                return;

            var audioPosition = concrete.GetAudioPosition();
            var numericsPosition = new NumericsVector3(audioPosition.x, audioPosition.y, audioPosition.z);

            if (_rocketInstance == null || !_rocketInstance.IsPlaying)
            {
                _rocketInstance = _audio.Play(
                    _rocketSound,
                    AudioPlayMode.SegmentedLoop,
                    new AudioPlayOptions
                    {
                        VolumeOverride = _rocketSound.Settings.Volume,
                        SpeedOverride = Math.Clamp(_rocketSound.Speed.Base * 1.08f, _rocketSound.Speed.Min, _rocketSound.Speed.Max),
                        WorldPosition = numericsPosition
                    });
                return;
            }

            _rocketInstance.SetWorldPosition(numericsPosition);
        }

        private void StopRocket(bool playEndSegment)
        {
            if (_rocketInstance == null)
                return;

            _rocketInstance.Stop(playEndSegment);
            _rocketInstance = null;
        }

        private static void UpdateEngineGlow(I3dObject theObject)
        {
            string color = GetEngineGlowColor();
            SetPartColor(theObject, "JetMotor", color);
            SetPartColor(theObject, "RearEngine", color);
        }

        private static string GetEngineGlowColor()
        {
            float flicker = (float)FlickerRng.NextDouble() * 0.16f;
            int g = 210 + (int)MathF.Round(flicker * 255f);
            g = Math.Clamp(g, 210, 255);
            return $"FF{g:X2}00";
        }

        private static void SetPartColor(I3dObject theObject, string partName, string color)
        {
            foreach (var part in theObject.ObjectParts)
            {
                if (part.PartName != partName)
                    continue;

                for (int i = 0; i < part.Triangles.Count; i++)
                {
                    var tri = part.Triangles[i];
                    tri.Color = color;
                    part.Triangles[i] = tri;
                }

                return;
            }
        }

        private static float SmoothStep(float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        private static float Lerp(float from, float to, float amount) => from + (to - from) * amount;

        private void CaptureBaselineGeometry(I3dObject theObject)
        {
            if (_baselineGeometry != null)
                return;

            _baselineGeometry = new List<List<BaselineTriangle>>(theObject.ObjectParts.Count);
            float sumX = 0f;
            float sumY = 0f;
            float sumZ = 0f;
            int vertexCount = 0;

            foreach (var part in theObject.ObjectParts)
            {
                var partBaseline = new List<BaselineTriangle>(part.Triangles.Count);
                foreach (var triangle in part.Triangles)
                {
                    var baseline = new BaselineTriangle
                    {
                        V1 = CopyVector(triangle.vert1),
                        V2 = CopyVector(triangle.vert2),
                        V3 = CopyVector(triangle.vert3)
                    };

                    partBaseline.Add(baseline);
                    AddToCenterSum(baseline.V1, ref sumX, ref sumY, ref sumZ, ref vertexCount);
                    AddToCenterSum(baseline.V2, ref sumX, ref sumY, ref sumZ, ref vertexCount);
                    AddToCenterSum(baseline.V3, ref sumX, ref sumY, ref sumZ, ref vertexCount);
                }

                _baselineGeometry.Add(partBaseline);
            }

            if (vertexCount > 0)
            {
                _baselineCenter = new Vector3
                {
                    x = sumX / vertexCount,
                    y = sumY / vertexCount,
                    z = sumZ / vertexCount
                };
            }
        }

        private void ApplyCinematicScale(I3dObject theObject)
        {
            if (_baselineGeometry == null)
                return;

            float scale = Lerp(1f, TargetVisualScale, GetDiveProgress());

            int partCount = Math.Min(theObject.ObjectParts.Count, _baselineGeometry.Count);
            for (int partIndex = 0; partIndex < partCount; partIndex++)
            {
                var part = theObject.ObjectParts[partIndex];
                var baselinePart = _baselineGeometry[partIndex];
                int triangleCount = Math.Min(part.Triangles.Count, baselinePart.Count);

                for (int triangleIndex = 0; triangleIndex < triangleCount; triangleIndex++)
                {
                    var triangle = part.Triangles[triangleIndex];
                    var baseline = baselinePart[triangleIndex];
                    triangle.vert1 = ScaleFromBaseline(baseline.V1, scale);
                    triangle.vert2 = ScaleFromBaseline(baseline.V2, scale);
                    triangle.vert3 = ScaleFromBaseline(baseline.V3, scale);
                    part.Triangles[triangleIndex] = triangle;
                }
            }
        }

        private Vector3 ScaleFromBaseline(Vector3 vertex, float scale)
        {
            return new Vector3
            {
                x = _baselineCenter.x + ((vertex.x - _baselineCenter.x) * scale),
                y = _baselineCenter.y + ((vertex.y - _baselineCenter.y) * scale),
                z = _baselineCenter.z + ((vertex.z - _baselineCenter.z) * scale)
            };
        }

        private float GetDiveProgress()
        {
            return SmoothStep(Math.Clamp((_elapsedSeconds - ApproachSeconds) / DiveSeconds, 0f, 1f));
        }

        private static Vector3 CopyVector(IVector3 vector)
        {
            return new Vector3 { x = vector.x, y = vector.y, z = vector.z };
        }

        private static void AddToCenterSum(Vector3 vertex, ref float sumX, ref float sumY, ref float sumZ, ref int count)
        {
            sumX += vertex.x;
            sumY += vertex.y;
            sumZ += vertex.z;
            count++;
        }

        private sealed class BaselineTriangle
        {
            public Vector3 V1 { get; init; }
            public Vector3 V2 { get; init; }
            public Vector3 V3 { get; init; }
        }
    }
}
