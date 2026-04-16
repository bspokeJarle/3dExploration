using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using CommonUtilities._3DHelpers;
using Domain;
using GameAiAndControls.Helpers;
using static Domain._3dSpecificsImplementations;
using CommonUtilities.CommonSetup;
using static GameAiAndControls.Helpers.PhysicsHelpers;

namespace GameAiAndControls.Physics
{
    public class Physics : IPhysics
    {
        private bool LocalEnableLogging = false;
        private const float DEG2RAD = MathF.PI / 180f;

        // ── General physics ──────────────────────────────────────────
        public float Mass { get; set; } = 1.0f;
        public IVector3 Velocity { get; set; } = new Vector3(0, -90f, 0);
        public float Thrust { get; set; }
        public float Friction { get; set; } = 0.0f;
        public float MaxSpeed { get; set; } = 10.0f;
        public float MaxThrust { get; set; } = 20.0f;
        public IVector3 Acceleration { get; set; } = new Vector3(0, 0, 0);

        // ── Gravity & bounce (used by particle/object physics) ───────
        public float GravityStrength { get; set; } = 1f;
        public IVector3 GravitySource { get; set; } = new Vector3 { x = 0, y = -10f, z = 0 };
        public float BounceHeightMultiplier { get; set; } = 0.8f;
        public float EnergyLossFactor { get; set; } = 0.2f;
        public int BounceCooldownFrames { get; set; } = 0;

        // ── Flight inertia state (reset between thrust activations) ──
        public float FallVelocity { get; set; } = 0f;
        public float InertiaX { get; set; } = 0f;
        public float InertiaY { get; set; } = 0f;
        public float InertiaZ { get; set; } = 0f;
        public float ThrustEffect { get; set; } = 0f;
        public float VerticalLiftFactor { get; set; } = 0f;

        // ── Flight tuning constants ──────────────────────────────────
        public float GravityAcceleration { get; set; } = 3.6f;
        public float TerminalFallSpeed { get; set; } = 35f;
        public float GravityPullMultiplier { get; set; } = 9.0f;
        public float ThrustSpeedMultiplier { get; set; } = 9.6f;
        public float ThrustHeightMultiplier { get; set; } = 7.0f;
        public float ThrustRampRate { get; set; } = 30.0f;
        public float InertiaDrag { get; set; } = 0.92f;
        public float MaxInertia { get; set; } = 45.0f;
        public float VerticalThrustSmoothing { get; set; } = 0.6f;
        public float VerticalLiftRate { get; set; } = 3.0f;

        // ── Height limits ────────────────────────────────────────────
        public float CeilingHeight { get; set; } = 1000f;
        public float FloorHeight { get; set; } = -100f;
        // Computed from current screen size so the value stays correct even
        // when Physics is constructed before ScreenSetup.Initialize().
        public float MaxScreenDrop
        {
            get => ScreenSetup.screenSizeY * 0.44f;
            set { }
        }

        // ── Hover/float after thrust release ─────────────────────────
        // When thrust stops, gravity stays at HoverMinGravityScale for
        // HoverFloatDuration seconds, then ramps linearly to full over
        // HoverRampDuration seconds.
        public float HoverElapsed { get; set; } = 0f;
        public float HoverFloatDuration { get; set; } = 0.4f;
        public float HoverRampDuration { get; set; } = 0.75f;
        public float HoverMinGravityScale { get; set; } = 0.05f;

        // ── Airborne settle (return-to-rest while not thrusting) ─────
        // Gentle spring rate that pulls the surface back toward its
        // resting screen position and zero altitude when airborne.
        // Scaled by 1/ScreenScaleY so the gravity-settle equilibrium
        // distance stays proportional to the screen height, ensuring
        // the ship can always reach the surface crash box.
        // Computed from current screen size so the value stays correct even
        // when Physics is constructed before ScreenSetup.Initialize().
        public float AirborneSettleRate
        {
            get => 2.0f / ScreenSetup.ScreenScaleY;
            set { }
        }

        // Applies speed-dependent drag (Aviator-inspired v² scaling) and clamps inertia.
        // At low speeds the drag factor is close to InertiaDrag (0.92); at MaxInertia the
        // effective multiplier drops to ~0.85, giving a natural top-speed feel.
        private const float DragSpeedScaling = 0.08f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float ApplyDragAndClamp(float inertia)
        {
            float speedRatio = MathF.Abs(inertia) / MaxInertia;
            float drag = InertiaDrag - DragSpeedScaling * speedRatio * speedRatio;
            return Math.Clamp(inertia * drag, -MaxInertia, MaxInertia);
        }

