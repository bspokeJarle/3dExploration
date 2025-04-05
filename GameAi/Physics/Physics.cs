using System;
using System.Diagnostics;
using Domain;
using GameAiAndControls.Helpers;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Physics
{
    public class Physics : IPhysics
    {
        public float Mass { get; set; } = 1.0f;
        public IVector3 Velocity { get; set; } = new Vector3(0, -90f, 0); // 🚀 Økt initialhastighet for høyere sprett
        public float Thrust { get; set; }
        public float Friction { get; set; } = 0.0f; // Ingen luftmotstand i testen
        public float MaxSpeed { get; set; } = 10.0f;
        public float MaxThrust { get; set; } = 20.0f;
        public float GravityStrength { get; set; } = 200f; // Realistisk gravitasjon
        public IVector3 GravitySource { get; set; } = new Vector3 { x = 0, y = -10f, z = 0 };
        public IVector3 Acceleration { get; set; } = new Vector3(0, 0, 0);
        public float BounceHeightMultiplier { get; set; } = 2f; // Høyere sprett

        public float EnergyLossFactor { get; set; } = 0.98f; // 🔁 Realistisk sprett med litt energitap
        public int BounceCooldownFrames { get; set; } = 0;

        public IVector3 ApplyDragForce(IVector3 currentPosition, float deltaTime)
        {
            float scaledDrag = MathF.Pow(1f - Friction, deltaTime * 60f);
            Velocity = PhysicsHelpers.Multiply(Velocity, scaledDrag);
            return PhysicsHelpers.Add(currentPosition, PhysicsHelpers.Multiply(Velocity, deltaTime));
        }

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

        public IVector3 ApplyGravityForce(IVector3 currentPosition, float deltaTime)
        {
            if (BounceCooldownFrames > 0)
            {
                BounceCooldownFrames--;
                return PhysicsHelpers.Add(currentPosition, PhysicsHelpers.Multiply(Velocity, deltaTime));
            }

            var direction = new Vector3(0, -1, 0);
            var gravityForce = PhysicsHelpers.Multiply(direction, GravityStrength / Mass);
            Velocity = PhysicsHelpers.Add(Velocity, PhysicsHelpers.Multiply(gravityForce, deltaTime));

            return PhysicsHelpers.Add(currentPosition, PhysicsHelpers.Multiply(Velocity, deltaTime));
        }

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

        public void Bounce(Vector3 normal)
        {
            var dot = PhysicsHelpers.Dot(Velocity, normal);
            var reflection = PhysicsHelpers.Subtract(Velocity, PhysicsHelpers.Multiply(normal, 2 * dot));

            // Tving refleksjonen oppover og juster høyde med multiplier
            reflection.y = MathF.Abs(reflection.y) * BounceHeightMultiplier;
            BounceHeightMultiplier *= (EnergyLossFactor/ BounceHeightMultiplier);

            Debug.WriteLine($"BounceHeightMultiplier nå: {BounceHeightMultiplier}");

            Velocity = PhysicsHelpers.Multiply(reflection, EnergyLossFactor);
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
