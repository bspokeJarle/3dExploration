using System;
using static Domain._3dSpecificsImplementations;

namespace Domain
{
    public interface IPhysics
    {
        float Mass { get; set; }
        IVector3 Velocity { get; set; }
        float Thrust { get; set; }
        float Friction { get; set; }
        float MaxSpeed { get; set; }
        float MaxThrust { get; set; }
        float GravityStrength { get; set; }
        IVector3 GravitySource { get; set; }
        IVector3 Acceleration { get; set; }
        int BounceCooldownFrames { get; set; }
        float BounceHeightMultiplier { get; set; }
        IVector3 ApplyDragForce(IVector3 currentPosition, float deltaTime);
        IVector3 ApplyForces(IVector3 currentPosition, float deltaTime);
        IVector3 ApplyGravityForce(IVector3 currentPosition, float deltaTime);
        IVector3 ApplyThrust(IVector3 currentPosition, IVector3 direction, float deltaTime);
        IVector3 ApplyRotationDragForce(IVector3 rotationVector);
        void Bounce(Vector3 normal, ImpactDirection? direction);
        void TiltStabilization(ref IVector3 tiltState);
        I3dObject ExplodeObject(I3dObject explodingObject, float explosionForece);
        I3dObject UpdateExplosion(I3dObject explodingObject, DateTime deltaTime);
    }
}
