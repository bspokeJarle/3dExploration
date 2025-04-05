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

            float bounceStartY = 0f;
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
                        System.Diagnostics.Debug.WriteLine($"Sprett {bounceCount}: Høyde = {currentBounceHeight:F2}");

                        if (bounceCount == 1)
                        {
                            Assert.IsTrue(currentBounceHeight >= 5f, $"Første sprett var for lav: {currentBounceHeight:F2} enheter");
                            previousBounceHeight = currentBounceHeight;
                        }
                        else if (bounceCount == 2)
                        {
                            Assert.IsTrue(currentBounceHeight < previousBounceHeight, $"Andre sprett var ikke lavere enn første: {currentBounceHeight:F2} vs {previousBounceHeight:F2}");
                            return;
                        }

                        trackingBounce = false;
                    }
                }
            }

            Assert.Fail("Det oppstod ikke minst to synlige sprett");
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

            Assert.IsTrue(physics.Velocity.y < 0, "Velocity.y should increase in positive direction (down in this world)");
            Assert.IsTrue(position.y < 0, "Position.y should move downwards (positive Y)");
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
                Acceleration = new Vector3(0, -500, 0) // upward force
            };

            var pos = new Vector3(0, 0, 0);
            var newPos = physics.ApplyForces(pos, 1f / 60f);

            Assert.IsTrue(newPos.y > pos.y, "Should move down (positive Y in this world), gravity stronger than acceleration");
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

            Assert.AreEqual(2, physics.BounceCooldownFrames, "Cooldown should decrease");
            Assert.AreEqual(pos.y + physics.Velocity.y * (1f / 60f), nextPos.y, 0.01, "Only velocity should be applied during cooldown");
        }
    }
}
