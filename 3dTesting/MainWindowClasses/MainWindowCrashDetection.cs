using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using _3dTesting._3dWorld;
using Domain;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.Helpers
{
    public static class CrashDetection
    {
        public static DateTime staticOjectLastCheck { get; set; } = new DateTime();
        private static DateTime _lastStaticCheck = DateTime.MinValue;

        public static void HandleCrashboxes(List<_3dObject> activeWorld)
        {
            int count = activeWorld.Count;
            bool shouldCheckStaticObjects = (DateTime.Now - _lastStaticCheck).TotalMilliseconds > 100;

            for (int i = 0; i < count; i++)
            {
                var inhabitant = activeWorld[i];
                if (inhabitant == null || inhabitant.CrashBoxes == null) continue;

                for (int j = i + 1; j < count; j++)
                {
                    var otherInhabitant = activeWorld[j];
                    if (otherInhabitant == null || otherInhabitant.CrashBoxes == null) continue;

                    bool isInhabitantStatic = IsStatic(inhabitant.ObjectName);
                    bool isOtherStatic = IsStatic(otherInhabitant.ObjectName);

                    if (inhabitant.ObjectName == otherInhabitant.ObjectName) continue;
                    if (isInhabitantStatic && isOtherStatic) continue;
                    if ((isInhabitantStatic || isOtherStatic) && !shouldCheckStaticObjects) continue;

                    if (isInhabitantStatic || isOtherStatic) _lastStaticCheck = DateTime.Now;

                    if (!RoughAABBOverlap(inhabitant, otherInhabitant))
                    {
                        Logger.Log($"[EarlySkip] {inhabitant.ObjectName} vs {otherInhabitant.ObjectName} – No rough AABB overlap.");
                        continue;
                    }

                    Logger.Log("----------------------------------------------------");
                    Logger.Log($"[CrashCheck] Checking Start {inhabitant.ObjectName} vs {otherInhabitant.ObjectName}");

                    ObjectPlacementHelpers.TryGetCrashboxWorldPosition(inhabitant, out var inhabitantWorldOffset);
                    ObjectPlacementHelpers.TryGetCrashboxWorldPosition(otherInhabitant, out var otherWorldOffset);

                    var rotatedCrashBoxes = RotateAllCrashboxes(inhabitant.CrashBoxes, (Vector3)inhabitant.Rotation, (Vector3)inhabitant.ObjectOffsets, inhabitantWorldOffset, inhabitant.ObjectName);
                    var rotatedOtherCrashBoxes = RotateAllCrashboxes(otherInhabitant.CrashBoxes, (Vector3)otherInhabitant.Rotation, (Vector3)otherInhabitant.ObjectOffsets, otherWorldOffset, inhabitant.ObjectName);

                    foreach (var crashBox in rotatedCrashBoxes)
                    {
                        foreach (var otherCrashBox in rotatedOtherCrashBoxes)
                        {
                            CenterCrashBoxIfSurfaceBased(inhabitant, crashBox);
                            CenterCrashBoxIfSurfaceBased(otherInhabitant, otherCrashBox);

                            if (Logger.EnableFileLogging)
                            {
                                ObjectPlacementHelpers.LogCrashboxContact(inhabitant.ObjectName, inhabitant, crashBox, otherInhabitant, otherCrashBox);
                                ObjectPlacementHelpers.LogCrashboxAnalysis($"{inhabitant.ObjectName} CrashBox", crashBox);
                                ObjectPlacementHelpers.LogCrashboxAnalysis($"{otherInhabitant.ObjectName} CrashBox", otherCrashBox);
                            }

                            if (_3dObjectHelpers.CheckCollisionBoxVsBox(crashBox, otherCrashBox))
                            {
                                Logger.Log($"[COLLISION] {inhabitant.ObjectName} <-> {otherInhabitant.ObjectName}");
                                inhabitant.HasCrashed = true;
                                otherInhabitant.HasCrashed = true;
                                return;
                            }
                        }
                    }
                    Logger.Log($"[CrashCheck] Checking End {inhabitant.ObjectName} vs {otherInhabitant.ObjectName}");
                }
            }
        }

        private static void CenterCrashBoxIfSurfaceBased(_3dObject obj, List<Vector3> box)
        {
            if (obj?.SurfaceBasedId > 0)
            {
                var tri = obj.ParentSurface?.RotatedSurfaceTriangles.Find(t => t.landBasedPosition == obj.SurfaceBasedId);
                if (tri != null)
                    ObjectPlacementHelpers.CenterCrashBoxAt(box, tri.vert1, obj.CrashboxOffsets);
            }
        }

        public static bool IsStatic(string objectName) =>
            objectName == "Tree" || objectName == "Surface" || objectName == "House";

        private static List<List<Vector3>> RotateAllCrashboxes(List<List<IVector3>> crashboxes, Vector3 rotation, Vector3 objectOffsets, Vector3 worldPosition, string objectName)
        {
            var rotatedCrashboxes = new List<List<Vector3>>(crashboxes.Count);
            foreach (var crashbox in crashboxes)
            {
                var rotated = new List<Vector3>(crashbox.Count);
                foreach (var point in crashbox)
                {
                    rotated.Add(RotatePoint(point, rotation, objectOffsets, worldPosition, objectName));
                }
                rotatedCrashboxes.Add(rotated);
            }
            return rotatedCrashboxes;
        }

        private static Vector3 RotatePoint(IVector3 point, Vector3 rotation, Vector3 objectOffsets, Vector3 worldPosition, string objectName)
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

            rotatedPoint.x += worldPosition.x + objectOffsets.x;
            rotatedPoint.y += worldPosition.y + objectOffsets.y;
            rotatedPoint.z += worldPosition.z + objectOffsets.z;

            return (Vector3)rotatedPoint;
        }

        private static bool RoughAABBOverlap(_3dObject a, _3dObject b, float margin = 150f)
        {
            foreach (var boxA in a.CrashBoxes)
            {
                foreach (var boxB in b.CrashBoxes)
                {
                    var minA = new Vector3(boxA.Min(p => p.x) - margin, boxA.Min(p => p.y) - margin, boxA.Min(p => p.z) - margin);
                    var maxA = new Vector3(boxA.Max(p => p.x) + margin, boxA.Max(p => p.y) + margin, boxA.Max(p => p.z) + margin);

                    var minB = new Vector3(boxB.Min(p => p.x) - margin, boxB.Min(p => p.y) - margin, boxB.Min(p => p.z) - margin);
                    var maxB = new Vector3(boxB.Max(p => p.x) + margin, boxB.Max(p => p.y) + margin, boxB.Max(p => p.z) + margin);

                    bool overlapX = maxA.x >= minB.x && minA.x <= maxB.x;
                    bool overlapY = maxA.y >= minB.y && minA.y <= maxB.y;
                    bool overlapZ = maxA.z >= minB.z && minA.z <= maxB.z;

                    if (overlapX && overlapY && overlapZ)
                        return true;
                }
            }
            return false;
        }
    }
}
