using CommonUtilities._3DHelpers;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using Domain;
using System;
using System.Collections.Generic;
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
        public static void  UpdateMapOverlay(System.Windows.Controls.Image mapOverlay, BitmapSource surfaceMapBitmap, int mapX, int mapY)
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

        // -----------------------------------------------------------------
        //  MINIMAP MARKERS
        // -----------------------------------------------------------------

        // Stores original pixels so they can be restored each frame
        private static byte[]? _savedMarkerPixels;
        private static List<(int x, int z)>? _savedMarkerPositions;
        private static int _markerFrame;

        /// <summary>
        /// Restores the original terrain pixels that were overwritten by markers in the previous frame.
        /// Must be called BEFORE UpdateDirtyTilesInMap so infected pixels are not lost.
        /// </summary>
        public static void RestoreMinimapMarkers(BitmapSource surfaceMapBitmap)
        {
            if (surfaceMapBitmap is not WriteableBitmap wb)
                return;

            try
            {
                if (_savedMarkerPixels != null && _savedMarkerPositions != null)
                {
                    int w = wb.PixelWidth;
                    int h = wb.PixelHeight;
                    int pixelIndex = 0;
                    foreach (var (px, pz) in _savedMarkerPositions)
                    {
                        if (px >= 0 && pz >= 0 && px < w && pz < h)
                        {
                            wb.WritePixels(new Int32Rect(px, pz, 1, 1),
                                _savedMarkerPixels, 4, pixelIndex * 4);
                        }
                        pixelIndex++;
                    }
                    _savedMarkerPixels = null;
                    _savedMarkerPositions = null;
                }
            }
            catch (Exception ex)
            {
                if (enableLogging) Logger.Log("RestoreMinimapMarkers: " + ex.Message, "Error");
            }
        }

        /// <summary>
        /// Draws object markers on the minimap bitmap.
        /// Ship = grey (always visible), others blink.
        /// Seeder = magenta, Drone = blue, Decoy = orange.
        /// Must be called AFTER UpdateDirtyTilesInMap so saved pixels include infected tiles.
        /// </summary>
        public static void DrawMinimapMarkers(BitmapSource surfaceMapBitmap)
        {
            if (surfaceMapBitmap is not WriteableBitmap wb)
                return;

            int w = wb.PixelWidth;
            int h = wb.PixelHeight;
            int tileSize = MapSetup.tileSize;

            try
            {
                // Blink toggle for AI objects (ship is always visible)
                _markerFrame++;
                bool aiVisible = (_markerFrame % 30) < 15;

                // BGRA format: [B, G, R, A]
                byte[] greyPx    = { 180, 180, 180, 255 }; // Ship (always on)
                byte[] darkRedPx = { 0, 0, 160, 255 };     // Seeder (darker red than infection's 255)
                byte[] bluePx    = { 255, 80, 0, 255 };    // Drone
                byte[] orangePx  = { 0, 140, 255, 255 };   // Decoy

                var positions = new List<(int x, int z)>();
                var colors = new List<byte[]>();

                // Ship marker — always visible, center of the surface viewport
                var mapPos = GameState.SurfaceState.GlobalMapPosition;
                if (mapPos != null)
                {
                    int viewportCenterOffset = (SurfaceSetup.viewPortSize * tileSize) / 2;
                    int shipBx = (int)((mapPos.x + viewportCenterOffset) / tileSize);
                    int shipBz = (int)((mapPos.z + viewportCenterOffset) / tileSize);
                    AddMarkerPixels(positions, colors, shipBx, shipBz, greyPx, w, h);
                }

                // AI objects — only drawn during blink-on phase
                if (aiVisible)
                {
                    var aiObjects = GameState.SurfaceState?.AiObjects;
                    if (aiObjects != null)
                    {
                        for (int i = 0; i < aiObjects.Count; i++)
                        {
                            var obj = aiObjects[i];
                            // Only filter on HasExploded (fully dead).
                            // HasCrashed just means "took a hit" and doesn't mean destroyed.
                            if (obj.ImpactStatus?.HasExploded == true)
                                continue;
                            if (obj.WorldPosition == null)
                                continue;
                            // Skip objects at origin (on-screen objects without world position)
                            if (obj.WorldPosition.x == 0 && obj.WorldPosition.z == 0)
                                continue;

                            byte[]? color = obj.ObjectName switch
                            {
                                "Seeder" => darkRedPx,
                                "KamikazeDrone" => bluePx,
                                "DroneDecoy" => orangePx,
                                _ => null
                            };
                            if (color == null) continue;

                            int bx = (int)(obj.WorldPosition.x / tileSize);
                            int bz = (int)(obj.WorldPosition.z / tileSize);
                            AddMarkerPixels(positions, colors, bx, bz, color, w, h);
                        }
                    }
                }

                // Save original pixels before overwriting
                _savedMarkerPositions = positions;
                _savedMarkerPixels = new byte[positions.Count * 4];
                for (int i = 0; i < positions.Count; i++)
                {
                    var (px, pz) = positions[i];
                    if (px >= 0 && pz >= 0 && px < w && pz < h)
                    {
                        wb.CopyPixels(new Int32Rect(px, pz, 1, 1), _savedMarkerPixels, 4, i * 4);
                    }
                }

                // Draw markers
                for (int i = 0; i < positions.Count; i++)
                {
                    var (px, pz) = positions[i];
                    if (px >= 0 && pz >= 0 && px < w && pz < h)
                    {
                        wb.WritePixels(new Int32Rect(px, pz, 1, 1), colors[i], 4, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                if (enableLogging) Logger.Log("DrawMinimapMarkers: " + ex.Message, "Error");
            }
        }

        /// <summary>
        /// Adds a 3x3 block of pixels for a marker at the given bitmap center.
        /// </summary>
        private static void AddMarkerPixels(List<(int x, int z)> positions, List<byte[]> colors,
            int cx, int cz, byte[] color, int w, int h)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int px = cx + dx;
                    int pz = cz + dz;
                    if (px >= 0 && pz >= 0 && px < w && pz < h)
                    {
                        positions.Add((px, pz));
                        colors.Add(color);
                    }
                }
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
