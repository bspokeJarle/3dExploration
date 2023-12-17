using Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Ai
{
    //make a class that will spawn particles that follow a trajectory
    public class ParticlesAI : IParticles
    {        
        private Random random = new();        
        public List<IParticle> Particles { get; set; } = new();

        public void MoveParticles()
        {
            var deadParticles = new List<IParticle>();
            foreach( var particle in Particles)
            {
                var particleDateTime = new DateTime(particle.BirthTime.Ticks + Convert.ToInt64(particle.Life * 1000));
                if (particleDateTime.Ticks<= new DateTime().Ticks)
                {                    
                    //todo
                    //move particles according to their velocity, acceleration and direction
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
      
        public void ReleaseParticles(ITriangleMeshWithColor Trajectory, ITriangleMeshWithColor StartPosition)
        {                        
            long particleCount = random.NextInt64(50,100);
            for (int i = 0; i < particleCount; i++)
            {
                var startX = (StartPosition.vert1.x + StartPosition.vert2.x + StartPosition.vert3.x)/3;
                var startY = (StartPosition.vert1.y + StartPosition.vert2.y + StartPosition.vert3.y)/3;
                var startz = (StartPosition.vert1.z + StartPosition.vert2.z + StartPosition.vert3.z)/3;

                var guideX = (Trajectory.vert1.x + Trajectory.vert2.x + Trajectory.vert3.x)/3;
                var guideY = (Trajectory.vert1.y + Trajectory.vert2.y + Trajectory.vert3.y)/3;
                var guideZ = (Trajectory.vert1.z + Trajectory.vert2.z + Trajectory.vert3.z)/3;

                var particleX = startX + (guideX - startX) * (float)random.NextDouble();
                var particleY = startY + (guideY - startY) * (float)random.NextDouble();
                var particleZ = startz + (guideZ - startz) * (float)random.NextDouble();

                var particle = new Particle();                                
                particle.Velocity = new Vector3()
                {
                    x = (float)random.NextDouble() * 2 - 1,
                    y = (float)random.NextDouble() * 2 - 1,
                    z = (float)random.NextDouble() * 2 - 1
                };
                //particle.Velocity = particle.Velocity.Normalize();
                //particle.Velocity = particle.Velocity * (float)random.NextDouble() * 0.1f;
                //particle.Acceleration = Trajectory;
                //particle.Acceleration = particle.Acceleration.Normalize();
                //particle.Acceleration = particle.Acceleration * (float)random.NextDouble() * 0.1f;
                particle.Life = random.NextInt64(2,4);
                //Size is a value between 2 and 5
                particle.Size = random.NextInt64(2,5);
                particle.BirthTime = DateTime.Now;
                particle.Color = "ff0000";                
                Particles.Add(particle);                
            }            
        }
    }
    public class Particle : IParticle
    {
        public DateTime BirthTime { get; set; } = DateTime.Now;
        public ITriangleMeshWithColor Position { get; set; }
        public IVector3 Velocity { get; set; }
        public IVector3 Acceleration { get; set; }
        public float Life { get; set; }
        public float Size { get; set; }
        public string Color { get; set; }
    }
}
