using System.Collections.Generic;
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
            int count = activeWorld.Count;

            Parallel.For(0, count, i =>
            {
                var inhabitant = activeWorld[i];
                if (inhabitant == null || inhabitant.CrashBoxes == null) return;

                for (int j = i + 1; j < count; j++)
                {
                    var otherInhabitant = activeWorld[j];
                    if (otherInhabitant == null || otherInhabitant.CrashBoxes == null) continue;

                    // Skip particle-to-particle collisions early
                    if (inhabitant.ObjectName == "Particle" && otherInhabitant.ObjectName == "Particle")
                        continue;

                    var rotatedCrashboxes = RotateAllCrashboxes(inhabitant.CrashBoxes, (Vector3)inhabitant.Rotation, (Vector3)inhabitant.Position);
                    var rotatedOtherCrashboxes = RotateAllCrashboxes(otherInhabitant.CrashBoxes, (Vector3)otherInhabitant.Rotation, (Vector3)otherInhabitant.Position);

                    foreach (var crashbox in rotatedCrashboxes)
                    {
                        foreach (var otherCrashbox in rotatedOtherCrashboxes)
                        {
                            if (_3dObjectHelpers.CheckCollisionBoxVsBox(crashbox, otherCrashbox))
                            {
                                inhabitant.HasCrashed = true;
                                otherInhabitant.HasCrashed = true;
                                return; // Early exit once a collision is found
                            }
                        }
                    }
                }
            });
        }

        private static List<List<Vector3>> RotateAllCrashboxes(List<List<IVector3>> crashboxes, Vector3 rotation, Vector3 position)
        {
            List<List<Vector3>> rotatedCrashboxes = new List<List<Vector3>>(crashboxes.Count);

            foreach (var crashbox in crashboxes)
            {
                List<Vector3> rotated = new List<Vector3>(crashbox.Count);
                foreach (var point in crashbox)
                {
                    rotated.Add(RotatePoint(point, rotation, position));
                }
                rotatedCrashboxes.Add(rotated);
            }

            return rotatedCrashboxes;
        }

        private static Vector3 RotatePoint(IVector3 point, Vector3 rotation, Vector3 position)
        {
            // Maintain your existing logic for rotation using a mesh-based approach
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

            // Apply position offset after rotation
            rotatedPoint.x += position.x;
            rotatedPoint.y += position.y;
            rotatedPoint.z += position.z;

            return (Vector3)rotatedPoint;
        }
    }
}
