using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CommonUtilities._3DHelpers;
using Domain;
using GameAiAndControls.Helpers;
using static Domain._3dSpecificsImplementations;
using static GameAiAndControls.Helpers.PhysicsHelpers;

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
        public float GravityStrength { get; set; } = 1f; // Strong gravity for faster falling
        public IVector3 GravitySource { get; set; } = new Vector3 { x = 0, y = -10f, z = 0 };
        public IVector3 Acceleration { get; set; } = new Vector3(0, 0, 0);
        public float BounceHeightMultiplier { get; set; } = 0.8f; // Affects bounce height
        public float EnergyLossFactor { get; set; } = 0.2f; // Bounce energy retention factor
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

            // 1. Legg til akselerasjon (hvis partikkelen har det)
            Velocity.x += Acceleration.x;
            Velocity.y += Acceleration.y;
            Velocity.z += Acceleration.z;

            // 2. Påfør gravity på Y-aksen (GravityStrength drar ned, altså minus siden -Y er opp)
            Velocity.y -= GravityStrength * deltaTime;

            // 3. Påfør friksjon
            Velocity.x *= 0.95f;
            Velocity.y *= 0.95f;
            Velocity.z *= 0.95f;

            // 4. Flytt posisjon motsatt av velocity
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

            // Bounce på Y-akse (Top/Bottom)
            if (normal.y != 0)
            {
                Velocity.y = -Velocity.y * EnergyLossFactor;
            }

            // Bounce på X-akse (Left/Right)
            if (normal.x != 0)
            {
                Velocity.x = -Velocity.x * EnergyLossFactor;
            }

            // Bounce på Z-akse (for front/back treff, hvis vi trenger det)
            if (normal.z != 0)
            {
                Velocity.z = -Velocity.z * EnergyLossFactor;
            }

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

        private class ExplodingTriangle
        {
            public TriangleMeshWithColor Triangle;
            public Vector3 Direction;
            public Vector3 RotationAxis;
            public float Speed;
            public float RotationSpeed;
            public float ElapsedTime;
            public float Duration = 2.0f;
            public Vector3 Center;
            public int PartIndex = 0;
            public int TriangleIndex;
        }

        private List<ExplodingTriangle> _explodingTriangles = new();
        private bool _isExploding = false;

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

                    // --- Her justeres retningen ---
                    if (direction.y < 0) direction.y *= 0.25f; // Demp nedover
                    if (direction.y < 0.05f) direction.y += 0.1f; // Løft litt
                    direction.x *= 1.3f; // Mer sideveis
                    direction.z *= 1.3f;
                    direction = Normalize(direction); // Re-normaliser

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
                        TriangleIndex = triangleIndex
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

            if (Logger.EnableFileLogging && LocalEnableLogging)
            {
                Logger.Log($"[UPDATE] Explosion update called. Triangles: {_explodingTriangles.Count}");
            }

            float frameTime = 1f / 60f; // Fixed frame rate per update tick

            foreach (var exploding in _explodingTriangles)
            {
                if (exploding.ElapsedTime >= exploding.Duration)
                    continue;

                exploding.ElapsedTime += frameTime;

                // Move the triangle outward along its direction vector
                var move = PhysicsHelpers.Multiply(exploding.Direction, exploding.Speed * frameTime);
                exploding.Triangle.vert1 = PhysicsHelpers.Add(exploding.Triangle.vert1, move);
                exploding.Triangle.vert2 = PhysicsHelpers.Add(exploding.Triangle.vert2, move);
                exploding.Triangle.vert3 = PhysicsHelpers.Add(exploding.Triangle.vert3, move);

                // Rotate triangle around its center
                float angle = exploding.RotationSpeed * frameTime;
                exploding.Triangle.vert1 = PhysicsHelpers.RotateAroundAxis(exploding.Triangle.vert1, exploding.RotationAxis, angle, exploding.Center);
                exploding.Triangle.vert2 = PhysicsHelpers.RotateAroundAxis(exploding.Triangle.vert2, exploding.RotationAxis, angle, exploding.Center);
                exploding.Triangle.vert3 = PhysicsHelpers.RotateAroundAxis(exploding.Triangle.vert3, exploding.RotationAxis, angle, exploding.Center);

                // Update the Triangles inside the Mesh
                explodingObject.ObjectParts[exploding.PartIndex].Triangles[exploding.TriangleIndex] = exploding.Triangle;
            }

            // Log a few triangle positions for debugging
            foreach (var (t, index) in _explodingTriangles.Select((et, i) => (et, i)))
            {
                if (index >= 3) break;

                if (Logger.EnableFileLogging && LocalEnableLogging)
                {
                    Logger.Log($"[UPDATE] T{index}: Elapsed={t.ElapsedTime:F2} v1=({t.Triangle.vert1.x:F2}, {t.Triangle.vert1.y:F2}, {t.Triangle.vert1.z:F2})");
                }
            }

            return explodingObject;
        }
    }
}
