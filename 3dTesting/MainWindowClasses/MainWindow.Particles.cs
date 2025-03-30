using _3dTesting._3dRotation;
using _3dTesting._3dWorld;
using Domain;
using GameAiAndControls.Controls;
using System.Collections.Generic;
using System.Linq;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.MainWindowClasses
{
    public class ParticleManager
    {
        private readonly _3dRotate Rotate3d = new();

        public void HandleParticles(_3dObject inhabitant, List<_3dObject> particleObjectList)
        {
            if (inhabitant.Particles?.Particles is null || inhabitant.Particles.Particles.Count == 0)
                return;

            List<Particle> particles;
            lock (inhabitant.Particles)
            {
                particles = inhabitant.Particles.Particles.OfType<Particle>().Where(p => p.Visible).ToList();
            }

            foreach (var particle in particles)
            {
                var particleTriangle = RotateParticle(particle.ParticleTriangle, particle.Rotation as Vector3);

                particleObjectList.Add(new _3dObject
                {
                    ObjectName = "Particle",
                    WorldPosition = particle.WorldPosition,
                    ParentSurface = inhabitant.ParentSurface,
                    ObjectParts = new List<I3dObjectPart>
                    {
                        new _3dObjectPart { Triangles = new List<ITriangleMeshWithColor> { particleTriangle }, PartName = "Particle", IsVisible = true }
                    },
                    ObjectOffsets = new Vector3
                    {
                        x = inhabitant.ObjectOffsets.x + particle.Position.x,
                        y = inhabitant.ObjectOffsets.y + particle.Position.y,
                        z = inhabitant.ObjectOffsets.z + particle.Position.z
                    },
                    Rotation = particle.Rotation
                });
            }
        }

        private ITriangleMeshWithColor RotateParticle(ITriangleMeshWithColor particleTriangle, Vector3 rotation)
        {
            return Rotate3d.RotateXMesh(
                Rotate3d.RotateYMesh(
                    Rotate3d.RotateZMesh(new List<ITriangleMeshWithColor> { particleTriangle }, rotation.z),
                    rotation.y
                ),
                rotation.x
            ).First();
        }
    }
}
