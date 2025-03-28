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

            Parallel.For(0, count, i =>
            {
                var inhabitant = activeWorld[i];
                if (inhabitant == null || inhabitant.CrashBoxes == null) return;

                for (int j = i + 1; j < count; j++)
                {
                    var otherInhabitant = activeWorld[j];
                    if (otherInhabitant == null || otherInhabitant.CrashBoxes == null) continue;

                    bool isInhabitantStatic = IsStatic(inhabitant.ObjectName);
                    bool isOtherStatic = IsStatic(otherInhabitant.ObjectName);

                    if (inhabitant.ObjectName == otherInhabitant.ObjectName) continue;
                    if (isInhabitantStatic && isOtherStatic) continue;
                    if ((isInhabitantStatic || isOtherStatic) && !shouldCheckStaticObjects) continue;

                    if (isInhabitantStatic || isOtherStatic)
                        _lastStaticCheck = DateTime.Now;

                    var inhabitantWorldOffset = (inhabitant.WorldPosition.x == 0 && inhabitant.WorldPosition.y == 0 && inhabitant.WorldPosition.z == 0)
                        ? _3dObjectHelpers.FindWorldPosition(inhabitant)
                        : (Vector3)inhabitant.WorldPosition;

                    var otherWorldOffset = (otherInhabitant.WorldPosition.x == 0 && otherInhabitant.WorldPosition.y == 0 && otherInhabitant.WorldPosition.z == 0)
                        ? _3dObjectHelpers.FindWorldPosition(otherInhabitant)
                        : (Vector3)otherInhabitant.WorldPosition;

                    var rotatedCrashBoxes = RotateAllCrashboxes(inhabitant.CrashBoxes, (Vector3)inhabitant.Rotation, (Vector3)inhabitant.Position, inhabitantWorldOffset);
                    var rotatedOtherCrashBoxes = RotateAllCrashboxes(otherInhabitant.CrashBoxes, (Vector3)otherInhabitant.Rotation, (Vector3)otherInhabitant.Position, otherWorldOffset);

                    var surfaceTriangle = inhabitant?.ParentSurface?.RotatedSurfaceTriangles.Find(tri => tri.landBasedPosition == inhabitant.SurfaceBasedId);

                    foreach (var crashBox in rotatedCrashBoxes)
                    {
                        foreach (var otherCrashBox in rotatedOtherCrashBoxes)
                        {
                            Logger.Log($"[Check] {inhabitant.ObjectName} vs {otherInhabitant.ObjectName}");

                            var yMin1 = crashBox.Min(p => p.y);
                            var yMax1 = crashBox.Max(p => p.y);
                            var yMin2 = otherCrashBox.Min(p => p.y);
                            var yMax2 = otherCrashBox.Max(p => p.y);

                            var xMin1 = crashBox.Min(p => p.x);
                            var xMax1 = crashBox.Max(p => p.x);
                            var xMin2 = otherCrashBox.Min(p => p.x);
                            var xMax2 = otherCrashBox.Max(p => p.x);

                            var zMin1 = crashBox.Min(p => p.z);
                            var zMax1 = crashBox.Max(p => p.z);
                            var zMin2 = otherCrashBox.Min(p => p.z);
                            var zMax2 = otherCrashBox.Max(p => p.z);

                            Logger.Log($"Y-range: [{yMin1}–{yMax1}] vs [{yMin2}–{yMax2}]");
                            Logger.Log($"X-range: [{xMin1}–{xMax1}] vs [{xMin2}–{xMax2}]");
                            Logger.Log($"Z-range: [{zMin1}–{zMax1}] vs [{zMin2}–{zMax2}]");

                            LogCrashbox("Inhabitant Box", crashBox);
                            LogCrashbox("Other Box", otherCrashBox);

                            //Surfacebased Crashboxes must be centered and put on top of the surface
                            //CenterCrashBoxIfSurfaceBased(inhabitant, crashBox);
                            //CenterCrashBoxIfSurfaceBased(otherInhabitant, otherCrashBox);

                            if (_3dObjectHelpers.CheckCollisionBoxVsBox(crashBox, otherCrashBox))
                            {
                                Logger.Log($"[COLLISION] {inhabitant.ObjectName} <-> {otherInhabitant.ObjectName}");
                                inhabitant.HasCrashed = true;
                                otherInhabitant.HasCrashed = true;
                                return;
                            }
                        }
                    }
                }
            });
        }

        private static void CenterCrashBoxIfSurfaceBased(_3dObject obj, List<Vector3> box)
        {
            if (obj?.SurfaceBasedId > 0)
            {
                var tri = obj.ParentSurface?.RotatedSurfaceTriangles.Find(t => t.landBasedPosition == obj.SurfaceBasedId);
                if (tri != null)
                    _3dObjectHelpers.CenterCrashBoxAt(box, tri.vert1);
            }
        }

        private static void LogCrashbox(string label, List<Vector3> box)
        {
            Logger.Log($"--- {label} ({box.Count} pts) ---");
            foreach (var p in box)
                Logger.Log($"(x={p.x}, y={p.y}, z={p.z})");
            Logger.Log($"--- End of {label} ---\n");
        }

        public static bool IsStatic(string objectName) =>
            objectName == "Tree" || objectName == "Surface" || objectName == "House";


        //TODO: Must do some changes here, to center correctly we need to add the world positions and offsets in the end
        private static List<List<Vector3>> RotateAllCrashboxes(List<List<IVector3>> crashboxes, Vector3 rotation, Vector3 position, Vector3 worldPosition)
        {
            var rotatedCrashboxes = new List<List<Vector3>>(crashboxes.Count);
            foreach (var crashbox in crashboxes)
            {
                var rotated = new List<Vector3>(crashbox.Count);
                foreach (var point in crashbox)
                {
                    rotated.Add(RotatePoint(point, rotation, position, worldPosition));
                }
                rotatedCrashboxes.Add(rotated);
            }
            return rotatedCrashboxes;
        }

        private static Vector3 RotatePoint(IVector3 point, Vector3 rotation, Vector3 position, Vector3 worldPosition)
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

            rotatedPoint.x += worldPosition.x + position.x;
            rotatedPoint.y += worldPosition.y + position.y;
            rotatedPoint.z += worldPosition.z + position.z;

            return (Vector3)rotatedPoint;
        }
    }
}
