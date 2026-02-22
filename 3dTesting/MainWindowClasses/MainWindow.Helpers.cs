using CommonUtilities._3DHelpers;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using Domain;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.Helpers
{
    public static class GameHelpers
    {
        private static _3dRotationCommon Rotate3d = new _3dRotationCommon();
        private static bool enableLogging = false;
        /// <summary>
        /// Applies the rotation offset to prevent flipping when rotating.
        /// </summary>
        public static void ApplyRotationOffset(ref ITriangleMeshWithColor triangle, int? offsetX, int? offsetY, int? offsetZ)
        {
            if (offsetX > 0) { triangle.vert1.x += (float)offsetX; triangle.vert2.x += (float)offsetX; triangle.vert3.x += (float)offsetX; }
            if (offsetY > 0) { triangle.vert1.y += (float)offsetY; triangle.vert2.y += (float)offsetY; triangle.vert3.y += (float)offsetY; }
            if (offsetZ > 0) { triangle.vert1.z += (float)offsetZ; triangle.vert2.z += (float)offsetZ; triangle.vert3.z += (float)offsetZ; }
        }

        public static Vector3 ComputeCentroid(_3dObject inhabitant)
        {
            if (inhabitant.ObjectParts == null || inhabitant.ObjectParts.Count == 0)
                return new Vector3(0, 0, 0);

            float sumX = 0, sumY = 0, sumZ = 0;
            int totalVertices = 0;

            foreach (var part in inhabitant.ObjectParts)
            {
                foreach (var triangle in part.Triangles)
                {
                    sumX += triangle.vert1.x + triangle.vert2.x + triangle.vert3.x;
                    sumY += triangle.vert1.y + triangle.vert2.y + triangle.vert3.y;
                    sumZ += triangle.vert1.z + triangle.vert2.z + triangle.vert3.z;
                    totalVertices += 3;
                }
            }

            if (totalVertices == 0) return new Vector3(0, 0, 0);

            return new Vector3(sumX / totalVertices, sumY / totalVertices, sumZ / totalVertices);
        }

        /// <summary>
        /// Updates the minimap overlay with the correct cropped portion.
        /// </summary>
        public static void UpdateMapOverlay(System.Windows.Controls.Image mapOverlay, BitmapSource surfaceMapBitmap, int mapX, int mapY)
        {
            if (mapX == 0 || mapY == 0) return; // Avoid division by zero or invalid map coordinates 
            if (surfaceMapBitmap != null && mapOverlay != null)
            {
                try
                {
                    mapOverlay.Source = new CroppedBitmap(surfaceMapBitmap, new Int32Rect((mapX - MapSetup.bitmapMapCenterOffsetX) / MapSetup.tileSize, (mapY - MapSetup.bitmapMapCenterOffsetY) / MapSetup.tileSize, MapSetup.bitmapSize * 2, MapSetup.bitmapSize));
                }
                catch (Exception ex)
                {
                    if (enableLogging) Logger.Log("UpdateMapOverlay: Exception while updating map overlay " + ex.Message, "Error");
                }
            }
        }

        public static void UpdateDirtyTilesInMap(BitmapSource surfaceMapBitmap)
        {
            var state = GameState.SurfaceState;
            if (state == null) return;

            // Scene reset: bitmap can be null or not ready
            if (surfaceMapBitmap is not WriteableBitmap wb)
            {
                state.DirtyTiles?.Clear();
                return;
            }

            if (state.DirtyTiles == null || state.DirtyTiles.Count == 0)
                return;

            try
            {
                int w = wb.PixelWidth;
                int h = wb.PixelHeight;

                // BGRA (pure red)
                byte[] infectedPx = { 0, 0, 255, 255 };

                // Snapshot + clear to avoid modifying collection while iterating,
                // and to avoid repeated work if something adds more while we write.
                var dirtySnapshot = state.DirtyTiles.ToList();
                state.DirtyTiles.Clear();

                foreach (var dirty in dirtySnapshot)
                {
                    int x = (int)dirty.x;
                    int z = (int)dirty.z;

                    if (x < 0 || z < 0 || x >= w || z >= h)
                    {
                        if (enableLogging) Logger.Log($"UpdateDirtyTilesInMap: skip OOB ({x},{z})");
                        continue;
                    }

                    if (enableLogging) Logger.Log($"UpdateDirtyTilesInMap: Infect pixel ({x},{z})");
                    wb.WritePixels(new Int32Rect(x, z, 1, 1), infectedPx, 4, 0);
                }
            }
            catch (Exception ex)
            {
                if (enableLogging) Logger.Log("UpdateDirtyTilesInMap: Exception while updating dirty tiles  " + ex.Message, "Error");
                return;
            }
        }

        public static void UpdateShipStatistics(System.Windows.Shapes.Rectangle healthRectangle, _3dObject ship)
        {
            if (ship == null || ship.ImpactStatus == null) return;
            if (ship.ImpactStatus.ObjectHealth > 0)
            {
                healthRectangle.Width = ship.ImpactStatus.ObjectHealth.Value * 2;
            }
            else healthRectangle.Width = 1;
        }
    }
}
