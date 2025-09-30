using Domain;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

        public enum TerrainType
        {
            DeepWater,
            Coast,
            Grassland,
            Highlands,
            Mountains,
            Unknown
        }

        public static SurfaceData[,] ReturnPseudoRandomMap(int mapSize, out int maxHeight, int? maxTs, int? maxHs)
        {
            // Set configurable caps (keeps your previous behavior)
            maxTrees = maxTs ?? 200;
            maxHouses = maxHs ?? 50;

            // 1) Base terrain (Perlin placeholder) + forced mountain regions
            SurfaceData[,] surfaceValues = GeneratePerlinNoiseMap(mapSize, out maxHeight);

            // 2) Water patches (depth=0) – this can change effective max height indirectly later
            surfaceValues = AddWaterPatches(surfaceValues, mapSize);

            // 3) Smoothing (and slight flattening of mid-range) – uses current maxHeight thresholds
            surfaceValues = SmoothTerrain(surfaceValues, mapSize, maxHeight);

            // 4) Flat landing area plateau (uses current maxHeight)
            EnsureFlatLandingArea(surfaceValues, mapSize, maxHeight);

            // 5) Edge wrapping (expands the map)
            surfaceValues = ApplyEdgeWrapping(surfaceValues, mapSize);

            // 🔑 6) Now that the map is fully built/wrapped, recompute the *actual* max
            maxHeight = GetActualMaxHeight(surfaceValues);

            // 7) Crash boxes should use the same (final) classification thresholds
            GenerateCrashBoxes(surfaceValues, maxHeight);

            // 8) Terrain bitmap should also use the same (final) maxHeight + final size
            int wrappedSize = surfaceValues.GetLength(0);
            GenerateTerrainBitmapSource(surfaceValues, wrappedSize, maxHeight);

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

        public static TerrainType GetTerrainType(int height, int maxHeight)
        {
            if (height < maxHeight * 0.05)
                return TerrainType.DeepWater;
            else if (height < maxHeight * 0.15)
                return TerrainType.Coast;
            else if (height < maxHeight * 0.40)
                return TerrainType.Grassland;
            else if (height < maxHeight * 0.70)
                return TerrainType.Highlands;
            else if (height <= maxHeight)
                return TerrainType.Mountains;
            else
                return TerrainType.Unknown;
        }

        private static int GetActualMaxHeight(SurfaceData[,] map)
        {
            int sx = map.GetLength(0), sy = map.GetLength(1), m = 0;
            for (int i = 0; i < sx; i++)
                for (int j = 0; j < sy; j++)
                    if (map[i, j].mapDepth > m) m = map[i, j].mapDepth;
            return m;
        }

        // === Placement helpers (put inside SurfaceGeneration) ===

        public enum TileAnchor { TopLeft, TopRight, BottomLeft, BottomRight }


        // Absolutt vann/kyst: depth==0 er alltid vann; "coast" ≈ lav absolutt høyde.
        // coastFraction ~ 0.15 matcher GetTerrainType-terskel (med litt slack).
        public static bool IsWaterOrCoastHeight(int depth, int refMax, double coastFraction = 0.15)
        {
            if (depth <= 0) return true;
            int coastCutoff = Math.Max(1, (int)Math.Ceiling(refMax * coastFraction));
            return depth <= coastCutoff;
        }

        // Tørt terreng i henhold til din enum (Grassland / Highlands)
        public static bool IsDryByEnum(int depth, int refMax)
        {
            var t = GetTerrainType(depth, refMax);
            return t == TerrainType.Grassland || t == TerrainType.Highlands;
        }

        // Chebyshev-radius rundt (x,y) for vann/kyst (brukes som buffer)
        public static bool HasWaterWithinRadiusChebyshev(
            SurfaceData[,] map, int x, int y, int radius, int refMax)
        {
            int sx = map.GetLength(0), sy = map.GetLength(1);
            for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= sx || ny >= sy) continue;
                    if (IsWaterOrCoastHeight(map[nx, ny].mapDepth, refMax)) return true;
                }
            return false;
        }

        // Hjørnesjekk mot valgt anker (TopLeft matcher viewporten din)
        public static bool CornerIsAwayFromWater(
            int x, int y, TileAnchor anchor, SurfaceData[,] map, int refMax)
        {
            int sx = map.GetLength(0), sy = map.GetLength(1);

            // Hvilke 4 ruter utgjør hjørnet avhenger av anker
            (int dx, int dy)[] offsets = anchor switch
            {
                TileAnchor.TopLeft => new[] { (0, 0), (-1, 0), (0, -1), (-1, -1) },
                TileAnchor.TopRight => new[] { (0, 0), (1, 0), (0, -1), (1, -1) },
                TileAnchor.BottomLeft => new[] { (0, 0), (-1, 0), (0, 1), (-1, 1) },
                TileAnchor.BottomRight => new[] { (0, 0), (1, 0), (0, 1), (1, 1) },
                _ => new[] { (0, 0) }
            };

            foreach (var (dx, dy) in offsets)
            {
                int nx = x + dx, ny = y + dy;
                // Krev at alle fire finnes (unngå OOB) og er tørre nok
                if (nx <= 0 || ny <= 0 || nx >= sx || ny >= sy) return false;
                if (IsWaterOrCoastHeight(map[nx, ny].mapDepth, refMax)) return false;
            }
            return true;
        }

        // Sjekk om vi allerede har objekter innenfor gitt radius
        public static bool HasOccupiedWithinRadius(
            HashSet<(int x, int y)> used, int cx, int cy, int radius, int mapSizeX, int mapSizeY)
        {
            for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int nx = cx + dx, ny = cy + dy;
                    if (nx < 0 || ny < 0 || nx >= mapSizeX || ny >= mapSizeY) continue;
                    if (used.Contains((nx, ny))) return true;
                }
            return false;
        }

        // Fisher–Yates shuffle (bruk din 'random' for determinisme ift seed)
        public static void FisherYatesShuffle<T>(IList<T> list, Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // Valgfritt: sub-tile jitter til world-coords (unngå at alt står i venstre hjørne visuelt)
        public static (int jitterX, int jitterY) SubTileJitter(int tileSize, int margin, Random rng)
        {
            int jx = rng.Next(margin, Math.Max(margin + 1, tileSize - margin));
            int jy = rng.Next(margin, Math.Max(margin + 1, tileSize - margin));
            return (jx, jy);
        }

        public static List<(int x, int y, int height)> FindTreePlacementAreas(
            SurfaceData[,] map, int mapSize, int tileSize, int maxHeight)
        {
            // real sizes (wrapped map is square, but keep explicit X/Y to avoid XY/YX bugs)
            int sizeY = map.GetLength(0);
            int sizeX = map.GetLength(1);
            mapSize = Math.Min(sizeX, sizeY);

            // --- helper: consistent height read (map[y, x]) ---
            int H(int x, int y) => map[y, x].mapDepth;

            // === Tunables ===
            const int ScreenSize = 18;
            const int WaterBufferRadius = 1;
            const int MinTreeSpacing = 1;

            int numberOfTrees = random.Next((int)(maxTrees * 0.95), maxTrees);

            // sync maxHeight to actual
            int actualMax = 0;
            for (int y = 0; y < sizeY; y++)
                for (int x = 0; x < sizeX; x++)
                    if (map[y, x].mapDepth > actualMax) actualMax = map[y, x].mapDepth;
            if (actualMax != maxHeight) maxHeight = actualMax;

            // --- local helpers (use H(x,y)) ---
            bool IsWaterOrCoastHeight(int d, int refMax)
            {
                if (d <= 0) return true;
                int coastCutoff = Math.Max(1, (int)Math.Ceiling(refMax * 0.15));
                return d <= coastCutoff;
            }

            bool IsDryByEnum(int d, int refMax)
            {
                var t = GetTerrainType(d, refMax);
                return t == TerrainType.Grassland || t == TerrainType.Highlands;
            }

            bool HasWaterWithinRadius(int cx, int cy, int r)
            {
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                    {
                        int nx = cx + dx, ny = cy + dy;
                        if (nx < 0 || ny < 0 || nx >= sizeX || ny >= sizeY) continue;
                        if (IsWaterOrCoastHeight(H(nx, ny), maxHeight)) return true;
                    }
                return false;
            }

            // ✅ check the actual quad we render: TL(x,y), TR(x+1,y), BL(x,y+1), BR(x+1,y+1)
            bool QuadTopLeftIsDry(int x, int y)
            {
                if (x < 0 || y < 0 || x >= sizeX - 1 || y >= sizeY - 1) return false;

                int dTL = H(x, y);
                int dTR = H(x + 1, y);
                int dBL = H(x, y + 1);
                int dBR = H(x + 1, y + 1);

                if (IsWaterOrCoastHeight(dTL, maxHeight)) return false;
                if (IsWaterOrCoastHeight(dTR, maxHeight)) return false;
                if (IsWaterOrCoastHeight(dBL, maxHeight)) return false;
                if (IsWaterOrCoastHeight(dBR, maxHeight)) return false;

                return true;
            }

            bool HasTreeWithinRadius(HashSet<(int x, int y)> usedSet, int cx, int cy, int r)
            {
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                    {
                        int nx = cx + dx, ny = cy + dy;
                        if (nx < 0 || ny < 0 || nx >= sizeX || ny >= sizeY) continue;
                        if (usedSet.Contains((nx, ny))) return true;
                    }
                return false;
            }

            void Shuffle<T>(IList<T> list)
            {
                for (int k = list.Count - 1; k > 0; k--)
                {
                    int j = random.Next(0, k + 1);
                    (list[k], list[j]) = (list[j], list[k]);
                }
            }

            int screensPerAxis = mapSize / ScreenSize;
            int totalScreens = screensPerAxis * screensPerAxis;
            float treesPerScreen = (float)numberOfTrees / totalScreens;
            int basePerScreen = (int)Math.Floor(treesPerScreen);
            int extraTrees = numberOfTrees - (basePerScreen * totalScreens);

            var treeLocations = new List<(int x, int y, int height)>();
            var used = new HashSet<(int x, int y)>();
            var screenCandidates = new Dictionary<(int sx, int sy), List<(int x, int y, int h)>>();

            // Candidates per screen
            for (int sy = 0; sy < screensPerAxis; sy++)
            {
                for (int sx = 0; sx < screensPerAxis; sx++)
                {
                    int x0 = sx * ScreenSize, y0 = sy * ScreenSize;
                    int x1 = Math.Min(x0 + ScreenSize, mapSize);
                    int y1 = Math.Min(y0 + ScreenSize, mapSize);

                    var list = new List<(int x, int y, int h)>();

                    for (int y = y0; y < y1; y++)
                        for (int x = x0; x < x1; x++)
                        {
                            if (x < 2 || y < 2 || x >= mapSize - 2 || y >= mapSize - 2) continue;

                            int d = H(x, y);

                            if (!IsDryByEnum(d, maxHeight)) continue;
                            if (HasWaterWithinRadius(x, y, WaterBufferRadius)) continue;
                            if (!QuadTopLeftIsDry(x, y)) continue;

                            list.Add((x, y, d));
                        }

                    screenCandidates[(sx, sy)] = list;
                }
            }

            // Phase 1: per screen
            for (int sy = 0; sy < screensPerAxis; sy++)
            {
                for (int sx = 0; sx < screensPerAxis; sx++)
                {
                    int treesThis = basePerScreen + (extraTrees > 0 ? 1 : 0);
                    if (extraTrees > 0) extraTrees--;

                    var candidates = screenCandidates[(sx, sy)];
                    int attempts = 0, maxAttempts = Math.Max(100, treesThis * 15);

                    while (treesThis > 0 && attempts < maxAttempts && candidates.Count > 0)
                    {
                        attempts++;
                        int idx = random.Next(0, candidates.Count);
                        var (x, y, h) = candidates[idx];
                        int last = candidates.Count - 1;
                        candidates[idx] = candidates[last];
                        candidates.RemoveAt(last);

                        if (used.Contains((x, y))) continue;
                        if (HasTreeWithinRadius(used, x, y, MinTreeSpacing)) continue;

                        used.Add((x, y));
                        treeLocations.Add((x, y, h));
                        treesThis--;
                    }
                }
            }

            // Phase 2: neighbor borrowing
            var shortagePool = new List<(int x, int y, int h)>();
            int[,] placedPerScreen = new int[screensPerAxis, screensPerAxis]; // [sy, sx]
            foreach (var (x, y, _) in treeLocations)
            {
                int psx = x / ScreenSize, psy = y / ScreenSize;
                if (psx >= 0 && psx < screensPerAxis && psy >= 0 && psy < screensPerAxis)
                    placedPerScreen[psy, psx]++;
            }

            for (int sy = 0; sy < screensPerAxis; sy++)
                for (int sx = 0; sx < screensPerAxis; sx++)
                {
                    int placed = placedPerScreen[sy, sx];
                    int target = basePerScreen;
                    if (placed < target)
                    {
                        for (int nsy = Math.Max(0, sy - 1); nsy <= Math.Min(sy + 1, screensPerAxis - 1); nsy++)
                            for (int nsx = Math.Max(0, sx - 1); nsx <= Math.Min(sx + 1, screensPerAxis - 1); nsx++)
                                if (screenCandidates.TryGetValue((nsx, nsy), out var neigh) && neigh.Count > 0)
                                    shortagePool.AddRange(neigh);
                    }
                }

            Shuffle(shortagePool);
            foreach (var (x, y, h) in shortagePool)
            {
                if (treeLocations.Count >= numberOfTrees) break;
                if (used.Contains((x, y))) continue;
                if (HasTreeWithinRadius(used, x, y, MinTreeSpacing)) continue;

                used.Add((x, y));
                treeLocations.Add((x, y, h));
            }

            // Phase 3: global fill
            int fillAttempts = 0, maxFillAttempts = Math.Max(numberOfTrees * 20, 2000);
            while (treeLocations.Count < numberOfTrees && fillAttempts < maxFillAttempts)
            {
                fillAttempts++;
                int x = random.Next(2, mapSize - 2);
                int y = random.Next(2, mapSize - 2);
                if (used.Contains((x, y))) continue;

                int d = H(x, y);
                if (!IsDryByEnum(d, maxHeight)) continue;
                if (HasWaterWithinRadius(x, y, WaterBufferRadius)) continue;
                if (!QuadTopLeftIsDry(x, y)) continue;
                if (HasTreeWithinRadius(used, x, y, MinTreeSpacing)) continue;

                used.Add((x, y));
                treeLocations.Add((x, y, d));
            }

            return treeLocations;
        }

        public static List<(int x, int y, int height)> FindHousePlacementAreas(
            SurfaceData[,] map, int mapSize, int maxHeight, List<(int x, int y, int height)> existingTrees)
        {
            int sizeY = map.GetLength(0);
            int sizeX = map.GetLength(1);
            mapSize = Math.Min(sizeX, sizeY);

            int H(int x, int y) => map[y, x].mapDepth;

            var houseLocations = new List<(int x, int y, int height)>();
            int numberOfHouses = random.Next((int)(maxHouses * 0.9), maxHouses);
            var reserved = new HashSet<(int x, int y)>();
            foreach (var (x, y, _) in existingTrees) reserved.Add((x, y));

            bool IsDryByEnumLocal(int d, int refMax)
            {
                var t = GetTerrainType(d, refMax);
                return t == TerrainType.Grassland || t == TerrainType.Highlands;
            }

            // require the whole quad to be dry for houses as well
            bool QuadTopLeftIsDry(int x, int y)
            {
                if (x < 0 || y < 0 || x >= sizeX - 1 || y >= sizeY - 1) return false;
                int dTL = H(x, y);
                int dTR = H(x + 1, y);
                int dBL = H(x, y + 1);
                int dBR = H(x + 1, y + 1);

                if (dTL <= 0 || dTR <= 0 || dBL <= 0 || dBR <= 0) return false;

                int coastCutoff = Math.Max(1, (int)Math.Ceiling(maxHeight * 0.15));
                if (dTL <= coastCutoff || dTR <= coastCutoff || dBL <= coastCutoff || dBR <= coastCutoff) return false;

                return true;
            }

            int spacing = 40;
            int start = spacing / 2;
            int endX = mapSize - spacing / 2 - 1;
            int endY = mapSize - spacing / 2 - 1;

            for (int x = start; x <= endX && houseLocations.Count < numberOfHouses; x += spacing)
            {
                for (int y = start; y <= endY && houseLocations.Count < numberOfHouses; y += spacing)
                {
                    if (reserved.Contains((x, y))) continue;

                    int h = H(x, y);
                    if (!IsDryByEnumLocal(h, maxHeight)) continue;
                    if (!QuadTopLeftIsDry(x, y)) continue;

                    bool isFlat =
                        Math.Abs(h - H(x - 1, y)) < 5 &&
                        Math.Abs(h - H(x + 1, y)) < 5 &&
                        Math.Abs(h - H(x, y - 1)) < 5 &&
                        Math.Abs(h - H(x, y + 1)) < 5;

                    bool inBand = h >= (int)(maxHeight * 0.20) && h < (int)(maxHeight * 0.70);

                    if (isFlat && inBand)
                    {
                        houseLocations.Add((x, y, h));
                        reserved.Add((x, y));
                    }
                }
            }

            if (IncludeTestHousesInFrontOfPlatform)
            {
                int fixedX = 1274, fixedY = 1241;
                for (int n = 0; n < 3; n++)
                {
                    int px = Math.Clamp(fixedX + random.Next(-2, 3), 0, mapSize - 1);
                    int py = Math.Clamp(fixedY + random.Next(-2, 3), 0, mapSize - 1);
                    if (reserved.Contains((px, py))) continue;

                    int h = H(px, py);
                    if (!IsDryByEnumLocal(h, maxHeight)) continue;
                    if (!QuadTopLeftIsDry(px, py)) continue;

                    bool inBand = h >= (int)(maxHeight * 0.15) && h < (int)(maxHeight * 0.70);
                    if (inBand)
                    {
                        houseLocations.Add((px, py, h));
                        reserved.Add((px, py));
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


        public static Color GetTileColor(int height, int maxHeight)
        {
            int red, green, blue;

            if (height < maxHeight * 0.05)
            {
                red = 0;
                green = 0;
                blue = 180 + (int)((height / (maxHeight * 0.05)) * 75);
            }
            else if (height < maxHeight * 0.15)
            {
                red = 0;
                green = (int)((height / (maxHeight * 0.2)) * 100);
                blue = 255;
            }
            else if (height < maxHeight * 0.4)
            {
                red = 0;
                green = 150 + ((height - (int)(maxHeight * 0.2)) * 3);
                blue = 0;
            }
            else if (height < maxHeight * 0.7)
            {
                red = 139 + ((height - (int)(maxHeight * 0.4)) * 3);
                green = 69 + ((height - (int)(maxHeight * 0.4)) * 2);
                blue = 19;
            }
            else
            {
                red = 120 + ((height - (int)(maxHeight * 0.7)) * 3);
                green = 120 + ((height - (int)(maxHeight * 0.7)) * 3);
                blue = 120 + ((height - (int)(maxHeight * 0.7)) * 3);
            }

            return Color.FromArgb(
                255,
                (byte)Math.Clamp(red, 0, 255),
                (byte)Math.Clamp(green, 0, 255),
                (byte)Math.Clamp(blue, 0, 255));
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
                        while (j + width < mapSizeY && !visited[i, j + width] &&
                               IsHighlandOrMountain(map[i, j + width].mapDepth, maxHeight))
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
                            // Highest mapdepth in the area
                            int maxDepth = int.MinValue;
                            for (int di = 0; di < heightBox; di++)
                            {
                                for (int dj = 0; dj < width; dj++)
                                {
                                    int depth = map[i + di, j + dj].mapDepth;
                                    if (depth > maxDepth) maxDepth = depth;
                                }
                            }

                            // Set crashbox
                            map[i, j].crashBox = new SurfaceData.CrashBoxData
                            {
                                width = width,
                                height = heightBox,
                                boxDepth = maxDepth + 20 //Add some padding
                            };

                            numCrashBoxes++;
                        }

                        // Mark the areas visited
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
