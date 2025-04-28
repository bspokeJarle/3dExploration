using System;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using Domain;
using System.Collections.Generic;
using System.Diagnostics;

namespace _3dRotations.Helpers
{
    public static class SurfaceGeneration
    {
        // ======== CONFIGURABLE PARAMETERS ========
        const int zFactor = 6;
        static Random random = new Random();
        const float perlinScaleMin = 0.007f;
        const float perlinScaleMax = 0.013f;
        const double waterPatchProbability = 0.50;
        const float heightExponent = 1.7f;
        const int wrapSize = 18;
        const int landingAreaSize = 8;
        public static int maxTrees { get; set; }
        public static int maxHouses { get; set; }
        const int clusterSizeMin = 2;
        const int clusterSizeMax = 5;
        public static bool IncludeTestTreesInFrontOfPlatform = true;
        public static bool IncludeTestHousesInFrontOfPlatform = true;

        public static SurfaceData[,] ReturnPseudoRandomMap(int mapSize, out int maxHeight, int? maxTs, int? maxHs)
        {
            maxTrees = maxTs ?? 200;
            maxHouses = maxHs ?? 50;
            SurfaceData[,] surfaceValues = GeneratePerlinNoiseMap(mapSize, out maxHeight);
            surfaceValues = AddWaterPatches(surfaceValues, mapSize);
            surfaceValues = SmoothTerrain(surfaceValues, mapSize, maxHeight);
            EnsureFlatLandingArea(surfaceValues, mapSize, maxHeight);
            surfaceValues = ApplyEdgeWrapping(surfaceValues, mapSize);

            // Generate crashboxes after terrain is fully built
            GenerateCrashBoxes(surfaceValues, maxHeight);

            GenerateTerrainBitmapSource(surfaceValues, mapSize, maxHeight);
            return surfaceValues;
        }

        private static SurfaceData[,] GeneratePerlinNoiseMap(int mapSize, out int maxHeight)
        {
            SurfaceData[,] map = new SurfaceData[mapSize, mapSize];
            maxHeight = 0;
            float scale = perlinScaleMin + (float)(random.NextDouble() * (perlinScaleMax - perlinScaleMin));
            var mapId = 0;

            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    mapId++;
                    float perlinValue = (float)random.NextDouble(); // Placeholder for actual Perlin noise function
                    map[i, j] = new SurfaceData { mapDepth = (int)(Math.Pow(perlinValue, heightExponent) * 20 * zFactor), mapId = mapId };
                    if (map[i, j].mapDepth > maxHeight) maxHeight = map[i, j].mapDepth;
                }
            }

            // Add 3-4 larger mountain areas
            AddMountainRegions(map, mapSize, maxHeight);

