﻿using System;
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

                    ObjectPlacementHelpers.TryGetCrashboxWorldPosition(inhabitant, out var inhabitantWorldOffset);
                    ObjectPlacementHelpers.TryGetCrashboxWorldPosition(otherInhabitant, out var otherWorldOffset);

                    var rotatedCrashBoxes = RotateAllCrashboxes(inhabitant.CrashBoxes, (Vector3)inhabitant.Rotation, (Vector3)inhabitant.Position, inhabitantWorldOffset, inhabitant.SurfaceBasedId > 0);
                    var rotatedOtherCrashBoxes = RotateAllCrashboxes(otherInhabitant.CrashBoxes, (Vector3)otherInhabitant.Rotation, (Vector3)otherInhabitant.Position, otherWorldOffset, otherInhabitant.SurfaceBasedId > 0);

                    foreach (var crashBox in rotatedCrashBoxes)
                    {
                        foreach (var otherCrashBox in rotatedOtherCrashBoxes)
                        {
                            CenterCrashBoxIfSurfaceBased(inhabitant, crashBox);
                            CenterCrashBoxIfSurfaceBased(otherInhabitant, otherCrashBox);

                            if (otherInhabitant.ObjectName == "Surface" &&
                                (inhabitant.ObjectName == "Tree" || inhabitant.ObjectName == "House"))
                            {
                                ObjectPlacementHelpers.LogCrashboxContact(inhabitant.ObjectName, inhabitant, crashBox, otherInhabitant, otherCrashBox);
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
                }
            });
        }

        private static void CenterCrashBoxIfSurfaceBased(_3dObject obj, List<Vector3> box)
        {
            if (obj?.SurfaceBasedId > 0)
            {
                var tri = obj.ParentSurface?.RotatedSurfaceTriangles.Find(t => t.landBasedPosition == obj.SurfaceBasedId);
                if (tri != null)
                    ObjectPlacementHelpers.CenterCrashBoxAt(box, tri.vert1);
            }
        }

        public static bool IsStatic(string objectName) =>
            objectName == "Tree" || objectName == "Surface" || objectName == "House";

        private static List<List<Vector3>> RotateAllCrashboxes(List<List<IVector3>> crashboxes, Vector3 rotation, Vector3 position, Vector3 worldPosition, bool surfaceBased)
        {
            var rotatedCrashboxes = new List<List<Vector3>>(crashboxes.Count);
            foreach (var crashbox in crashboxes)
            {
                var rotated = new List<Vector3>(crashbox.Count);
                foreach (var point in crashbox)
                {
                    rotated.Add(RotatePoint(point, rotation, position, worldPosition, surfaceBased));
                }
                rotatedCrashboxes.Add(rotated);
            }
            return rotatedCrashboxes;
        }

        private static Vector3 RotatePoint(IVector3 point, Vector3 rotation, Vector3 position, Vector3 worldPosition, bool surfaceBased)
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
            rotatedPoint.y += surfaceBased ? worldPosition.y : worldPosition.y + position.y;
            rotatedPoint.z += worldPosition.z + position.z;

            return (Vector3)rotatedPoint;
        }
    }
}
