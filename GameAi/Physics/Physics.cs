using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Domain;
using GameAiAndControls.Helpers;

namespace GameAiAndControls.Physics
{
    public class Physics : IPhysics
    {
        public float Mass { get; set; } = 1.0f; // Default mass
        public IVector3 Velocity { get; set; }
        public float Thrust { get; set; }
        public float Friction { get; set; } = 0.1f; // Default friction
        public float MaxSpeed { get; set; } = 10.0f; // Default max speed
        public float MaxThrust { get; set; } = 20.0f; // Default max thrust
        public IVector3 GravitySource { get; set; } = new _3dSpecificsImplementations.Vector3 { x = 0, y = -10f, z = 0 }; // Default gravity source

        public void ApplyDragForce(ref IVector3 worldPosition)
        {
            // Reduce velocity over time
            Velocity = PhysicsHelpers.Multiply(Velocity, (1 - Friction));

            // Move the object using the updated velocity
            worldPosition = PhysicsHelpers.Add(worldPosition, Velocity);
        }

        public void ApplyGravityForce(ref IVector3 worldPosition)
        {
            var direction = PhysicsHelpers.Normalize(PhysicsHelpers.Subtract(GravitySource, worldPosition));

            var gravityForce = PhysicsHelpers.Multiply(direction, 9.81f / Mass);

            Velocity = PhysicsHelpers.Add(Velocity, gravityForce);
            worldPosition = PhysicsHelpers.Add(worldPosition, Velocity);
        }

        public void ApplyRotationDragForce(ref IVector3 rotationVector)
        {
            
        }

        public void ApplyThrust(ref IVector3 worldPosition, IVector3 direction)
        {
            if (Thrust <= 0 || PhysicsHelpers.Length(direction) == 0)
                return;

            // Normalize the input direction to ensure consistent thrust
            var thrustDir = PhysicsHelpers.Normalize(direction);

            // Calculate thrust force
            var thrustForce = PhysicsHelpers.Multiply(thrustDir, Thrust / Mass);

            // Update velocity
            Velocity = PhysicsHelpers.Add(Velocity, thrustForce);

            // Clamp to max speed
            var speed = PhysicsHelpers.Length(Velocity);
            if (speed > MaxSpeed)
                Velocity = PhysicsHelpers.Multiply(PhysicsHelpers.Normalize(Velocity), MaxSpeed);

            // Apply velocity to position
            worldPosition = PhysicsHelpers.Add(worldPosition, Velocity);
        }

        public void TiltStabilization(ref IVector3 tiltState)
        {
           
        }
    }
}
