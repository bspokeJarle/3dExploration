using System;
using System.Diagnostics;
using Domain;
using GameAiAndControls.Helpers;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Physics
{
    public class Physics : IPhysics
    {
        private bool LocalEnableLogging = false;

        public float Mass { get; set; } = 1.0f;
        public IVector3 Velocity { get; set; } = new Vector3(0, -90f, 0); // Initial downward velocity for bouncing
        public float Thrust { get; set; }
        public float Friction { get; set; } = 0.0f; // No air resistance for the test
        public float MaxSpeed { get; set; } = 10.0f;
        public float MaxThrust { get; set; } = 20.0f;
        public float GravityStrength { get; set; } = 200f; // Strong gravity for faster falling
        public IVector3 GravitySource { get; set; } = new Vector3 { x = 0, y = -10f, z = 0 };
        public IVector3 Acceleration { get; set; } = new Vector3(0, 0, 0);
        public float BounceHeightMultiplier { get; set; } = 0.6f; // Affects bounce height
        public float EnergyLossFactor { get; set; } = 1.3f; // Bounce energy retention factor
        public int BounceCooldownFrames { get; set; } = 0;

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

            var direction = new Vector3(0, 1, 0);
            var gravityForce = PhysicsHelpers.Multiply(direction, GravityStrength / Mass);
            Velocity = PhysicsHelpers.Add(Velocity, PhysicsHelpers.Multiply(gravityForce, deltaTime));

            return PhysicsHelpers.Add(currentPosition, PhysicsHelpers.Multiply(Velocity, deltaTime));
        }

        // Applies thrust to the object in a specific direction
        public IVector3 ApplyThrust(IVector3 currentPosition, IVector3 direction, float deltaTime)
        {
            if (BounceCooldownFrames > 0)
                return PhysicsHelpers.Add(currentPosition, PhysicsHelpers.Multiply(Velocity, deltaTime));

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
            // Coordinate system reminder: -Y is up, +Y is down
            // Normals are defined to reflect correctly based on incoming direction
            if (direction.HasValue)
            {
                normal = direction.Value switch
                {
                    ImpactDirection.Top => new Vector3(0, -1, 0),
                    ImpactDirection.Bottom => new Vector3(0, 1, 0),
                    ImpactDirection.Left => new Vector3(-1, 0, 0),
                    ImpactDirection.Right => new Vector3(1, 0, 0),
                    ImpactDirection.Center => new Vector3(0, 1, 0), // Center assumed to bounce upward
                    _ => normal
                };
            }

            var dot = PhysicsHelpers.Dot(Velocity, normal);
            var reflection = PhysicsHelpers.Subtract(Velocity, PhysicsHelpers.Multiply(normal, 2 * dot));

            // Apply energy loss on X and Z and enforce positive Y bounce
            reflection.x *= EnergyLossFactor;
            reflection.y -= -MathF.Abs(reflection.y) * BounceHeightMultiplier * EnergyLossFactor;
            reflection.z *= EnergyLossFactor;

            // Dynamically reduce bounce height for next bounce
            BounceHeightMultiplier *= (EnergyLossFactor / BounceHeightMultiplier);

            if (Logger.EnableFileLogging && LocalEnableLogging)
            {
                Logger.Log($"[Bounce] Direction: {direction}, Normal: {normal}");
                Logger.Log($"[Bounce] Resulting Velocity: {reflection}");
                Logger.Log($"[Bounce] New Multiplier: {BounceHeightMultiplier:F3}");
            }

            Velocity = reflection;
            BounceCooldownFrames = 3;
        }


        public IVector3 ApplyRotationDragForce(IVector3 rotationVector)
        {
            return null; // Not implemented yet
        }

        public void TiltStabilization(ref IVector3 tiltState)
        {
            // Not implemented yet
        }
    }
}
