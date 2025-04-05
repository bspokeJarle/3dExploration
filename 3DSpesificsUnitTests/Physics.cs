using Domain;
using GameAiAndControls.Helpers;
using GameAiAndControls.Physics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using static Domain._3dSpecificsImplementations;

namespace _3DSpesificsUnitTests
{
    [TestClass]
    public class UnitTestPhysics
    {
        [TestMethod]
        public void Bounce_Should_Reflect_Correctly_With_ImpactDirectionEnum()
        {
            var physics = new Physics
            {
                Velocity = new Vector3(-10, -20, 5),
                EnergyLossFactor = 1f, // To isolate the effect of the normal direction
                BounceHeightMultiplier = 1f
            };

            // Test each direction
            physics.Bounce(new Vector3(0, 0, 0), ImpactDirection.Bottom);
            Assert.IsTrue(physics.Velocity.y > 0, "Bottom impact should reflect Y positively");

            physics.Velocity = new Vector3(-10, -20, 5);
            physics.Bounce(new Vector3(0, 0, 0), ImpactDirection.Top);
            Assert.IsTrue(physics.Velocity.y > 0, "Top impact should reflect Y positively");

            physics.Velocity = new Vector3(-10, -20, 5);
            physics.Bounce(new Vector3(0, 0, 0), ImpactDirection.Left);
            Assert.IsTrue(physics.Velocity.x > 0, "Left impact should reflect X positively");

            physics.Velocity = new Vector3(10, -20, 5);
            physics.Bounce(new Vector3(0, 0, 0), ImpactDirection.Right);
            Assert.IsTrue(physics.Velocity.x < 0, "Right impact should reflect X negatively");

            physics.Velocity = new Vector3(0, -20, 0);
            physics.Bounce(new Vector3(0, 0, 0), ImpactDirection.Center);
            Assert.IsTrue(physics.Velocity.y > 0, "Center impact should bounce upward");
        }

        [TestMethod]
        public void Particle_Should_Bounce_In_All_Axes_When_Hitting_Side()
        {
            var physics = new Physics
            {
                Mass = 1f,
                GravityStrength = 0f, // Gravity is disabled to test bounce reflection only
                Friction = 0f,
                EnergyLossFactor = 0.9f,
                BounceHeightMultiplier = 2f,
                Velocity = new Vector3(-10f, -20f, 5f) // Initial diagonal motion
            };

            var normal = new Vector3(1, 1, -1); // A wall with diagonal orientation
            normal = (Vector3)PhysicsHelpers.Normalize(normal);

            physics.Bounce(normal);

            var newVel = physics.Velocity;

            Debug.WriteLine($"After bounce: V=({newVel.x:F2}, {newVel.y:F2}, {newVel.z:F2})");

            Assert.IsTrue(newVel.x > 0, "X direction should be reflected");
            Assert.IsTrue(newVel.y > 0, "Y direction should be reflected and positive (upward)");
            Assert.IsTrue(newVel.z < 0, "Z direction should be reflected");
        }

