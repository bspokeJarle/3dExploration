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

                // Thread-safe snapshot + clear to prevent corruption from background thread
                List<IVector3> dirtySnapshot;
                lock (state.DirtyTiles)
                {
                    dirtySnapshot = new List<IVector3>(state.DirtyTiles);
                    state.DirtyTiles.Clear();
                }

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
        //  MINIMAP MARKERS  (overlay approach – never touches source bitmap)
        // -----------------------------------------------------------------

        private static int _markerFrame;

        /// <summary>
        /// Creates a cropped copy of the source bitmap, draws markers on the copy,
        /// and sets it as the minimap image source. The source bitmap is never modified
        /// by markers, eliminating all save/restore issues.
        /// </summary>
        public static void UpdateMapOverlayWithMarkers(
            System.Windows.Controls.Image mapOverlay,
            BitmapSource surfaceMapBitmap,
            int mapX, int mapZ)
        {
            if (mapX == 0 || mapZ == 0) return;
            if (surfaceMapBitmap == null || mapOverlay == null) return;

            try
            {
                // 1. Compute crop rect (same as old UpdateMapOverlay)
                int cropX = (mapX - MapSetup.bitmapMapCenterOffsetX) / MapSetup.tileSize;
                int cropZ = (mapZ - MapSetup.bitmapMapCenterOffsetY) / MapSetup.tileSize;
                int cropW = MapSetup.bitmapSize * 2;
                int cropH = MapSetup.bitmapSize;

                int srcW = surfaceMapBitmap.PixelWidth;
                int srcH = surfaceMapBitmap.PixelHeight;

                // Clamp crop to source bounds
                if (cropX < 0) cropX = 0;
                if (cropZ < 0) cropZ = 0;
                if (cropX + cropW > srcW) cropW = srcW - cropX;
                if (cropZ + cropH > srcH) cropH = srcH - cropZ;
                if (cropW <= 0 || cropH <= 0) return;

                // 2. Copy crop pixels into a writable bitmap
                int stride = cropW * 4; // Bgra32 = 4 bytes per pixel
                byte[] pixels = new byte[stride * cropH];
                surfaceMapBitmap.CopyPixels(
                    new Int32Rect(cropX, cropZ, cropW, cropH),
                    pixels, stride, 0);

                // 3. Draw markers onto the pixel buffer (positions relative to crop)
                DrawMarkersOnBuffer(pixels, cropW, cropH, stride, cropX, cropZ);

                // 4. Write into a WriteableBitmap and set as source
                var wb = new WriteableBitmap(cropW, cropH, 96, 96,
                    System.Windows.Media.PixelFormats.Bgra32, null);
                wb.WritePixels(new Int32Rect(0, 0, cropW, cropH), pixels, stride, 0);
                wb.Freeze(); // Allow cross-thread use and better perf
                mapOverlay.Source = wb;
            }
            catch (Exception ex)
            {
                if (enableLogging) Logger.Log("UpdateMapOverlayWithMarkers: " + ex.Message, "Error");
            }
        }

        /// <summary>
        /// Draws marker pixels into a raw BGRA pixel buffer.
        /// Positions are converted from full-bitmap coords to crop-relative coords.
        /// </summary>
        private static void DrawMarkersOnBuffer(
            byte[] pixels, int cropW, int cropH, int stride,
            int cropOriginX, int cropOriginZ)
        {
            int tileSize = MapSetup.tileSize;

            _markerFrame++;
            bool aiVisible = (_markerFrame % 30) < 15;

            // BGRA format
            byte[] greyPx    = { 180, 180, 180, 255 }; // Ship
            byte[] blackPx   = { 0, 0, 0, 255 };       // Seeder
            byte[] bluePx    = { 255, 80, 0, 255 };    // Drone
            byte[] orangePx  = { 0, 140, 255, 255 };   // Decoy
            byte[] powerupPx = { 255, 140, 30, 255 };  // PowerUp (strong blue)

            bool powerupVisible = (_markerFrame % 8) < 5;

            // Ship marker — always visible at viewport center
            var mapPos = GameState.SurfaceState.GlobalMapPosition;
            if (mapPos != null)
            {
                int viewportCenterOffset = (SurfaceSetup.viewPortSize * tileSize) / 2;
                int shipBx = (int)((mapPos.x + viewportCenterOffset) / tileSize) - cropOriginX;
                int shipBz = (int)((mapPos.z + viewportCenterOffset) / tileSize) - cropOriginZ;
                StampMarker(pixels, cropW, cropH, stride, shipBx, shipBz, greyPx);
            }

            // AI objects — only during blink-on phase
            if (aiVisible)
            {
                var aiObjects = GameState.SurfaceState?.AiObjects;
                _3dObject[]? snapshot = null;
                if (aiObjects != null)
                {
                    lock (aiObjects) { snapshot = [.. aiObjects]; }
                }
                if (snapshot != null)
                {
                    for (int i = 0; i < snapshot.Length; i++)
                    {
                        var obj = snapshot[i];
                        if (obj == null) continue;
                        if (!obj.IsActive) continue;
                        if (obj.ImpactStatus?.HasExploded == true)
                            continue;
                        // Also skip objects whose parts have been cleared (fully dead)
                        if (obj.ObjectParts == null || obj.ObjectParts.Count == 0)
                            continue;
                        if (obj.WorldPosition == null)
                            continue;
                        if (obj.WorldPosition.x == 0 && obj.WorldPosition.z == 0)
                            continue;

                        bool isPowerUp = obj.ObjectName == "PowerUp";

                        if (isPowerUp)
                        {
                            if (!powerupVisible) continue;
                            int bx = (int)(obj.WorldPosition.x / tileSize) - cropOriginX;
                            int bz = (int)(obj.WorldPosition.z / tileSize) - cropOriginZ;
                            StampMarkerLarge(pixels, cropW, cropH, stride, bx, bz, powerupPx);
                            continue;
                        }

                        byte[]? color = obj.ObjectName switch
                        {
                            "Seeder" => blackPx,
                            "KamikazeDrone" => bluePx,
                            "DroneDecoy" => orangePx,
                            "MotherShipSmall" => new byte[] { 0, 0, 255, 255 },
                            _ => null
                        };
                        if (color == null) continue;

                        int bx2 = (int)(obj.WorldPosition.x / tileSize) - cropOriginX;
                        int bz2 = (int)(obj.WorldPosition.z / tileSize) - cropOriginZ;
                        StampMarker(pixels, cropW, cropH, stride, bx2, bz2, color);
                    }
                }
            }
        }

        /// <summary>
        /// Stamps a 3x3 marker into a raw pixel buffer at the given crop-relative position.
        /// </summary>
        private static void StampMarker(byte[] pixels, int w, int h, int stride,
            int cx, int cz, byte[] bgra)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int px = cx + dx;
                    int pz = cz + dz;
                    if (px >= 0 && pz >= 0 && px < w && pz < h)
                    {
                        int offset = pz * stride + px * 4;
                        pixels[offset]     = bgra[0]; // B
                        pixels[offset + 1] = bgra[1]; // G
                        pixels[offset + 2] = bgra[2]; // R
                        pixels[offset + 3] = bgra[3]; // A
                    }
                }
            }
        }

        private static void StampMarkerLarge(byte[] pixels, int w, int h, int stride,
            int cx, int cz, byte[] bgra)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dz = -2; dz <= 2; dz++)
                {
                    int px = cx + dx;
                    int pz = cz + dz;
                    if (px >= 0 && pz >= 0 && px < w && pz < h)
                    {
                        int offset = pz * stride + px * 4;
                        pixels[offset]     = bgra[0]; // B
                        pixels[offset + 1] = bgra[1]; // G
                        pixels[offset + 2] = bgra[2]; // R
                        pixels[offset + 3] = bgra[3]; // A
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
