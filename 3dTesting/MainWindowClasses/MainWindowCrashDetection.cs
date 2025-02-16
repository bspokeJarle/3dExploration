using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _3dTesting._3dWorld;
using Domain;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.Helpers
{
    public static class CrashDetection
    {
        public static void HandleCrashboxes(List<_3dObject> activeWorld)
        {
            Parallel.ForEach(activeWorld, inhabitant =>
            {
                foreach (var otherInhabitant in activeWorld)
                {
                    if (inhabitant == null || otherInhabitant == null ||
                        inhabitant == otherInhabitant || otherInhabitant.CrashBoxes == null || inhabitant.CrashBoxes == null)
                        continue;

                    foreach (var crashbox in inhabitant.CrashBoxes)
                    {
                        foreach (var otherCrashbox in otherInhabitant.CrashBoxes)
                        {
                            var rotatedCrashbox = RotateCrashbox(crashbox, (Vector3)inhabitant.Rotation, (Vector3)inhabitant.Position);
                            var rotatedOtherCrashbox = RotateCrashbox(otherCrashbox, (Vector3)otherInhabitant.Rotation, (Vector3)otherInhabitant.Position);

                            if (_3dObjectHelpers.CheckCollisionBoxVsBox(rotatedCrashbox, rotatedOtherCrashbox))
                            {
                                // Skip particle-to-particle collisions
                                if (inhabitant.ObjectName == "Particle" && otherInhabitant.ObjectName == "Particle")
                                    continue;

                                inhabitant.HasCrashed = true;
                                otherInhabitant.HasCrashed = true;
                            }
                        }
                    }
                }
            });
        }

        private static List<Vector3> RotateCrashbox(List<IVector3> crashbox, Vector3 rotation, Vector3 position)
        {
            return crashbox.Select(point => RotatePoint(point, rotation, position)).ToList();
        }

        private static Vector3 RotatePoint(IVector3 point, Vector3 rotation, Vector3 position)
        {
            var singleTriangle = new List<ITriangleMeshWithColor>
            {
                new TriangleMeshWithColor
                {
                    vert1 = new Vector3 { x = point.x, y = point.y, z = point.z },
                    vert2 = new Vector3 { x = point.x, y = point.y, z = point.z },
                    vert3 = new Vector3 { x = point.x, y = point.y, z = point.z }
                }
            };

            var rotatedTriangle = GameHelpers.RotateMesh(singleTriangle, rotation);
            var rotatedPoint = rotatedTriangle[0].vert1;

            // Apply the position offset after rotation
            rotatedPoint.x += position.x;
            rotatedPoint.y += position.y;
            rotatedPoint.z += position.z;

            return (Vector3)rotatedPoint;
        }
    }
}
