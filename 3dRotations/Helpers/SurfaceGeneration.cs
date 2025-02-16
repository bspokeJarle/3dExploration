using System;
//using System.Drawing;
//using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace _3dRotations.Helpers
{
    public static class SurfaceGeneration
    {
        // ======== CONFIGURABLE PARAMETERS ========
        const int zFactor = 6; // Scaling factor for terrain height
        static Random random = new Random();
        const float perlinScaleMin = 0.008f; // Lower frequency for smoother, more swoopy hills
        const float perlinScaleMax = 0.012f; // Adjusted for larger terrain variations
        const double waterPatchProbability = 0.02; // Lower probability for fewer, larger lakes
        const double plateauProbability = 0.04; // Lower probability for larger, more defined plateaus
        const float heightExponent = 1.4f; // More exaggerated elevation for natural swoops
        const int plateauHeight = 10; // Higher plateau levels for distinct flat areas

        public static int[,] ReturnPseudoRandomMap(int mapSize, out int maxHeight)
        {
            int[,] surfaceValues = GeneratePerlinNoiseMap(mapSize, out maxHeight);
            surfaceValues = AddWaterPatches(surfaceValues, mapSize);
            surfaceValues = SmoothTerrain(surfaceValues, mapSize);
            GenerateTerrainBitmapSource(surfaceValues, mapSize, maxHeight);
            return surfaceValues;
        }

        private static int[,] GeneratePerlinNoiseMap(int mapSize, out int maxHeight)
        {
            int[,] map = new int[mapSize, mapSize];
            maxHeight = 0;
            float scale = perlinScaleMin + (float)(random.NextDouble() * (perlinScaleMax - perlinScaleMin));

            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    float perlinValue = (float)random.NextDouble(); // Placeholder for actual Perlin noise function
                    map[i, j] = (int)(Math.Pow(perlinValue, heightExponent) * 20 * zFactor);
                    if (map[i, j] > maxHeight) maxHeight = map[i, j];
                }
            }
            return map;
        }

         private static int[,] AddWaterPatches(int[,] map, int mapSize)
        {
            int waterThreshold = (int)(zFactor * 5.0);
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    if (map[i, j] < waterThreshold || random.NextDouble() < waterPatchProbability)
                    {
                        map[i, j] = 0;
                    }
                }
            }
            return map;
        }

        private static int[,] SmoothTerrain(int[,] map, int mapSize)
        {
            int[,] newMap = new int[mapSize, mapSize];
            for (int i = 1; i < mapSize - 1; i++)
            {
                for (int j = 1; j < mapSize - 1; j++)
                {
                    int sum = 0;
                    int count = 0;
                    for (int di = -1; di <= 1; di++)
                    {
                        for (int dj = -1; dj <= 1; dj++)
                        {
                            sum += map[i + di, j + dj];
                            count++;
                        }
                    }
                    newMap[i, j] = (sum / count > zFactor * 6.0) ? (int)(zFactor * 5.0 + sum / count * 0.4) : sum / count;
                    if (random.NextDouble() < plateauProbability)
                    {
                        newMap[i, j] = (int)(zFactor * plateauHeight);
                    }
                }
            }
            return newMap;
        }

        public static BitmapSource GenerateTerrainBitmapSource(int[,] terrainMap, int mapSize, int maxHeight)
        {
            WriteableBitmap bitmap = new WriteableBitmap(mapSize, mapSize, 96, 96, PixelFormats.Bgra32, null);
            int stride = mapSize * 4; // 4 bytes per pixel (BGRA32)
            byte[] pixelData = new byte[mapSize * mapSize * 4];

            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    int height = terrainMap[i, j];
                    Color color = GetTileColor(height, maxHeight);

                    int index = (j * mapSize + i) * 4;
                    pixelData[index] = color.B; // Blue
                    pixelData[index + 1] = color.G; // Green
                    pixelData[index + 2] = color.R; // Red
                    pixelData[index + 3] = 255; // Alpha
                }
            }

            bitmap.WritePixels(new Int32Rect(0, 0, mapSize, mapSize), pixelData, stride, 0);
            return bitmap;
        }

        public static int[,] Return2DViewPort(int viewPortSize, int GlobalX, int GlobalZ, int[,] Global2DMap, int tileSize)
        {
            if (Global2DMap == null || Global2DMap.Length == 0) return new int[0, 0];

            int mapSize = Global2DMap.GetLength(0);
            int[,] viewPort = new int[viewPortSize + 3, viewPortSize];

            int MapZindex = GlobalZ / tileSize;
            int MapXindex = GlobalX / tileSize;

            for (int y = 0; y < viewPortSize + 3; y++)
            {
                int globalY = MapZindex + y;
                if (globalY >= mapSize) break;

                for (int x = 0; x < viewPortSize; x++)
                {
                    int globalX = MapXindex + x;
                    if (globalX >= mapSize) break;

                    viewPort[y, x] = Global2DMap[globalY, globalX];
                }
            }
            return viewPort;
        }

        private static Color GetTileColor(int height, int maxHeight)
        {
            int red, green, blue;

            if (height < maxHeight * 0.05) // Deep Ocean (Very Dark Blue)
            {
                red = 0;
                green = 0;
                blue = 180 + (int)((height / (maxHeight * 0.05)) * 75); // Darker blue in deeper water
            }
            else if (height < maxHeight * 0.15) // Coastal Water (Medium Blue)
            {
                red = 0;
                green = (int)((height / (maxHeight * 0.2)) * 100);
                blue = 255;
            }
            else if (height < maxHeight * 0.4) // Grassland (Green Gradient)
            {
                red = 0;
                green = 150 + ((height - (int)(maxHeight * 0.2)) * 3);
                blue = 0;
            }
            else if (height < maxHeight * 0.7) // Highlands (Brown Gradient)
            {
                red = 139 + ((height - (int)(maxHeight * 0.4)) * 3);
                green = 69 + ((height - (int)(maxHeight * 0.4)) * 2);
                blue = 19;
            }
            else // Mountains (Gray Gradient)
            {
                red = 120 + ((height - (int)(maxHeight * 0.7)) * 3);
                green = 120 + ((height - (int)(maxHeight * 0.7)) * 3);
                blue = 120 + ((height - (int)(maxHeight * 0.7)) * 3);
            }

            return Color.FromArgb(255,
                (byte)Math.Clamp(red, 0, 255),
                (byte)Math.Clamp(green, 0, 255),
                (byte)Math.Clamp(blue, 0, 255)
            );
        }
    }
}