        // Applies drag to the current velocity and returns the updated position
        public IVector3 ApplyDragForce(IVector3 currentPosition, float deltaTime)
        {
            float scaledDrag = MathF.Pow(1f - Friction, deltaTime * 60f);
            Velocity = PhysicsHelpers.Multiply(Velocity, scaledDrag);
            return PhysicsHelpers.Add(currentPosition, PhysicsHelpers.Multiply(Velocity, deltaTime));
        }

        // Applies gravity, acceleration and drag and returns the updated position
        public IVector3 ApplyForces(IVector3 currentPosition, float deltaTime)
        {
            if (BounceCooldownFrames > 0)
            {
                BounceCooldownFrames--;
                return PhysicsHelpers.Add(currentPosition, PhysicsHelpers.Multiply(Velocity, deltaTime));
            }

            var gravityDir = new Vector3(0, 1, 0);
            var gravityForce = PhysicsHelpers.Multiply(gravityDir, GravityStrength / Mass);
            Velocity = PhysicsHelpers.Add(Velocity, PhysicsHelpers.Multiply(gravityForce, deltaTime));

            Velocity = PhysicsHelpers.Add(Velocity, PhysicsHelpers.Multiply(Acceleration, deltaTime));
            Velocity = PhysicsHelpers.Multiply(Velocity, 1 - Friction);

            return PhysicsHelpers.Add(currentPosition, PhysicsHelpers.Multiply(Velocity, deltaTime));
        }

        // Applies only gravity to the object
        public IVector3 ApplyGravityForce(IVector3 currentPosition, float deltaTime)
        {
            if (BounceCooldownFrames > 0)
            {
                BounceCooldownFrames--;
                return PhysicsHelpers.Add(currentPosition, PhysicsHelpers.Multiply(Velocity, deltaTime));
            }

            // 1. Add acceleration (if the particle has it)
            Velocity.x += Acceleration.x;
            Velocity.y += Acceleration.y;
            Velocity.z += Acceleration.z;

            // 2. Apply gravity on the Y axis (GravityStrength pulls down, minus since -Y is up)
            Velocity.y -= GravityStrength * deltaTime;

            // 3. Apply friction
            Velocity.x *= 0.95f;
            Velocity.y *= 0.95f;
            Velocity.z *= 0.95f;

            // 4. Move position opposite to velocity
            currentPosition.x -= Velocity.x;
            currentPosition.y -= Velocity.y;
            currentPosition.z -= Velocity.z;

            return currentPosition;
        }

        // Applies thrust to the object in a specific direction
        public IVector3 ApplyThrust(IVector3 currentPosition, IVector3 direction, float deltaTime)
        {
            if (BounceCooldownFrames > 0)
            {
                BounceCooldownFrames--;
                return PhysicsHelpers.Add(currentPosition, PhysicsHelpers.Multiply(Velocity, deltaTime));
            }

            if (Thrust <= 0) return currentPosition;

            var thrustDir = PhysicsHelpers.Normalize(direction);
            var thrustForce = PhysicsHelpers.Multiply(thrustDir, Thrust / Mass);

            Velocity = PhysicsHelpers.Add(
                Velocity,
                PhysicsHelpers.Multiply(thrustForce, deltaTime)
            );

            var speed = PhysicsHelpers.Length(Velocity);
            if (speed > MaxSpeed)
            {
                Velocity = PhysicsHelpers.Multiply(
                    PhysicsHelpers.Normalize(Velocity),
                    MaxSpeed
                );
            }

            return PhysicsHelpers.Add(currentPosition, PhysicsHelpers.Multiply(Velocity, deltaTime));
        }

        // Reflects velocity along a surface normal and applies energy loss
        public void Bounce(Vector3 normal, ImpactDirection? direction = null)
        {
            if (direction.HasValue)
            {
                normal = direction.Value switch
                {
                    ImpactDirection.Top => new Vector3(0, -1, 0),
                    ImpactDirection.Bottom => new Vector3(0, 1, 0),
                    ImpactDirection.Left => new Vector3(-1, 0, 0),
                    ImpactDirection.Right => new Vector3(1, 0, 0),
                    ImpactDirection.Center => new Vector3(0, -1, 0),
                    _ => normal
                };
            }

            // Bounce on Y axis (Top/Bottom)
            if (normal.y != 0)
            {
                Velocity.y = -Velocity.y * EnergyLossFactor;
            }

            // Bounce on X axis (Left/Right)
            if (normal.x != 0)
            {
                Velocity.x = -Velocity.x * EnergyLossFactor;
            }

            // Bounce on Z axis (for front/back hits, if needed)
            if (normal.z != 0)
            {
                Velocity.z = -Velocity.z * EnergyLossFactor;
            }

            BounceCooldownFrames = 3;
        }


