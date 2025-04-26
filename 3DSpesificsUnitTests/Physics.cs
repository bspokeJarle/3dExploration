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
                Velocity = new Vector3(10, -20, 5), // Kommer fra venstre mot høyre
                EnergyLossFactor = 1f
            };

            physics.Bounce(new Vector3(0, 0, 0), ImpactDirection.Left);
            Assert.IsTrue(physics.Velocity.x < 0, "Left impact should bounce to the left (negative X)");

            physics.Velocity = new Vector3(-10, -20, 5); // Kommer fra høyre mot venstre
            physics.Bounce(new Vector3(0, 0, 0), ImpactDirection.Right);
            Assert.IsTrue(physics.Velocity.x > 0, "Right impact should bounce to the right (positive X)");

            physics.Velocity = new Vector3(0, 20, 0); // Kommer nedenfra opp
            physics.Bounce(new Vector3(0, 0, 0), ImpactDirection.Top);
            Assert.IsTrue(physics.Velocity.y < 0, "Top impact should bounce upward (negative Y)");

            physics.Velocity = new Vector3(0, -20, 0); // Kommer ovenfra ned
            physics.Bounce(new Vector3(0, 0, 0), ImpactDirection.Bottom);
            Assert.IsTrue(physics.Velocity.y > 0, "Bottom impact should bounce downward (positive Y)");

            physics.Velocity = new Vector3(0, 20, 0); // Samme som topp-impact, generalisert
            physics.Bounce(new Vector3(0, 0, 0), ImpactDirection.Center);
            Assert.IsTrue(physics.Velocity.y < 0, "Center impact should bounce upward (negative Y)");
        }

        [TestMethod]
        public void Particle_Should_Bounce_In_All_Axes_When_Hitting_Side()
        {
            var physics = new Physics
            {
                Mass = 1f,
                GravityStrength = 0f,
                Friction = 0f,
                EnergyLossFactor = 0.9f,
                Velocity = new Vector3(10f, -20f, 5f)
            };

            var normal = (Vector3)PhysicsHelpers.Normalize(new Vector3(1, 1, -1));
            physics.Bounce(normal);

            var newVel = physics.Velocity;

            Assert.IsTrue(newVel.x < 0, "After side bounce, X should be negative");
            Assert.IsTrue(newVel.y > 0, "After side bounce, Y should be positive (bounce upward)");
            Assert.IsTrue(newVel.z < 0, "Z should reflect negatively after bouncing on a surface with negative Z normal");
        }

        [TestMethod]
        public void Particle_Should_Bounce_AtLeastTwice_When_Crashing()
        {
            var physics = new Physics
            {
                Mass = 1f,
                GravityStrength = 50f,
                Friction = 0.02f,
                EnergyLossFactor = 0.88f,
                Velocity = new Vector3(0, 30f, 0) // Start nedover
            };

            var position = new Vector3(0, -150, 0);
            float deltaTime = 1f / 60f;
            int bounceCount = 0;
            bool wasFalling = true;

            for (int frame = 0; frame < 500; frame++)
            {
                position = (Vector3)physics.ApplyGravityForce(position, deltaTime);
                Debug.WriteLine($"Frame {frame}: Position: {position}, Velocity: {physics.Velocity}");

                if (position.y >= 0 && wasFalling)
                {
                    Debug.WriteLine($"Bounce at frame {frame}");
                    physics.Bounce(new Vector3(0, -1, 0));
                    bounceCount++;
                    wasFalling = false; // Nå har vi snudd oppover
                }

                if (physics.Velocity.y < 0)
                {
                    wasFalling = true;
                }

                if (bounceCount >= 2)
                    break;
            }

            Assert.IsTrue(bounceCount >= 2, $"Particle should bounce at least twice but only bounced {bounceCount} times.");
        }



        [TestMethod]
        public void GravityForce_Should_Accelerate_Y_Axis()
        {
            var physics = new Physics
            {
                Mass = 1f,
                GravityStrength = 1000f,
                Velocity = new Vector3(0, 0, 0)
            };

            var position = new Vector3(0, 100, 0);
            float deltaTime = 1f / 60f;

            position = (Vector3)physics.ApplyGravityForce(position, deltaTime);

            // FORDI Gravity drar -Y i ditt system:
            Assert.IsTrue(physics.Velocity.y < 0, "Gravity should pull velocity upward (negative Y direction in your coordinate system)");
            Assert.IsTrue(position.y > 100, "Position.y should move downward on screen");
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

            Assert.IsTrue(physics.Velocity.x < startVelocityX, "Drag should reduce X velocity");
            Assert.IsTrue(position.x > 0, "Position should move forward");
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
                Acceleration = new Vector3(0, 0, 0)
            };

            var pos = new Vector3(0, 100, 0);
            var newPos = physics.ApplyForces(pos, 1f / 60f);

            Assert.IsTrue(newPos.y > pos.y, "Gravity and no counter-acceleration should cause fall");
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

            Assert.AreEqual(2, physics.BounceCooldownFrames, "Cooldown should decrement each frame");
            Assert.AreEqual(pos.y + physics.Velocity.y * (1f / 60f), nextPos.y, 0.01, "Should move only with velocity during cooldown");
        }
    }
}
