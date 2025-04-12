using _3dTesting._3dWorld;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.Helpers
{
    public static class ObjectPlacementHelpers
    {
        public static bool EnablePlacementLogging = true;

        private static ITriangleMeshWithColor? GetSurfaceTriangle(_3dObject obj)
        {
            return obj?.ParentSurface?.RotatedSurfaceTriangles
                .FirstOrDefault(t => t.landBasedPosition == obj.SurfaceBasedId);
        }

        public static (float x, float y, float z) GetCrashBoxCenter(List<List<IVector3>> crashBoxes)
        {
            var allPoints = crashBoxes.SelectMany(box => box).Cast<Vector3>().ToList();
            return (
                allPoints.Average(p => p.x),
                allPoints.Average(p => p.y),
                allPoints.Average(p => p.z)
            );
        }

        public static bool TryGetRenderPosition(_3dObject obj, int screenCenterX, int screenCenterY, out double x, out double y, out double z)
        {
            x = y = z = 0;
            if (obj == null) return false;

            var localWorldPosition = obj.GetLocalWorldPosition();

            if (localWorldPosition == null)
            {
                if (obj.SurfaceBasedId > 0)
                {
                    var triangle = GetSurfaceTriangle(obj);
                    if (triangle == null) return false;

                    CenterObjectAt(obj, triangle.vert1);
                    CenterCrashBoxesAt(obj, triangle.vert1);
                    x = screenCenterX + obj.ObjectOffsets.x;
                    y = screenCenterY + obj.ObjectOffsets.y;
                    z = obj.ObjectOffsets.z;
                }
                else
                {
                    x = screenCenterX + obj.ObjectOffsets.x;
                    y = screenCenterY + obj.ObjectOffsets.y;
                    z = obj.ObjectOffsets.z;
                }
            }
            else
            {
                x = screenCenterX + localWorldPosition.x + obj.ObjectOffsets.x;
                y = screenCenterY + localWorldPosition.y + obj.ObjectOffsets.y;
                z = localWorldPosition.z + obj.ObjectOffsets.z;
            }

            return true;
        }

        public static void CenterObjectAt(I3dObject obj, IVector3 targetPosition)
        {
            if (obj == null || targetPosition == null)
                return;

            IVector3 objectCenter = GetObjectGeometricCenter(obj, true);

            float shiftX = targetPosition.x - objectCenter.x;
            float shiftY = targetPosition.y - objectCenter.y;
            float shiftZ = targetPosition.z - objectCenter.z;

            foreach (var part in obj.ObjectParts)
            {
                foreach (var triangle in part.Triangles)
                {
                    triangle.vert1.x += shiftX;
                    triangle.vert1.y += shiftY;
                    triangle.vert1.z += shiftZ;

                    triangle.vert2.x += shiftX;
                    triangle.vert2.y += shiftY;
                    triangle.vert2.z += shiftZ;

                    triangle.vert3.x += shiftX;
                    triangle.vert3.y += shiftY;
                    triangle.vert3.z += shiftZ;
                }
            }
        }

        public static void CenterCrashBoxesAt(_3dObject obj, IVector3 targetPosition)
        {
            if (obj?.CrashBoxes == null || obj.CrashBoxes.Count == 0 || targetPosition == null)
                return;

            var allPoints = obj.CrashBoxes.SelectMany(box => box).Cast<Vector3>().ToList();

            if (allPoints.Count == 0) return;

            var center = new Vector3
            {
                x = allPoints.Average(p => p.x),
                y = allPoints.Min(p => p.y),
                z = allPoints.Average(p => p.z)
            };

            float shiftX = targetPosition.x - center.x;
            float shiftY = targetPosition.y - center.y;
            float shiftZ = targetPosition.z - center.z;

            for (int i = 0; i < obj.CrashBoxes.Count; i++)
            {
                for (int j = 0; j < obj.CrashBoxes[i].Count; j++)
                {
                    var p = (Vector3)obj.CrashBoxes[i][j];
                    obj.CrashBoxes[i][j] = new Vector3
                    {
                        x = p.x + shiftX,
                        y = p.y + shiftY,
                        z = p.z + shiftZ
                    };
                }
            }
        }

        public static IVector3 GetObjectGeometricCenter(I3dObject obj, bool snapToBottomY = false)
        {
            float sumX = 0, sumY = 0, sumZ = 0;
            int count = 0;
            float minY = float.MaxValue;

            foreach (var part in obj.ObjectParts)
            {
                foreach (var triangle in part.Triangles)
                {
                    var vertices = new[] { triangle.vert1, triangle.vert2, triangle.vert3 };

                    foreach (var v in vertices)
                    {
                        sumX += v.x;
                        sumY += v.y;
                        sumZ += v.z;
                        count++;

                        if (snapToBottomY)
                            minY = Math.Min(minY, v.y);
                    }
                }
            }

            if (count == 0) return new Vector3();

            return new Vector3
            {
                x = sumX / count,
                y = snapToBottomY ? minY : sumY / count,
                z = sumZ / count
            };
        }

        public static void CenterCrashBoxAt(List<Vector3> crashBox, IVector3 targetPosition, IVector3 crashboxOffsets)
        {
            if (crashBox == null || crashBox.Count == 0 || targetPosition == null) return;

            float minY = crashBox.Min(p => p.y);
            float shiftY = targetPosition.y - minY + crashboxOffsets.y;

            if (EnablePlacementLogging)
            {
                Logger.Log("[CenterAt] Target Y: " + targetPosition.y + ", CrashBox MinY: " + minY + ", CrashBoxOffsetY: " + crashboxOffsets.y + ", Final ShiftY: " + shiftY);
            }

            for (int i = 0; i < crashBox.Count; i++)
            {
                crashBox[i] = new Vector3
                {
                    x = crashBox[i].x,
                    y = crashBox[i].y + shiftY,
                    z = crashBox[i].z
                };
            }
        }


        public static void LogCrashboxAnalysis(string label, List<Vector3> box)
        {
            if (!EnablePlacementLogging || box == null || box.Count == 0) return;

            var yMin = box.Min(p => p.y);
            var yMax = box.Max(p => p.y);
            var xMin = box.Min(p => p.x);
            var xMax = box.Max(p => p.x);
            var zMin = box.Min(p => p.z);
            var zMax = box.Max(p => p.z);

            var center = new Vector3
            {
                x = box.Average(p => p.x),
                y = box.Average(p => p.y),
                z = box.Average(p => p.z)
            };

            Logger.Log("--- " + label + " ---");
            Logger.Log("Y-range: [" + yMin + "–" + yMax + "], X-range: [" + xMin + "–" + xMax + "], Z-range: [" + zMin + "–" + zMax + "]");
            Logger.Log("Center: (x=" + center.x.ToString("F1") + ", y=" + center.y.ToString("F1") + ", z=" + center.z.ToString("F1") + ")");
            foreach (var p in box)
                Logger.Log("(x=" + p.x.ToString("F1") + ", y=" + p.y.ToString("F1") + ", z=" + p.z.ToString("F1") + ")");
            Logger.Log("--- End of " + label + " ---\n");
        }
    }
}