            return map;
        }

        // **Forces 3-4 larger mountain regions**
        private static void AddMountainRegions(SurfaceData[,] map, int mapSize, int maxHeight)
        {
            int numMountains = 3 + random.Next(2); // Ensures 3-4 mountain regions

            for (int m = 0; m < numMountains; m++)
            {
                int centerX = random.Next(mapSize * 3 / 8, mapSize * 5 / 8);
                int centerY = random.Next(mapSize * 3 / 8, mapSize * 5 / 8);
                int radius = random.Next(10, 15);

                for (int i = -radius; i <= radius; i++)
                {
                    for (int j = -radius; j <= radius; j++)
                    {
                        int x = centerX + i;
                        int y = centerY + j;
                        if (x >= 0 && x < mapSize && y >= 0 && y < mapSize)
                        {
                            double distance = Math.Sqrt(i * i + j * j);
                            if (distance < radius)
                            {
                                int height = (int)(maxHeight * (0.8 + 0.2 * (radius - distance) / radius));
                                map[x, y].mapDepth = Math.Max(map[x, y].mapDepth, height);
                            }
                        }
                    }
                }
            }
        }

        private static SurfaceData[,] ApplyEdgeWrapping(SurfaceData[,] map, int mapSize)
        {
            SurfaceData[,] wrappedMap = new SurfaceData[mapSize + wrapSize * 2, mapSize + wrapSize * 2];

            // Copy original map
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    wrappedMap[i + wrapSize, j + wrapSize] = map[i, j];
                }
            }

            // Copy horizontal edges
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < wrapSize; j++)
                {
                    wrappedMap[i + wrapSize, j] = map[i, mapSize - wrapSize + j]; // Left wrap
                    wrappedMap[i + wrapSize, mapSize + wrapSize + j] = map[i, j]; // Right wrap
                }
            }

            // Copy vertical edges
            for (int j = 0; j < mapSize; j++)
            {
                for (int i = 0; i < wrapSize; i++)
                {
                    wrappedMap[i, j + wrapSize] = map[mapSize - wrapSize + i, j]; // Top wrap
                    wrappedMap[mapSize + wrapSize + i, j + wrapSize] = map[i, j]; // Bottom wrap
                }
            }

            // Copy corners
            for (int i = 0; i < wrapSize; i++)
            {
                for (int j = 0; j < wrapSize; j++)
                {
                    wrappedMap[i, j] = map[mapSize - wrapSize + i, mapSize - wrapSize + j]; // Top-left
                    wrappedMap[i, mapSize + wrapSize + j] = map[mapSize - wrapSize + i, j]; // Top-right
                    wrappedMap[mapSize + wrapSize + i, j] = map[i, mapSize - wrapSize + j]; // Bottom-left
                    wrappedMap[mapSize + wrapSize + i, mapSize + wrapSize + j] = map[i, j]; // Bottom-right
                }
            }

            return wrappedMap;
        }

        private static SurfaceData[,] AddWaterPatches(SurfaceData[,] map, int mapSize)
        {
            int waterThreshold = (int)(zFactor * 5.0);
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    if (map[i, j].mapDepth < waterThreshold || random.NextDouble() < waterPatchProbability)
                    {
                        map[i, j].mapDepth = 0;
                    }
                }
            }
            return map;
        }

        private static SurfaceData[,] SmoothTerrain(SurfaceData[,] map, int mapSize, int maxHeight)
        {
            SurfaceData[,] newMap = new SurfaceData[mapSize, mapSize];

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
                            sum += map[i + di, j + dj].mapDepth;
                            count++;
                        }
                    }

                    int avgHeight = sum / count;
                    newMap[i, j].mapDepth = avgHeight;

                    // Ensure larger flat areas
                    if (avgHeight < maxHeight * 0.4 && avgHeight > maxHeight * 0.15)
                    {
                        newMap[i, j].mapDepth = avgHeight - 2; // Flatten grasslands slightly
                    }

                    newMap[i, j].mapId = map[i, j].mapId;
                }
            }
            return newMap;
        }



        private static void EnsureFlatLandingArea(SurfaceData[,] map, int mapSize, int maxHeight)
        {
            int center = mapSize / 2;
            int startX = center - landingAreaSize / 2;
            int startY = center - landingAreaSize / 2;
            int landingHeight = (int)(maxHeight * 0.75); // High flat plateau

            for (int i = startX; i < startX + landingAreaSize; i++)
            {
                for (int j = startY; j < startY + landingAreaSize; j++)
                {
                    map[i, j].mapDepth = landingHeight;
                }
            }
        }

        public static BitmapSource GenerateTerrainBitmapSource(SurfaceData[,] terrainMap, int mapSize, int maxHeight)
        {
            WriteableBitmap bitmap = new WriteableBitmap(mapSize, mapSize, 96, 96, PixelFormats.Bgra32, null);
            int stride = mapSize * 4; // 4 bytes per pixel (BGRA32)
            byte[] pixelData = new byte[mapSize * mapSize * 4];

            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    int height = terrainMap[i, j].mapDepth;
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

        public static SurfaceData[,] Return2DViewPort(int viewPortSize, int GlobalX, int GlobalZ, SurfaceData[,] Global2DMap, int tileSize)
        {
            if (Global2DMap == null || Global2DMap.Length == 0) return new SurfaceData[0, 0];

            int mapSize = Global2DMap.GetLength(0);
            SurfaceData[,] viewPort = new SurfaceData[viewPortSize, viewPortSize];

            int MapZindex = GlobalZ / tileSize;
            int MapXindex = GlobalX / tileSize;

            for (int y = 0; y < viewPortSize; y++)
            {
                int globalY = (MapZindex + y) % mapSize; // Wrap around map edges
                for (int x = 0; x < viewPortSize; x++)
                {
                    int globalX = (MapXindex + x) % mapSize; // Wrap around map edges
                    viewPort[y, x] = Global2DMap[globalY, globalX];
                }
            }

            return viewPort;
        }

        public static List<(int x, int y, int height)> FindTreePlacementAreas(SurfaceData[,] map, int mapSize, int tileSize, int maxHeight)
        {
            List<(int x, int y, int height)> treeLocations = new List<(int x, int y, int height)>();
            HashSet<(int x, int y)> used = new HashSet<(int x, int y)>();

            int numberOfTrees = random.Next((int)(maxTrees * 0.95), maxTrees);
            var landingAreaCenter = GetLandingAreaCenter(map, mapSize, (int)(tileSize * 0.75));
            int landingX = landingAreaCenter.x;
            int landingY = landingAreaCenter.y;

            List<(int x, int y)> clusterCenters = new List<(int x, int y)>();
            int spacing = 40;

            for (int i = spacing / 2; i < mapSize; i += spacing)
            {
                for (int j = spacing / 2; j < mapSize; j += spacing)
                {
                    int height = map[i, j].mapDepth;
                    bool isFlat = Math.Abs(map[i, j].mapDepth - map[i - 1, j].mapDepth) < 10 &&
                                  Math.Abs(map[i, j].mapDepth - map[i + 1, j].mapDepth) < 10 &&
                                  Math.Abs(map[i, j].mapDepth - map[i, j - 1].mapDepth) < 10 &&
                                  Math.Abs(map[i, j].mapDepth - map[i, j + 1].mapDepth) < 10;
                    bool isAboveWater = height >= (int)(maxHeight * 0.15);
                    bool isSuitable = isAboveWater && height < (int)(maxHeight * 0.6);

                    if (isFlat && isSuitable)
                    {
                        clusterCenters.Add((i, j));
                    }
                }
            }

            if (IncludeTestTreesInFrontOfPlatform)
            {
                int fixedX = 1274;
                int fixedY = 1241;
                for (int t = 0; t < 5; t++)
                {
                    int offsetX = random.Next(-2, 3);
                    int offsetY = random.Next(-2, 3);
                    int px = Math.Clamp(fixedX + offsetX, 0, mapSize - 1);
                    int py = Math.Clamp(fixedY + offsetY, 0, mapSize - 1);
                    int height = map[px, py].mapDepth;
                    if (height >= (int)(maxHeight * 0.15) && height < (int)(maxHeight * 0.6))
                    {
                        treeLocations.Add((px, py, height));
                        used.Add((px, py));
                    }
                }

                clusterCenters.Insert(0, (landingX + random.Next(-20, 21), landingY + random.Next(10, 30)));
            }

            int desiredClusters = numberOfTrees / clusterSizeMax;
            int clusterIndex = 0;

            while (treeLocations.Count < numberOfTrees && clusterIndex < clusterCenters.Count)
            {
                var (cx, cy) = clusterCenters[clusterIndex++];
                int clusterSize = random.Next(clusterSizeMin, clusterSizeMax + 1);

                for (int k = 0; k < clusterSize && treeLocations.Count < numberOfTrees; k++)
                {
                    int offsetX = random.Next(-10, 11);
                    int offsetY = random.Next(-10, 11);
                    int nx = cx + offsetX;
                    int ny = cy + offsetY;

                    if (nx >= 10 && nx < mapSize - 10 && ny >= 10 && ny < mapSize - 10 && !used.Contains((nx, ny)))
                    {
                        int height = map[nx, ny].mapDepth;
                        bool isAboveWater = height >= (int)(maxHeight * 0.18);
                        if (isAboveWater && height < (int)(maxHeight * 0.6) &&
                            Math.Abs(height - map[nx - 1, ny].mapDepth) < 12 &&
                            Math.Abs(height - map[nx + 1, ny].mapDepth) < 12 &&
                            Math.Abs(height - map[nx, ny - 1].mapDepth) < 12 &&
                            Math.Abs(height - map[nx, ny + 1].mapDepth) < 12)
                        {
                            treeLocations.Add((nx, ny, height));
                            used.Add((nx, ny));
                        }
                    }
                }
            }

            return treeLocations;
        }

        public static List<(int x, int y, int height)> FindHousePlacementAreas(SurfaceData[,] map, int mapSize, int maxHeight, List<(int x, int y, int height)> existingTrees)
        {
            List<(int x, int y, int height)> houseLocations = new List<(int x, int y, int height)>();
            int numberOfHouses = random.Next((int)(maxHouses * 0.9), maxHouses);
            HashSet<(int x, int y)> reserved = new HashSet<(int x, int y)>();
            foreach (var (x, y, _) in existingTrees)
                reserved.Add((x, y));

            int spacing = 40;
            for (int i = spacing / 2; i < mapSize; i += spacing)
            {
                for (int j = spacing / 2; j < mapSize; j += spacing)
                {
                    if (reserved.Contains((i, j))) continue;

                    int height = map[i, j].mapDepth;
                    bool isFlat = Math.Abs(height - map[i - 1, j].mapDepth) < 5 &&
                                  Math.Abs(height - map[i + 1, j].mapDepth) < 5 &&
                                  Math.Abs(height - map[i, j - 1].mapDepth) < 5 &&
                                  Math.Abs(height - map[i, j + 1].mapDepth) < 5;
                    bool isAboveWater = height >= (int)(maxHeight * 0.19);
                    bool isSuitable = isAboveWater && height < (int)(maxHeight * 0.7);

                    if (isFlat && isSuitable && houseLocations.Count < numberOfHouses)
                    {
                        houseLocations.Add((i, j, height));
                        reserved.Add((i, j));
                    }
                }
            }

            if (IncludeTestHousesInFrontOfPlatform)
            {
                int fixedX = 1274;
                int fixedY = 1241;
                for (int h = 0; h < 3; h++)
                {
                    int offsetX = random.Next(-2, 3);
                    int offsetY = random.Next(-2, 3);
                    int px = Math.Clamp(fixedX + offsetX, 0, mapSize - 1);
                    int py = Math.Clamp(fixedY + offsetY, 0, mapSize - 1);
                    if (!reserved.Contains((px, py)))
                    {
                        int height = map[px, py].mapDepth;
                        if (height >= (int)(maxHeight * 0.15) && height < (int)(maxHeight * 0.7))
                        {
                            houseLocations.Add((px, py, height));
                            reserved.Add((px, py));
                        }
                    }
                }
            }

            return houseLocations;
        }

        // (Other unchanged methods omitted for brevity...)
    
        public static (int x, int y) GetLandingAreaCenter(SurfaceData[,] map, int mapSize, int landingHeight)
        {
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    if (map[i, j].mapDepth == landingHeight)
                    {
                        return (i + landingAreaSize / 2, j + landingAreaSize / 2);
                    }
                }
            }
            return (mapSize / 2, mapSize / 2); // Default to center if not found
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

        private static void GenerateCrashBoxes(SurfaceData[,] map, int maxHeight)
        {
            int mapSizeX = map.GetLength(0);
            int mapSizeY = map.GetLength(1);
            bool[,] visited = new bool[mapSizeX, mapSizeY];
            int numCrashBoxes = 0;
            const int MinimumCrashBoxArea = 9; // Example: minimum 9 tiles (3x3 area)

            for (int i = 0; i < mapSizeX; i++)
            {
                for (int j = 0; j < mapSizeY; j++)
                {
                    if (visited[i, j])
                        continue;

                    int height = map[i, j].mapDepth;

                    if (IsHighlandOrMountain(height, maxHeight))
                    {
                        int width = 1;
                        int heightBox = 1;

                        // Expand horizontally
                        while (j + width < mapSizeY && !visited[i, j + width] && IsHighlandOrMountain(map[i, j + width].mapDepth, maxHeight))
                        {
                            width++;
                        }

                        // Expand vertically
                        bool canExpandDown = true;
                        while (i + heightBox < mapSizeX && canExpandDown)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                if (visited[i + heightBox, j + x] || !IsHighlandOrMountain(map[i + heightBox, j + x].mapDepth, maxHeight))
                                {
                                    canExpandDown = false;
                                    break;
                                }
                            }
                            if (canExpandDown) heightBox++;
                        }

                        int area = width * heightBox;

                        if (area >= MinimumCrashBoxArea)
                        {
                            map[i, j].crashBox = new SurfaceData.CrashBoxData
                            {
                                width = width,
                                height = heightBox
                            };
                            numCrashBoxes++;
                        }

                        // Always mark all covered tiles as visited
                        for (int di = 0; di < heightBox; di++)
                        {
                            for (int dj = 0; dj < width; dj++)
                            {
                                visited[i + di, j + dj] = true;
                            }
                        }
                    }
                }
            }

            Debug.WriteLine($"[SurfaceGeneration] Generated {numCrashBoxes} crashboxes.");
        }

        private static bool IsHighlandOrMountain(int height, int maxHeight)
        {
            // Highland starts at 40% of maximum elevation
            return height >= maxHeight * 0.4;
        }

    }
}