        [TestMethod]
        public void Particle_Should_Bounce_AtLeastOnce_When_Crashing()
        {
            var physics = new Physics
            {
                Mass = 1f,
                GravityStrength = 120f,
                Friction = 0.0f,
                EnergyLossFactor = 0.88f,
                BounceHeightMultiplier = 6.0f,
                Velocity = new Vector3(0, -40f, 0)
            };

            var position = new Vector3(0, 0, 0);
            float deltaTime = 1f / 60f;
            float maxYAfterBounce = 0f;
            float previousBounceHeight = 0f;
            int bounceCount = 0;
            bool trackingBounce = false;
            bool waitingForFall = false;

            for (int frame = 0; frame < 400; frame++)
            {
                position = (Vector3)physics.ApplyGravityForce(position, deltaTime);
                Debug.WriteLine($"Frame {frame}: Y = {position.y:F2}, VY = {physics.Velocity.y:F2}");

                if (position.y <= 0 && physics.Velocity.y < 0)
                {
                    Debug.WriteLine($"Bounce! Position Y: {position.y}, Velocity Y: {physics.Velocity.y}");

                    physics.Bounce(new Vector3(0, -1, 0));
                    maxYAfterBounce = 0f;
                    trackingBounce = true;
                    waitingForFall = false;
                    bounceCount++;
                }
                else if (trackingBounce)
                {
                    if (!waitingForFall)
                    {
                        if (physics.Velocity.y < 0)
                        {
                            waitingForFall = true;
                        }
                        else if (position.y > maxYAfterBounce)
                        {
                            maxYAfterBounce = position.y;
                        }
                    }
                    else if (waitingForFall && position.y < maxYAfterBounce - 0.5f)
                    {
                        float currentBounceHeight = maxYAfterBounce;
                        Debug.WriteLine($"Bounce {bounceCount}: Height = {currentBounceHeight:F2}");

                        if (bounceCount == 1)
                        {
                            Assert.IsTrue(currentBounceHeight >= 5f, $"First bounce was too low: {currentBounceHeight:F2} units");
                            previousBounceHeight = currentBounceHeight;
                        }
                        else if (bounceCount == 2)
                        {
                            Assert.IsTrue(currentBounceHeight < previousBounceHeight, $"Second bounce was not lower: {currentBounceHeight:F2} vs {previousBounceHeight:F2}");
                            return;
                        }

                        trackingBounce = false;
                    }
                }
            }

            Assert.Fail("There were not at least two visible bounces");
        }

        [TestMethod]
        public void GravityForce_Should_Accelerate_Y_Axis()
        {
            var physics = new Physics
            {
                Mass = 1f,
                GravityStrength = 1000f,
                Velocity = new Vector3(0, 0, 0),
                GravitySource = new Vector3(0, 100f, 0),
            };

            var position = new Vector3(0, 0, 0);
            float deltaTime = 1f / 60f;

            position = (Vector3)physics.ApplyGravityForce(position, deltaTime);

            Assert.IsTrue(physics.Velocity.y < 0, "Velocity.y should be negative (falling downward)");
            Assert.IsTrue(position.y < 0, "Position.y should move downward");
        }

        [TestMethod]
        public void DragForce_Should_SlowDown_Velocity()
        {
            var physics = new Physics
            {
                Velocity = new Vector3(10, 0, 0),
                Friction = 0.1f
            };

            var startVelocityX = physics.Velocity.x;
            var position = physics.ApplyDragForce(new Vector3(0, 0, 0), 1f / 60f);

            Assert.IsTrue(physics.Velocity.x < startVelocityX, "Drag should reduce horizontal velocity");
            Assert.IsTrue(position.x > 0, "Object should have moved forward");
        }

        [TestMethod]
        public void ApplyForces_Should_Apply_Gravity_And_Acceleration()
        {
            var physics = new Physics
            {
                Mass = 1f,
                GravityStrength = 1000f,
                Friction = 0.0f,
                Velocity = new Vector3(0, 0, 0),
                Acceleration = new Vector3(0, -500, 0) // Upward force
            };

            var pos = new Vector3(0, 0, 0);
            var newPos = physics.ApplyForces(pos, 1f / 60f);

            Assert.IsTrue(newPos.y > pos.y, "Object should fall due to stronger gravity compared to upward acceleration");
        }

        [TestMethod]
        public void BounceCooldown_Should_Skip_Gravity()
        {
            var physics = new Physics
            {
                Velocity = new Vector3(0, -200, 0),
                BounceCooldownFrames = 3
            };

            var pos = new Vector3(0, 100, 0);
            var nextPos = physics.ApplyGravityForce(pos, 1f / 60f);

            Assert.AreEqual(2, physics.BounceCooldownFrames, "Cooldown should decrease by one each frame");
            Assert.AreEqual(pos.y + physics.Velocity.y * (1f / 60f), nextPos.y, 0.01, "Only velocity should be applied during cooldown");
        }
    }
}
