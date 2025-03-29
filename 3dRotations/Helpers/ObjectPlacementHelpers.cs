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

                    _3dObjectHelpers.CenterObjectAt(obj, triangle.vert1);
                    x = screenCenterX + obj.Position.x;
                    y = screenCenterY + obj.Position.y;
                    z = obj.Position.z;
                }
                else
                {
                    x = screenCenterX + obj.Position.x;
                    y = screenCenterY + obj.Position.y;
                    z = obj.Position.z;
                }
            }
            else
            {
                x = screenCenterX + localWorldPosition.x + obj.Position.x;
                y = screenCenterY + localWorldPosition.y + obj.Position.y;
                z = localWorldPosition.z + obj.Position.z;
            }

            return true;
        }

        public static bool TryGetCrashboxWorldPosition(_3dObject obj, out Vector3 position)
        {
            position = new Vector3();
            if (obj == null) return false;

            if (obj.SurfaceBasedId > 0)
            {
                var worldPos = GetWorldPosition(obj);
                position = new Vector3
                {
                    x = worldPos.x,
                    y = worldPos.y,
                    z = worldPos.z
                };
                return true;
            }

            position = GetGlobalMapOffset(obj);
            return true;
        }

        public static Vector3 GetWorldPosition(_3dObject obj)
        {
            if (obj == null) return new Vector3();

            if (obj.SurfaceBasedId > 0)
            {
                var triangle = GetSurfaceTriangle(obj);
                if (triangle != null)
                {
                    return new Vector3
                    {
                        x = obj.ParentSurface.GlobalMapPosition.x + obj.Position.x,
                        y = triangle.vert1.y + obj.Position.y,
                        z = obj.ParentSurface.GlobalMapPosition.z + obj.Position.z
                    };
                }
            }

            if (obj.ObjectName == "Ship")
            {
                return _3dObjectHelpers.GetCenterWorldPosition(
                    obj.ParentSurface.GlobalMapPosition,
                    obj.ParentSurface.GlobalMapPosition,
                    obj.ParentSurface.SurfaceWidth(),
                    obj.ParentSurface.TileSize(),
                    (Vector3)obj.Position);
            }

            return new Vector3
            {
                x = obj.ParentSurface.GlobalMapPosition.x + obj.Position.x,
                y = obj.ParentSurface.GlobalMapPosition.y + obj.Position.y,
                z = obj.ParentSurface.GlobalMapPosition.z + obj.Position.z
            };
        }

        public static void CenterCrashBoxAt(List<Vector3> crashBox, IVector3 targetPosition)
        {
            if (crashBox == null || crashBox.Count == 0 || targetPosition == null) return;

            float minY = crashBox.Min(p => p.y);
            float shiftY = targetPosition.y - minY;

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

            Logger.Log($"[ContactCheck] {label} '{obj.ObjectName}' ΔY: {deltaY}");

            if (Math.Abs(deltaY) < 2.0f)
                Logger.Log($"[CONTACT] {label} '{obj.ObjectName}' appears to rest on the surface.");
        }
    }
}
