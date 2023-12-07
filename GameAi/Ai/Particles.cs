using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Domain._3dSpecificsImplementations;

namespace GameAiAndControls.Ai
{
    //make a class that will spawn particles that follow a trajectory
    public class ParticlesAI : Iparticles
    {
        public List<ITriangleMeshWithColor> particles = new();
        private Random random = new();
        public List<ITriangleMeshWithColor> MoveParticles()
        {
            //todo
            //move particles according to their velocity, acceleration and direction
            //when they hit something, they should bounce off
            //when they hit something, they should lose some of their velocity
            //when they hit something, they should lose some of their life
            //when they lose all their life, they should be removed from the list
            return particles;
        }
      
        public List<ITriangleMeshWithColor> ReleaseParticles(IVector3 Trajectory, IVector3 StartPosition)
        {            
            long particleCount = random.NextInt64(100);
            for (int i = 0; i < particleCount; i++)
            {
                var particle = new Particle();
                particle.Position = StartPosition;
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
                particle.Life = 1;
                particle.Size = 1;
                particle.Color = "ff0000";
                //particles.Add(particle);
            }
            return particles;
        }
    }
    public class Particle : IParticle
    {
        public IVector3 Position { get; set; }
        public IVector3 Velocity { get; set; }
        public IVector3 Acceleration { get; set; }
        public float Life { get; set; }
        public float Size { get; set; }
        public string Color { get; set; }
    }
}
