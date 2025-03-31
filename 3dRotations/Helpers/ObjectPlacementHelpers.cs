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
        private static ITriangleMeshWithColor? GetSurfaceTriangle(_3dObject obj)
        {
            return obj?.ParentSurface?.RotatedSurfaceTriangles
                .FirstOrDefault(t => t.landBasedPosition == obj.SurfaceBasedId);
        }

        private static Vector3 GetGlobalMapOffset(_3dObject obj)
        {
            return new Vector3
            {
                x = obj.ParentSurface.GlobalMapPosition.x,
                y = obj.ParentSurface.GlobalMapPosition.y,
                z = obj.ParentSurface.GlobalMapPosition.z
            };
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

            //Use offset from specified in the Scene
            // Center the object at the bottom, meaning place it on top
            IVector3 objectCenter = GetObjectGeometricCenter(obj, true);

            // Compute the shift required
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

        public static bool TryGetCrashboxWorldPosition(_3dObject obj, out Vector3 position)
        {
            position = new Vector3();
            if (obj == null) return false;
            var worldPos = GetWorldPosition(obj);
            position = new Vector3
            {
                x = worldPos.x,
                y = worldPos.y,
                z = worldPos.z
            };
            return true;
        }

        public static Vector3 GetWorldPosition(_3dObject obj)
        {
            if (obj == null) return new Vector3();

            if (obj.SurfaceBasedId > 0)
            {
                var triangle = GetSurfaceTriangle(obj);
                //Logger.Log($"[WorldPos Surfacebased] {obj.ObjectName} => ({obj.ParentSurface.GlobalMapPosition.x + obj.Position.x}, {triangle.vert1.y + obj.Position.y}, {obj.ParentSurface.GlobalMapPosition.z + obj.Position.z})");       
                if (triangle != null)
                {
                    return new Vector3
                    {
                        x = obj.ParentSurface.GlobalMapPosition.x + obj.CrashboxOffsets.x,
                        //Set this to 0 - the Y coordinate will be centered later, so setting anything here is futile
                        y = 0,
                        z = obj.ParentSurface.GlobalMapPosition.z + obj.CrashboxOffsets.z
                    };
                }
            }

            if (obj.ObjectName == "Ship")
            {
                /*var centerLog = GetCenterWorldPosition(
                    obj.ParentSurface.GlobalMapPosition,
                    obj.ParentSurface.SurfaceWidth(),
                    obj.ParentSurface.TileSize(),
                    (Vector3)obj.Position);

                Logger.Log($"[WorldPos Ship] {obj.ObjectName} => ({centerLog.x + obj.Position.x}, {centerLog.y + obj.Position.y}, {centerLog.z + obj.Position.z})");*/

                return GetCenterWorldPosition(
                    obj.ParentSurface.GlobalMapPosition,
                    obj.ParentSurface.SurfaceWidth(),
                    obj.ParentSurface.TileSize(),
                    (Vector3)obj.ObjectOffsets,(Vector3)obj.CrashboxOffsets);
            }

            //Logger.Log($"[WorldPos Surface] {obj.ObjectName} => ({obj.ParentSurface.GlobalMapPosition.x + obj.Position.x}, {obj.ParentSurface.GlobalMapPosition.y + obj.Position.y}, {obj.ParentSurface.GlobalMapPosition.z + obj.Position.z})");

            return new Vector3
            {
                x = obj.ParentSurface.GlobalMapPosition.x,
                y = obj.ParentSurface.GlobalMapPosition.y,
                z = obj.ParentSurface.GlobalMapPosition.z
            };
        }

        public static Vector3 GetCenterWorldPosition(Vector3 globalMapPosition, int screenPixels, int tileSize, Vector3 position, Vector3 crashboxOffsets)
        {
            float tilesPerScreen = screenPixels / (float)tileSize;
            float halfTiles = tilesPerScreen / 2f;
 
            return new Vector3
            {
                x = globalMapPosition.x + halfTiles * tileSize - position.x - crashboxOffsets.x,
                y = globalMapPosition.y + crashboxOffsets.y,
                z = globalMapPosition.z + halfTiles * tileSize - position.z - crashboxOffsets.z
            };
        }

        public static void CenterCrashBoxAt(List<Vector3> crashBox, IVector3 targetPosition, IVector3 crashboxOffsets)
        {
            // Center crashbox so its BUNN (MinY) plasseres på toppen av bakken (targetPosition.y)
            // Legg til en ekstra lokal offset om nødvendig
            if (crashBox == null || crashBox.Count == 0 || targetPosition == null) return;

            //float localOffset = -72; // Brukes for å simulere mer realistisk høyde/synk

            float minY = crashBox.Min(p => p.y);
            float shiftY = targetPosition.y - minY + crashboxOffsets.y;

            Logger.Log($"[CenterAt] Target Y: {targetPosition.y}, CrashBox MinY: {minY}, CrashBoxOffsetY: {crashboxOffsets.y}, Final ShiftY: {shiftY}");

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

        public static void LogCrashboxContact(string label, _3dObject obj, List<Vector3> crashBox, _3dObject surface, List<Vector3> surfaceBox)
        {
            float minY = crashBox.Min(p => p.y);
            float surfaceMaxY = surfaceBox.Max(p => p.y);
            float deltaY = minY - surfaceMaxY;

            Logger.Log($"[ContactCheck] {label} '{obj.ObjectName}' MinY: {minY} vs Surface MaxY: {surfaceMaxY} => ΔY: {deltaY}");

            if (Math.Abs(deltaY) < 2.0f)
                Logger.Log($"[CONTACT] {label} '{obj.ObjectName}' appears to rest on the surface.");
            else
                Logger.Log($"[NO CONTACT] {label} '{obj.ObjectName}' is floating or below surface (ΔY = {deltaY})");

            LogCrashboxAnalysis($"{label} CrashBox", crashBox);
            LogCrashboxAnalysis("Surface CrashBox", surfaceBox);
        }

        public static void LogCrashboxAnalysis(string label, List<Vector3> box)
        {
            if (box == null || box.Count == 0) return;

            var yMin = box.Min(p => p.y);
            var yMax = box.Max(p => p.y);
            var xMin = box.Min(p => p.x);
            var xMax = box.Max(p => p.x);
            var zMin = box.Min(p => p.z);
            var zMax = box.Max(p => p.z);

            Logger.Log($"--- {label} ---");
            Logger.Log($"Y-range: [{yMin}–{yMax}], X-range: [{xMin}–{xMax}], Z-range: [{zMin}–{zMax}]");
            foreach (var p in box)
                Logger.Log($"(x={p.x}, y={p.y}, z={p.z})");
            Logger.Log($"--- End of {label} ---\n");
        }
    }
}