        // Applies rotational damping proportional to current rotation rates (Aviator-inspired).
        // Returns a damped copy of the input rotation vector.
        public IVector3 ApplyRotationDragForce(IVector3 rotationVector)
        {
            const float RotationalDamping = 0.94f;
            return new Vector3
            {
                x = rotationVector.x * RotationalDamping,
                y = rotationVector.y * RotationalDamping,
                z = rotationVector.z * RotationalDamping
            };
        }

        // Gently returns tilt toward neutral (x→0) when no pitch input is applied.
        // StabilizationRate controls how quickly the tilt decays per call.
        private const float StabilizationRate = 0.03f;

        public void TiltStabilization(ref IVector3 tiltState)
        {
            tiltState.x -= tiltState.x * StabilizationRate;
        }

        // Applies gravity when falling (no thrust). Returns updated InertiaY.
        // Gravity is scaled by the hover ramp: near-zero during float, then gradually increasing.
        public float ApplyFallGravity(float rotationDegrees, float deltaTime)
        {
            HoverElapsed += deltaTime;
            float gravityScale = GetHoverGravityScale();

            float rotationRad = (rotationDegrees % 180) * DEG2RAD;
            float gravityModifier = Math.Clamp(MathF.Sin(rotationRad), 0.3f, 1.0f);
            float gravityPull = GravityAcceleration * gravityModifier * GravityPullMultiplier * gravityScale * deltaTime;

            InertiaY = ApplyDragAndClamp(InertiaY - gravityPull);
            FallVelocity = MathF.Max(-InertiaY, 0f);
            return InertiaY;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetHoverGravityScale()
        {
            if (HoverElapsed < HoverFloatDuration)
                return HoverMinGravityScale;

            float rampElapsed = HoverElapsed - HoverFloatDuration;
            if (rampElapsed >= HoverRampDuration)
                return 1f;

            return HoverMinGravityScale + (1f - HoverMinGravityScale) * (rampElapsed / HoverRampDuration);
        }

        public void ResetHover() => HoverElapsed = 0f;

        // Retained for IPhysics contract — no longer called by ship controls
        public void ReduceFallWithThrust(float thrust, float rotationDegrees, float deltaTime)
        {
            float upwardFactor = MathF.Cos(rotationDegrees * DEG2RAD);
            float thrustLift = thrust * upwardFactor * 0.75f * deltaTime;
            FallVelocity = Math.Max(FallVelocity - thrustLift, 0f);
        }

        // Calculates thrust on all three axes with continuous gravity. Returns updated InertiaY.
        // Tilt controls vertical/forward split; rotation controls heading.
        // When inverted (tilt ~180°), upwardFactor goes negative — thrust pushes into the ground.
        // Gravity scales in with VerticalLiftFactor to prevent an initial dip at thrust start.
        public float CalculateThrustForces(float thrust, float tiltDegrees, float rotationDegrees, float deltaTime)
        {
            ThrustEffect = MathF.Min(ThrustEffect + ThrustRampRate * deltaTime, 1f);
            VerticalLiftFactor = MathF.Min(VerticalLiftFactor + VerticalLiftRate * deltaTime, 1f);

            float tiltRad = tiltDegrees * DEG2RAD;
            float rotationRad = rotationDegrees * DEG2RAD;

            float upwardFactor = MathF.Cos(tiltRad);   // +1 upright, 0 sideways, -1 inverted
            float forwardFactor = MathF.Sin(tiltRad);
            float dirX = MathF.Sin(rotationRad);
            float dirZ = MathF.Cos(rotationRad);

            // Horizontal forces — projected onto world X/Z axes
            float horizontalForce = thrust * ThrustEffect * ThrustSpeedMultiplier * forwardFactor * deltaTime;
            InertiaX = ApplyDragAndClamp(InertiaX + horizontalForce * dirX);
            InertiaZ = ApplyDragAndClamp(InertiaZ - horizontalForce * dirZ);

            // Vertical thrust — angle-dependent (negative when inverted pushes into ground)
            float verticalThrust = thrust * ThrustEffect * VerticalLiftFactor * ThrustHeightMultiplier
                                 * upwardFactor * VerticalThrustSmoothing * deltaTime;
            float gravityPull = GravityAcceleration * GravityPullMultiplier * VerticalLiftFactor * deltaTime;

            InertiaY = ApplyDragAndClamp(InertiaY + verticalThrust - gravityPull);
            FallVelocity = MathF.Max(-InertiaY, 0f);
            return InertiaY;
        }

        public float CalculateCurrentSpeed(bool isLanded)
        {
            float horizontalSpeed = MathF.Sqrt(InertiaX * InertiaX + InertiaZ * InertiaZ);
            float verticalSpeed = isLanded ? 0f : MathF.Abs(InertiaY);
            return horizontalSpeed + verticalSpeed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ClampToHeightRange(float value) => Math.Clamp(value, FloorHeight, CeilingHeight);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ClampToScreenDrop(float value) => MathF.Min(value, MaxScreenDrop);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float WrapPosition(float position, float diff, float minValue, float maxValue)
        {
            float newPos = position + diff;
            if (newPos >= maxValue) return minValue;
            if (newPos <= minValue) return maxValue;
            return newPos;
        }

        private class ExplodingTriangle
        {
            public TriangleMeshWithColor Triangle;
            public Vector3 Direction;
            public Vector3 RotationAxis;
            public float Speed;
            public float RotationSpeed;
            public float ElapsedTime;
            public float Duration = 1.2f; // Adjusted for faster fading
            public Vector3 Center;
            public int PartIndex;
            public int TriangleIndex;
            public string OriginalColor; // NEW: reference color
        }

        private List<ExplodingTriangle> _explodingTriangles = new();
        private bool _isExploding = false;

        public string? ExplosionColorOverride { get; set; }

        public I3dObject ExplodeObject(I3dObject originalObject, float explosionForce = 200f)
        {
            _explodingTriangles.Clear();
            _isExploding = true;

            var explodingObject = Common3dObjectHelpers.DeepCopySingleObject(originalObject);
            var center = CalculateTriangleGeometryCenter(explodingObject);

            var sharedTriangles = new List<TriangleMeshWithColor>();
            int partIndex = 0;

            foreach (var part in explodingObject.ObjectParts)
            {
                int triangleIndex = 0;

                foreach (var triangle in part.Triangles.OfType<TriangleMeshWithColor>())
                {
                    var tri = new TriangleMeshWithColor
                    {
                        vert1 = triangle.vert1,
                        vert2 = triangle.vert2,
                        vert3 = triangle.vert3,
                        normal1 = triangle.normal1,
                        normal2 = triangle.normal2,
                        normal3 = triangle.normal3,
                        Color = triangle.Color,
                        angle = triangle.angle,
                        landBasedPosition = triangle.landBasedPosition,
                        noHidden = true
                    };

                    var triCenter = GetTriangleCenter(tri);
                    var rawDir = Subtract(triCenter, center);
                    var direction = Normalize(rawDir);

                    // Adjust the explosion direction
                    if (direction.y < 0) direction.y *= 0.25f; // Dampen downward
                    if (direction.y < 0.05f) direction.y += 0.1f; // Lift slightly
                    direction.x *= 1.3f; // More sideways spread
                    direction.z *= 1.3f;
                    direction = Normalize(direction); // Re-normalize

                    var rotationAxis = RandomUnitVector();

                    _explodingTriangles.Add(new ExplodingTriangle
                    {
                        Triangle = tri,
                        Direction = (Vector3)direction,
                        RotationAxis = (Vector3)rotationAxis,
                        Speed = RandomHelper.Float(explosionForce * 0.5f, explosionForce),
                        RotationSpeed = RandomHelper.Float(30f, 120f),
                        Center = (Vector3)triCenter,
                        ElapsedTime = 0f,
                        PartIndex = partIndex,
                        TriangleIndex = triangleIndex,
                        OriginalColor = triangle.Color // Store original color for fading
                    });

                    sharedTriangles.Add(tri);
                    triangleIndex++;
                }

                partIndex++;
            }

            explodingObject.ObjectParts.Clear();
            explodingObject.ObjectParts.Add(new _3dObjectPart
            {
                PartName = "ExplodingPart",
                Triangles = sharedTriangles.Cast<ITriangleMeshWithColor>().ToList(),
                IsVisible = true
            });

            return explodingObject;
        }



        public I3dObject UpdateExplosion(I3dObject explodingObject, DateTime deltaTime)
        {
            if (!_isExploding || _explodingTriangles.Count == 0)
                return explodingObject;

            float simulatedElapsedTime = _explodingTriangles[0].ElapsedTime;
            float realElapsedTime = (float)Math.Max(0d, (DateTime.Now - deltaTime).TotalSeconds);
            float frameTime = realElapsedTime - simulatedElapsedTime;

            if (frameTime < 0f)
            {
                frameTime = 0f;
            }

            frameTime = MathF.Min(frameTime, 0.1f);
            bool allTrianglesFinished = true;

            foreach (var exploding in _explodingTriangles)
            { 
                // Progress is capped between 0 and 1
                float progress = Clamp(exploding.ElapsedTime / exploding.Duration, 0f, 1f);

                // Apply color transition based on progress
                exploding.Triangle.Color = GetExplosionColor(progress, exploding.OriginalColor);

                if (Logger.EnableFileLogging && LocalEnableLogging)
                {
                    Logger.Log($"[EXPLOSION] TriangleIndex={exploding.TriangleIndex} " +
                               $"Elapsed={exploding.ElapsedTime:F2}, " +
                               $"RealElapsed={realElapsedTime:F2}, " +
                               $"FrameTime={frameTime:F4}, " +
                               $"Duration={exploding.Duration:F2}, " +
                               $"Progress={progress:F2}, " +
                               $"Color={exploding.Triangle.Color}");
                }

                // Skip movement if done, set Explosion to finished, reset Scene
                if (exploding.ElapsedTime >= exploding.Duration)
                {
                    continue;
                }

                allTrianglesFinished = false;

                // Move and rotate
                exploding.ElapsedTime += frameTime;

                var move = Multiply(exploding.Direction, exploding.Speed * frameTime);
                exploding.Triangle.vert1 = Add(exploding.Triangle.vert1, move);
                exploding.Triangle.vert2 = Add(exploding.Triangle.vert2, move);
                exploding.Triangle.vert3 = Add(exploding.Triangle.vert3, move);

                float angle = exploding.RotationSpeed * frameTime;
                exploding.Triangle.vert1 = RotateAroundAxis(exploding.Triangle.vert1, exploding.RotationAxis, angle, exploding.Center);
                exploding.Triangle.vert2 = RotateAroundAxis(exploding.Triangle.vert2, exploding.RotationAxis, angle, exploding.Center);
                exploding.Triangle.vert3 = RotateAroundAxis(exploding.Triangle.vert3, exploding.RotationAxis, angle, exploding.Center);

                // Apply updated triangle
                explodingObject.ObjectParts[exploding.PartIndex].Triangles[exploding.TriangleIndex] = exploding.Triangle;
            }

            if (allTrianglesFinished)
            {
                _isExploding = false;

                if (explodingObject.ImpactStatus != null)
                {
                    explodingObject.ImpactStatus.HasExploded = true;
                }
                else
                {
                    explodingObject.ImpactStatus = new ImpactStatus { HasExploded = true };
                }
            }

            return explodingObject;
        }

        private string GetExplosionColor(float progress, string originalHex)
        {
            if (ExplosionColorOverride != null)
            {
                if (progress < 0.10f)
                    return LerpColorHex("AADDFF", ExplosionColorOverride, progress / 0.10f);
                else if (progress < 0.35f)
                    return LerpColorHex(ExplosionColorOverride, "1133AA", (progress - 0.10f) / 0.25f);
                else if (progress < 0.7f)
                    return LerpColorHex("1133AA", "001133", (progress - 0.35f) / 0.35f);
                else
                    return LerpColorHex("001133", "000000", (progress - 0.7f) / 0.3f);
            }

            if (progress < 0.10f)
                return LerpColorHex(originalHex, "ffff00", progress / 0.10f); // original → yellow
            else if (progress < 0.35f)
                return LerpColorHex("ffff00", "ff0000", (progress - 0.10f) / 0.25f); // yellow → red
            else if (progress < 0.7f)
                return LerpColorHex("ff0000", "330000", (progress - 0.35f) / 0.35f); // red → dark red
            else
                return LerpColorHex("330000", "000000", (progress - 0.7f) / 0.3f); // dark red → black
        }
    }
}
