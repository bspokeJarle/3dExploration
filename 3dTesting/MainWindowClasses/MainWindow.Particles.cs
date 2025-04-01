using _3dTesting._3dRotation;
using _3dTesting._3dWorld;
using Domain;
using GameAiAndControls.Controls;
using System;
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
                    CrashBoxes = CreateCrashBoxFromTriangle(particleTriangle),
                    CrashboxOffsets = new Vector3 { x = 0, y = 0, z = 0 },
                    Rotation = particle.Rotation
                });
            }
        }

        //Creates a crashbox from a triangle
        private List<List<IVector3>> CreateCrashBoxFromTriangle(ITriangleMeshWithColor triangle)
        {
            float minX = MathF.Min(triangle.vert1.x, MathF.Min(triangle.vert2.x, triangle.vert3.x));
            float maxX = MathF.Max(triangle.vert1.x, MathF.Max(triangle.vert2.x, triangle.vert3.x));

            float minY = MathF.Min(triangle.vert1.y, MathF.Min(triangle.vert2.y, triangle.vert3.y));
            float maxY = MathF.Max(triangle.vert1.y, MathF.Max(triangle.vert2.y, triangle.vert3.y));

            float minZ = MathF.Min(triangle.vert1.z, MathF.Min(triangle.vert2.z, triangle.vert3.z));
            float maxZ = MathF.Max(triangle.vert1.z, MathF.Max(triangle.vert2.z, triangle.vert3.z));

            return new List<List<IVector3>>
            {
                new List<IVector3>
                {
                    new Vector3 { x = minX, y = minY, z = minZ },
                    new Vector3 { x = maxX, y = maxY, z = maxZ }
                }
            };
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
