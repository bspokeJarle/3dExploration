using Domain;
using Microsoft.Windows.Themes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Controls
{
    //make a class that will spawn particles that follow a trajectory
    public class ParticlesAI : IParticles
    {
        private Random random = new();        
        private const int MaxParticles = 20;
        public List<IParticle> Particles { get; set; } = new();

        public IObjectMovement? ParentShip { get; set; }
        public bool Visible { get; set; }

        public void MoveParticles()
        {
            var deadParticles = new List<IParticle>();
            foreach (var particle in Particles)
            {
                var currentDateTime = DateTime.Now;
                if (particle.BirthTime.Ticks + particle.VariedStart > currentDateTime.Ticks) continue;

                var particleDeathTime = new DateTime(particle.BirthTime.Ticks + Convert.ToInt64(particle.Life * 10000000) + particle.VariedStart);
                if (particleDeathTime.Ticks > currentDateTime.Ticks)
                {
                    particle.Visible = true;
                    //Set the particle to visible
                    //todo's
                    //move particles according to their velocity, acceleration and direction
                    particle.Position.x -= particle.Velocity.x;
                    particle.Position.y -= particle.Velocity.y;
                    particle.Position.z -= particle.Velocity.z;
                    //Should we skip shading on particles? Maybe fade out instead? First check to rotate them to see
                    //Lets rotate the particles for a nice effect
                    if (particle.Rotation != null && particle.RotationSpeed != null) particle.Rotation.x += particle.RotationSpeed.x;
                    if (particle.Rotation != null && particle.RotationSpeed != null) particle.Rotation.y += particle.RotationSpeed.y;
                    if (particle.Rotation != null && particle.RotationSpeed != null) particle.Rotation.z += particle.RotationSpeed.z;
                    //when they hit something, they should bounce off
                    //when they hit something, they should lose some of their velocity
                    //when they hit something, they should lose some of their life - deduct from life-time                    
                }
                else
                {
                    //when they lose all their life, they s hould be removed from the list                    
                    deadParticles.Add(particle);
                }
            }
            foreach (var deadParticle in deadParticles)
            {
                Particles.Remove(deadParticle);
            }
        }

        public void ReleaseParticles(ITriangleMeshWithColor Trajectory, ITriangleMeshWithColor StartPosition, IObjectMovement ParShip, int Thrust)
        {
            //When button is let go(Thrust=0), clear all particles
            if (Thrust == 0) {
                Particles.Clear();
                Particles.TrimExcess();
                return;
            }
            //To prevent too many particles, limit the amount
            if (Particles.Count > MaxParticles) return;
            //No trajectory or start position, no particles
            if (StartPosition == null && Trajectory == null) return;
            ParentShip = ParShip;            
            long particleCount = Thrust * 5;            
            for (int i = 0; i < particleCount; i++)
            {
                if (Particles.Count > MaxParticles) break;
                //Todo need to rotate the trajectory and startposition to position the particles according the actualt ship
                //start position is the center of the engine
                var startX = (StartPosition.vert1.x + StartPosition.vert2.x + StartPosition.vert3.x) / 3;
                var startY = (StartPosition.vert1.y + StartPosition.vert2.y + StartPosition.vert3.y) / 3;
                var startz = (StartPosition.vert1.z + StartPosition.vert2.z + StartPosition.vert3.z) / 3;

                //guide position is the center of the trajectory
                var guideX = (Trajectory.vert1.x + Trajectory.vert2.x + Trajectory.vert3.x) / 3;
                var guideY = (Trajectory.vert1.y + Trajectory.vert2.y + Trajectory.vert3.y) / 3;
                var guideZ = (Trajectory.vert1.z + Trajectory.vert2.z + Trajectory.vert3.z) / 3;

                var particle = new Particle();

                particle.Life = random.NextInt64(3, 5);

                //Give the particles a random speed, but in the direction of the trajectory
                var randomOffset = random.NextInt64(-4, 5);
                var xSpeed = (startX - guideX) / particle.Life + randomOffset;
                var ySpeed = (startY - guideY) / particle.Life + randomOffset;
                var zSpeed = (startz - guideZ) / particle.Life + randomOffset;

                particle.Velocity = new Vector3()
                {
                    x = xSpeed,
                    y = ySpeed,
                    z = zSpeed
                };

                particle.Size = random.NextInt64(1, 4);
                //To prevent the particles clumping together, give them a varied start time
                particle.VariedStart = random.NextInt64(0, 5000000);
                particle.ParticleTriangle = new TriangleMeshWithColor()
                {
                    Color = "eeffee",
                    vert1 = new Vector3()
                    {
                        x = -particle.Size / 2,
                        y = -particle.Size / 2,
                        z = 0
                    },
                    vert2 = new Vector3()
                    {
                        x = particle.Size / 2,
                        y = -particle.Size / 2,
                        z = 0
                    },
                    vert3 = new Vector3()
                    {
                        x = 0,
                        y = particle.Size / 2,
                        z = 0
                    },
                    noHidden = true
                };

                particle.Position = new Vector3()
                {
                    x = startX,
                    y = startY,
                    z = startz
                };

                particle.BirthTime = DateTime.Now;
                particle.noHidden = true;
                particle.IsRotated = false;
                particle.Rotation = new Vector3 { x = random.NextInt64(0, 360), y = random.NextInt64(0, 360), z = random.NextInt64(0, 360) };
                particle.RotationSpeed = new Vector3 { x = random.NextInt64(-5, 5), y = random.NextInt64(-5, 5), z = random.NextInt64(-5, 5) };
                Visible = false;
                Particles.Add(particle);
            }
        }
    }
    public class Particle : IParticle
    {
        public DateTime BirthTime { get; set; } = DateTime.Now;
        public ITriangleMeshWithColor ParticleTriangle { get; set; }
        public IVector3 Velocity { get; set; }
        public IVector3 Acceleration { get; set; }
        public float Life { get; set; }
        public float Size { get; set; }
        public string Color { get; set; }
        public bool noHidden { get; set; }
        public long VariedStart { get; set; }
        public bool IsRotated { get; set; }
        public IVector3? Rotation { get; set; }
        public IVector3? Position { get; set; }
        public IVector3? RotationSpeed { get; set; }
        public bool? NoShading { get; set; }
        public bool Visible { get; set; }
    }
}
