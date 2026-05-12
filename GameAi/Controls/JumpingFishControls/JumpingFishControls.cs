using CommonUtilities._3DHelpers;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using Domain;
using GameAiAndControls.Helpers;
using System;
using System.Collections.Generic;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Controls.JumpingFishControls
{
    public class JumpingFishControls : IObjectMovement
    {
        private const float BaseXRotation = 70f;
        private const float BaseYRotation = 0f;
        private const int InitialJumpDirection = -1;
        private const float StartZRotation = -90f;
        private const float DefaultJumpHorizontalSpan = 260f;
        private const float JumpRotationDegrees = 180f;
        private const float JumpDurationSeconds = 2.0f;
        private const float JumpHeight = 170f;
        private const float JumpDepthPulse = 18f;
        private const float TwistAmplitude = 24f;
        private const float ApexXRotationLift = 8f;
        private const float LandingSplashPhase = 0.94f;
        private const int SplashParticleThrust = 3;
        private const float SplashUpwardVelocityBoost = 4.5f;
        private const float SplashSurfaceLift = -12f;
        private const int TailFrameCount = 6;
        private const float TailFramesPerSecond = 14f;
        private const float FinRadiansPerSecond = 11.5f;
        private const float PectoralFinAmplitude = 26f;

        private const float LeftFinPivotY = -7.2f;
        private const float RightFinPivotY = 7.2f;
        private const float FinPivotZ = -1.6f;

        private readonly _3dRotationCommon _rotate = new();
        private readonly Dictionary<string, List<ITriangleMeshWithColor>> _baseTrianglesByPart = new();
        private readonly float _jumpHorizontalSpan;
        private readonly bool _hasPathBounds;
        private readonly float _minPathOffsetX;
        private readonly float _maxPathOffsetX;
        private readonly int _initialJumpDirection;

        private DateTime _lastFrameTime = DateTime.MinValue;
        private float _jumpTimeSeconds;
        private float _baseOffsetX;
        private float _baseOffsetY;
        private float _baseOffsetZ;
        private bool _baseInitialized;
        private bool _firstPoseApplied;
        private bool _takeoffSplashReleased;
        private bool _landingSplashReleased;
        private bool _particleAnchorInitialized;
        private Vector3 _particleAnchorOffsets = new();
        private int _jumpDirection = InitialJumpDirection;

        public ITriangleMeshWithColor? StartCoordinates { get; set; }
        public ITriangleMeshWithColor? GuideCoordinates { get; set; }
        public IPhysics Physics { get; set; } = new Physics.Physics();

        public JumpingFishControls()
            : this(DefaultJumpHorizontalSpan)
        {
        }

        public JumpingFishControls(float jumpHorizontalSpan)
            : this(jumpHorizontalSpan, 0f, 0f, InitialJumpDirection, hasPathBounds: false)
        {
        }

        public JumpingFishControls(float jumpHorizontalSpan, float minPathOffsetX, float maxPathOffsetX, int initialJumpDirection = InitialJumpDirection)
            : this(jumpHorizontalSpan, minPathOffsetX, maxPathOffsetX, initialJumpDirection, hasPathBounds: true)
        {
        }

        private JumpingFishControls(float jumpHorizontalSpan, float minPathOffsetX, float maxPathOffsetX, int initialJumpDirection, bool hasPathBounds)
        {
            _jumpHorizontalSpan = Math.Max(75f, jumpHorizontalSpan);
            _minPathOffsetX = Math.Min(minPathOffsetX, maxPathOffsetX);
            _maxPathOffsetX = Math.Max(minPathOffsetX, maxPathOffsetX);
            _initialJumpDirection = initialJumpDirection < 0 ? -1 : 1;
            _jumpDirection = _initialJumpDirection;
            _hasPathBounds = hasPathBounds && (_maxPathOffsetX - _minPathOffsetX) > _jumpHorizontalSpan;
        }

        public I3dObject MoveObject(I3dObject theObject, IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
            if (!theObject.IsOnScreen)
                return theObject;

            InitializeBaseGeometry(theObject);

            float deltaSeconds = GetDeltaSeconds();
            if (_firstPoseApplied)
            {
                bool wrapped = AdvanceJump(deltaSeconds);
                if (wrapped)
                {
                    AdvanceToNextJumpSegment();
                    _takeoffSplashReleased = false;
                    _landingSplashReleased = false;
                }
            }
            else
            {
                _firstPoseApplied = true;
                _takeoffSplashReleased = false;
                _landingSplashReleased = false;
            }

            ApplyJumpPose(theObject);
            AnchorExistingSplashParticles(theObject);
            if (theObject.IsOnScreen)
            {
                ReleaseSplashParticles(theObject);
                AnimateTailFrames(theObject);
                AnimatePectoralFins(theObject);
                ExplosionParticleHelpers.MoveParticles(theObject);
            }

            SyncToOriginal(theObject);

            return theObject;
        }

        private float GetDeltaSeconds()
        {
            if (GameState.DeltaTime > 0f)
                return Math.Clamp(GameState.DeltaTime, 0f, 0.1f);

            var now = DateTime.Now;
            float deltaSeconds = 1f / ScreenSetup.targetFps;

            if (_lastFrameTime != DateTime.MinValue)
                deltaSeconds = (float)(now - _lastFrameTime).TotalSeconds;

            _lastFrameTime = now;
            return Math.Clamp(deltaSeconds, 0f, 0.1f);
        }

        private bool AdvanceJump(float deltaSeconds)
        {
            _jumpTimeSeconds += deltaSeconds;
            bool wrapped = false;
            while (_jumpTimeSeconds > JumpDurationSeconds)
            {
                _jumpTimeSeconds -= JumpDurationSeconds;
                wrapped = true;
            }

            return wrapped;
        }

        private void AdvanceToNextJumpSegment()
        {
            if (!_hasPathBounds)
            {
                _jumpDirection *= -1;
                return;
            }

            float nextBaseOffsetX = _baseOffsetX + (_jumpDirection * _jumpHorizontalSpan);
            if (CanFitJumpSegment(nextBaseOffsetX))
            {
                _baseOffsetX = nextBaseOffsetX;
                return;
            }

            float currentLandingX = _baseOffsetX + (_jumpDirection * _jumpHorizontalSpan * 0.5f);
            _jumpDirection *= -1;
            _baseOffsetX = currentLandingX + (_jumpDirection * _jumpHorizontalSpan * 0.5f);
        }

        private bool CanFitJumpSegment(float baseOffsetX)
        {
            float halfSpan = _jumpHorizontalSpan * 0.5f;
            return baseOffsetX - halfSpan >= _minPathOffsetX &&
                   baseOffsetX + halfSpan <= _maxPathOffsetX;
        }

        private void ApplyJumpPose(I3dObject theObject)
        {
            float phase = GetJumpPhase();
            float easedPhase = SmoothStep(phase);
            float arc = MathF.Sin(phase * MathF.PI);

            if (theObject.ObjectOffsets != null)
            {
                theObject.ObjectOffsets = new Vector3
                {
                    x = _baseOffsetX + (_jumpDirection * _jumpHorizontalSpan * (easedPhase - 0.5f)),
                    y = _baseOffsetY - (JumpHeight * arc),
                    z = _baseOffsetZ + (JumpDepthPulse * arc)
                };
            }

            if (theObject.Rotation != null)
            {
                theObject.Rotation.x = BaseXRotation + (ApexXRotationLift * arc);
                theObject.Rotation.y = BaseYRotation - (_jumpDirection * TwistAmplitude * arc);
                theObject.Rotation.z = StartZRotation + (_jumpDirection * JumpRotationDegrees * easedPhase);
            }
        }

        private float GetJumpPhase()
        {
            if (JumpDurationSeconds <= 0f)
                return 0f;

            return Math.Clamp(_jumpTimeSeconds / JumpDurationSeconds, 0f, 1f);
        }

        private static float SmoothStep(float value)
        {
            value = Math.Clamp(value, 0f, 1f);
            return value * value * (3f - (2f * value));
        }

        private void AnimateTailFrames(I3dObject theObject)
        {
            int frame = (int)(_jumpTimeSeconds * TailFramesPerSecond) % TailFrameCount;
            if (frame < 0) frame += TailFrameCount;

            foreach (var part in theObject.ObjectParts)
            {
                if (part.PartName == null)
                    continue;

                if (TryGetTailFrameIndex(part.PartName, out int partFrame))
                    part.IsVisible = partFrame == frame;
            }
        }

        private void AnimatePectoralFins(I3dObject theObject)
        {
            float finAngle = MathF.Sin((_jumpTimeSeconds * FinRadiansPerSecond) + 0.35f) * PectoralFinAmplitude;

            ApplyFinAnimation(theObject, "LeftPectoralFin", LeftFinPivotY, finAngle);
            ApplyFinAnimation(theObject, "RightPectoralFin", RightFinPivotY, -finAngle);
        }

        private void ReleaseSplashParticles(I3dObject theObject)
        {
            float phase = GetJumpPhase();
            if (!_takeoffSplashReleased && phase <= 0.05f)
            {
                ReleaseBlueExplosionParticles(theObject, landingSplash: false);
                _takeoffSplashReleased = true;
            }

            if (!_landingSplashReleased && phase >= LandingSplashPhase)
            {
                ReleaseBlueExplosionParticles(theObject, landingSplash: true);
                _landingSplashReleased = true;
            }
        }

        private void ReleaseBlueExplosionParticles(I3dObject theObject, bool landingSplash)
        {
            if (theObject.Particles == null)
                return;

            var start = CreateSplashPointTriangle(GetSplashLocalPoint(theObject));
            var guide = CreateSplashPointTriangle(new Vector3
            {
                x = start.vert1.x,
                y = start.vert1.y - 1f,
                z = start.vert1.z
            });

            if (theObject.Particles is ParticlesAI particles)
            {
                string? previousStart = particles.ColorStartOverride;
                string? previousMid = particles.ColorMidOverride;
                string? previousEnd = particles.ColorEndOverride;
                float previousExplosionStartYOffset = particles.ExplosionStartYOffset;

                particles.ColorStartOverride = "d8f6ff";
                particles.ColorMidOverride = "3db7ff";
                particles.ColorEndOverride = "114cbb";
                particles.ExplosionStartYOffset = 0f;
                particles.ReleaseParticles(
                    guide,
                    start,
                    ToVector3(theObject.WorldPosition),
                    this,
                    SplashParticleThrust,
                    true,
                    SplashUpwardVelocityBoost);
                particles.ColorStartOverride = previousStart;
                particles.ColorMidOverride = previousMid;
                particles.ColorEndOverride = previousEnd;
                particles.ExplosionStartYOffset = previousExplosionStartYOffset;
                return;
            }

            theObject.Particles.ReleaseParticles(
                guide,
                start,
                ToVector3(theObject.WorldPosition),
                this,
                SplashParticleThrust,
                true,
                SplashUpwardVelocityBoost);
        }

        private void AnchorExistingSplashParticles(I3dObject theObject)
        {
            var offsets = theObject.ObjectOffsets;
            if (offsets == null)
                return;

            if (!_particleAnchorInitialized)
            {
                _particleAnchorOffsets = ToVector3(offsets);
                _particleAnchorInitialized = true;
                return;
            }

            float deltaX = _particleAnchorOffsets.x - offsets.x;
            float deltaY = _particleAnchorOffsets.y - offsets.y;
            float deltaZ = _particleAnchorOffsets.z - offsets.z;
            if (MathF.Abs(deltaX) < 0.001f && MathF.Abs(deltaY) < 0.001f && MathF.Abs(deltaZ) < 0.001f)
            {
                _particleAnchorOffsets = ToVector3(offsets);
                return;
            }

            var particles = theObject.Particles?.Particles;
            if (particles != null)
            {
                foreach (var particle in particles)
                {
                    if (particle.Position == null)
                        continue;

                    particle.Position.x += deltaX;
                    particle.Position.y += deltaY;
                    particle.Position.z += deltaZ;
                }
            }

            _particleAnchorOffsets = ToVector3(offsets);
        }

        private Vector3 GetSplashLocalPoint(I3dObject theObject)
        {
            var offsets = theObject.ObjectOffsets;

            if (offsets == null)
                return new Vector3 { y = SplashSurfaceLift };

            return new Vector3
            {
                x = 0f,
                y = _baseOffsetY - offsets.y + SplashSurfaceLift,
                z = _baseOffsetZ - offsets.z
            };
        }

        private static ITriangleMeshWithColor CreateSplashPointTriangle(Vector3 point)
        {
            return new TriangleMeshWithColor
            {
                Color = "d8f6ff",
                noHidden = true,
                vert1 = new Vector3 { x = point.x, y = point.y, z = point.z },
                vert2 = new Vector3 { x = point.x, y = point.y, z = point.z },
                vert3 = new Vector3 { x = point.x, y = point.y, z = point.z }
            };
        }

        private static Vector3 ToVector3(IVector3? vector)
        {
            if (vector == null) return new Vector3();
            return new Vector3 { x = vector.x, y = vector.y, z = vector.z };
        }

        private static bool TryGetTailFrameIndex(string partName, out int frame)
        {
            const string tailBasePrefix = "TailBase_Frame";
            const string tailTipPrefix = "TailTip_Frame";

            frame = -1;
            string? frameText = null;
            if (partName.StartsWith(tailBasePrefix, StringComparison.Ordinal))
                frameText = partName.Substring(tailBasePrefix.Length);
            else if (partName.StartsWith(tailTipPrefix, StringComparison.Ordinal))
                frameText = partName.Substring(tailTipPrefix.Length);

            return frameText != null && int.TryParse(frameText, out frame);
        }

        private void ApplyFinAnimation(I3dObject theObject, string partName, float pivotY, float angleDegrees)
        {
            var part = theObject.ObjectParts.Find(p => p.PartName == partName);
            if (part == null) return;
            if (!_baseTrianglesByPart.TryGetValue(partName, out var baseTriangles)) return;

            var shifted = CloneTriangles(baseTriangles);
            TranslateTrianglesYZ(shifted, -pivotY, -FinPivotZ);
            var rotated = _rotate.RotateXMesh(shifted, angleDegrees);
            TranslateTrianglesYZ(rotated, pivotY, FinPivotZ);
            part.Triangles = rotated;
        }

        private void InitializeBaseGeometry(I3dObject theObject)
        {
            if (_baseInitialized)
                return;

            _baseTrianglesByPart.Clear();
            foreach (var part in theObject.ObjectParts)
            {
                if (part.PartName == "LeftPectoralFin" || part.PartName == "RightPectoralFin")
                    _baseTrianglesByPart[part.PartName] = CloneTriangles(part.Triangles);
            }

            _baseOffsetX = theObject.ObjectOffsets?.x ?? 0f;
            _baseOffsetY = theObject.ObjectOffsets?.y ?? 0f;
            _baseOffsetZ = theObject.ObjectOffsets?.z ?? 0f;
            if (_hasPathBounds)
                _baseOffsetX = _jumpDirection < 0
                    ? _maxPathOffsetX - (_jumpHorizontalSpan * 0.5f)
                    : _minPathOffsetX + (_jumpHorizontalSpan * 0.5f);

            _baseInitialized = true;
        }

        private static void SyncToOriginal(I3dObject source)
        {
            var aiObjects = GameState.SurfaceState?.AiObjects;
            if (aiObjects == null) return;

            for (int i = 0; i < aiObjects.Count; i++)
            {
                if (aiObjects[i].ObjectId != source.ObjectId)
                    continue;

                var original = aiObjects[i];
                if (ReferenceEquals(original, source)) return;

                if (source.WorldPosition != null)
                {
                    original.WorldPosition = new Vector3
                    {
                        x = source.WorldPosition.x,
                        y = source.WorldPosition.y,
                        z = source.WorldPosition.z
                    };
                }

                if (source.ObjectOffsets != null)
                {
                    original.ObjectOffsets = new Vector3
                    {
                        x = source.ObjectOffsets.x,
                        y = source.ObjectOffsets.y,
                        z = source.ObjectOffsets.z
                    };
                }

                return;
            }
        }

        private static void TranslateTrianglesYZ(List<ITriangleMeshWithColor> triangles, float dy, float dz)
        {
            for (int i = 0; i < triangles.Count; i++)
            {
                var t = triangles[i];
                t.vert1.y += dy; t.vert1.z += dz;
                t.vert2.y += dy; t.vert2.z += dz;
                t.vert3.y += dy; t.vert3.z += dz;
            }
        }

        private static List<ITriangleMeshWithColor> CloneTriangles(List<ITriangleMeshWithColor> source)
        {
            var clone = new List<ITriangleMeshWithColor>(source.Count);
            foreach (var triangle in source)
            {
                clone.Add(new TriangleMeshWithColor
                {
                    Color = triangle.Color,
                    noHidden = triangle.noHidden,
                    landBasedPosition = triangle.landBasedPosition,
                    angle = triangle.angle,
                    vert1 = CopyVector(triangle.vert1),
                    vert2 = CopyVector(triangle.vert2),
                    vert3 = CopyVector(triangle.vert3),
                    normal1 = CopyVector(triangle.normal1),
                    normal2 = CopyVector(triangle.normal2),
                    normal3 = CopyVector(triangle.normal3)
                });
            }

            return clone;
        }

        private static Vector3 CopyVector(IVector3 vector)
        {
            return new Vector3
            {
                x = vector?.x ?? 0f,
                y = vector?.y ?? 0f,
                z = vector?.z ?? 0f
            };
        }

        public void ConfigureAudio(IAudioPlayer? audioPlayer, ISoundRegistry? soundRegistry)
        {
        }

        public void ReleaseParticles(I3dObject theObject)
        {
        }

        public void SetParticleGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
            if (StartCoord != null) StartCoordinates = StartCoord;
            if (GuideCoord != null) GuideCoordinates = GuideCoord;
        }

        public void SetRearEngineGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
        }

        public void SetWeaponGuideCoordinates(ITriangleMeshWithColor StartCoord, ITriangleMeshWithColor GuideCoord)
        {
        }

        public void Dispose()
        {
            _baseTrianglesByPart.Clear();
            _lastFrameTime = DateTime.MinValue;
            _jumpTimeSeconds = 0f;
            _baseOffsetX = 0f;
            _baseOffsetY = 0f;
            _baseOffsetZ = 0f;
            _baseInitialized = false;
            _firstPoseApplied = false;
            _takeoffSplashReleased = false;
            _landingSplashReleased = false;
            _particleAnchorInitialized = false;
            _particleAnchorOffsets = new Vector3();
            _jumpDirection = _initialJumpDirection;
            StartCoordinates = null;
            GuideCoordinates = null;
        }
    }
}
