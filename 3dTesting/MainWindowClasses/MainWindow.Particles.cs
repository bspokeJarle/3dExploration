using _3dTesting._3dRotation;
using _3dTesting._3dWorld;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.MainWindowClasses
{
    public class ParticleManager
    {
        private _3dRotate Rotate3d = new _3dRotate();

        public void HandleParticles(_3dObject inhabitant, List<_3dObject> particleObjectList)
        {
            if (inhabitant.Particles == null || inhabitant.Particles.Particles.Count == 0)
                return;

            foreach (var particle in inhabitant.Particles.Particles)
            {
                if (!particle.Visible) continue;

                var particleObject = new _3dObject { ObjectName = "Particle" };
                var particleTriangle = RotateParticle(particle.ParticleTriangle, (Vector3)particle.Rotation);

                particleObject.ObjectParts = new List<I3dObjectPart>
            {
                new _3dObjectPart { Triangles = new List<ITriangleMeshWithColor> { particleTriangle }, PartName = "Particle", IsVisible = true }
            };
                particleObject.Position = new Vector3
                {
                    x = inhabitant.Position.x + particle.Position.x,
                    y = inhabitant.Position.y + particle.Position.y,
                    z = inhabitant.Position.z + particle.Position.z
                };
                particleObject.Rotation = particle.Rotation;
                particleObjectList.Add(particleObject);
            }
        }

        private ITriangleMeshWithColor RotateParticle(ITriangleMeshWithColor particleTriangle, Vector3 rotation)
        {
            var rotated = Rotate3d.RotateZMesh(new List<ITriangleMeshWithColor> { particleTriangle }, rotation.z).First();
            rotated = Rotate3d.RotateYMesh(new List<ITriangleMeshWithColor> { rotated }, rotation.y).First();
            rotated = Rotate3d.RotateXMesh(new List<ITriangleMeshWithColor> { rotated }, rotation.x).First();
            return rotated;
        }
    }
}
