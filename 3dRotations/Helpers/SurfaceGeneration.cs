using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using CommonUtilities.GamePlayHelpers;
using Domain;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static bool IncludeTestTreesInFrontOfPlatform = true;
        public static bool IncludeTestHousesInFrontOfPlatform = true;

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
            // 9) Generate ecological meta-map for AI usage, stored in global state
            GameState.SurfaceState.ScreenEcoMetas = GenerateEcoMap(surfaceValues);
            return surfaceValues;
        }

        private static ScreenEcoMeta[,] GenerateEcoMap(SurfaceData[,] map)
        {
            int tilesPerScreen = SurfaceSetup.viewPortSize; // 18
            int tileSize = SurfaceSetup.tileSize;           // 75

            var ecoMap = new ScreenEcoMeta[MapSetup.screensPrMap, MapSetup.screensPrMap];

            // init (Y outer, X inner)
            int screenCount = 0;
            for (int screenY = 0; screenY < MapSetup.screensPrMap; screenY++)
            {
                for (int screenX = 0; screenX < MapSetup.screensPrMap; screenX++)
                {
                    screenCount++;
                    ecoMap[screenY, screenX].ScreenCount = screenCount;
                    ecoMap[screenY, screenX].BioTileCount = 0;
                    ecoMap[screenY, screenX].BioTiles = new List<TileCoord>(128);
                }
            }

            for (int globalY = 1; globalY < MapSetup.globalMapSize - 1; globalY++)
            {
                for (int globalX = 1; globalX < MapSetup.globalMapSize - 1; globalX++)
                {
                    int screenY = globalY / tilesPerScreen;
                    int screenX = globalX / tilesPerScreen;

                    if ((uint)screenY >= (uint)MapSetup.screensPrMap || (uint)screenX >= (uint)MapSetup.screensPrMap)
                        continue;

                    var terrainType = GamePlayHelpers.GetTerrainType(map[globalY, globalX].mapDepth, MapSetup.maxHeight);
                    if (terrainType == GamePlayHelpers.TerrainType.Grassland ||
                        terrainType == GamePlayHelpers.TerrainType.Highlands)
                    {
                        var meta = ecoMap[screenY, screenX];
                        meta.BioTileCount++;

                        // These are global coords of the tile's top-left corner, which the AI can use to correlate with the global map
                        meta.BioTiles.Add(new TileCoord
                        {
                            Y = globalY * tileSize, // local world Z/Y
                            X = globalX * tileSize  // local world X
                        });

                        ecoMap[screenY, screenX] = meta;
                    }
                }
            }

            return ecoMap;
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
                    map[i, j] = new SurfaceData { mapDepth = (int)(Math.Pow(perlinValue, heightExponent) * 20 * zFactor), mapId = mapId, isInfected = false };
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
            var t = GamePlayHelpers.GetTerrainType(depth, refMax);
            return t == GamePlayHelpers.TerrainType.Grassland || t == GamePlayHelpers.TerrainType.Highlands;
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
                var t = GamePlayHelpers.GetTerrainType(d, refMax);
                return t == GamePlayHelpers.TerrainType.Grassland || t == GamePlayHelpers.TerrainType.Highlands;
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
                var t = GamePlayHelpers.GetTerrainType(d, refMax);
                return t == GamePlayHelpers.TerrainType.Grassland || t == GamePlayHelpers.TerrainType.Highlands;
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

        // ============================================================
        //  TOWER PLACEMENT (1 near platform + total N across map)
        // ============================================================

        public static void FlattenTerrainAroundTowers_ToHighlands(
            SurfaceData[,] map,
            int maxHeight,
            List<(int x, int y, int height)> towerLocations,
            bool writeDebugLogs = true)
        {
            if (map == null || map.Length == 0) return;
            if (towerLocations == null || towerLocations.Count == 0) return;

            // map is [y, x] in your project
            int sizeY = map.GetLength(0);
            int sizeX = map.GetLength(1);

            // Highlands threshold per your enum logic (>= 0.40 * maxHeight)
            int highlandsMin = (int)(maxHeight * 0.40);

            void Log(string s)
            {
                if (writeDebugLogs)
                    System.Diagnostics.Debug.WriteLine(s);
            }

            Log($"=== ENTER FlattenTerrainAroundTowers_ToHighlands (3x3) ===");
            Log($"Towers={towerLocations.Count} highlandsMin={highlandsMin} maxHeight={maxHeight}");

            int totalChanged = 0;

            foreach (var (tx, ty, _) in towerLocations)
            {
                if (tx < 0 || ty < 0 || tx >= sizeX || ty >= sizeY)
                {
                    Log($"[SKIP] Tower out of bounds: ({tx},{ty})");
                    continue;
                }

                int beforeCenter = map[ty, tx].mapDepth;

                // We force the entire 3x3 to the same target depth, at least Highlands min.
                int target = Math.Max(beforeCenter, highlandsMin);

                int changedThisTower = 0;

                // 3x3 block: centered on tower tile (tx,ty)
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int x = tx + dx;
                        int y = ty + dy;

                        if (x < 0 || y < 0 || x >= sizeX || y >= sizeY)
                            continue;

                        int before = map[y, x].mapDepth;
                        if (before != target)
                        {
                            map[y, x].mapDepth = target;
                            changedThisTower++;
                            totalChanged++;
                        }
                    }
                }

                Log($"Tower ({tx},{ty}) centerDepth={beforeCenter} -> target={target} changedTiles={changedThisTower}");
            }

            Log($"TOTAL changedTiles={totalChanged}");
            Log($"=== EXIT FlattenTerrainAroundTowers_ToHighlands (3x3) ===");
        }

        public static List<(int x, int y, int height)> FindTowerPlacements(
            SurfaceData[,] map,
            int mapSize,
            int tileSize,
            int maxHeight,
            int totalTowers = 10)
        {
            Debug.WriteLine("=== ENTER FindTowerPlacements ===");

            int sizeY = map.GetLength(0);
            int sizeX = map.GetLength(1);
            mapSize = Math.Min(mapSize, Math.Min(sizeX, sizeY));

            // ✅ YX (outer = y, inner = x)
            int H(int x, int y) => map[y, x].mapDepth;

            Debug.WriteLine($"Map: sizeX={sizeX} sizeY={sizeY} mapSize={mapSize} maxHeight={maxHeight} totalTowers={totalTowers}");

            int nearRadius = 17;
            int spacing = 6;                 // same “looks good” value you liked
            int landingBufferTiles = 1;      // exclude platform + 1 tile ring around it

            // ------------------------------------------------------------
            // 0) Find landing platform robustly (8x8 flat block, highest depth near center)
            //    Return both center + top-left so we can exclude it.
            // ------------------------------------------------------------
            (int centerX, int centerY, int depth, int topLeftX, int topLeftY) FindLandingCenterLocal()
            {
                int cx = mapSize / 2;
                int cy = mapSize / 2;

                int bestDepth = -1;
                int bestCenterX = cx;
                int bestCenterY = cy;
                int bestTopLeftX = Math.Max(0, cx - landingAreaSize / 2);
                int bestTopLeftY = Math.Max(0, cy - landingAreaSize / 2);

                int searchRadius = 160; // plenty
                int x0 = Math.Max(0, cx - searchRadius);
                int x1 = Math.Min(mapSize - landingAreaSize, cx + searchRadius);
                int y0 = Math.Max(0, cy - searchRadius);
                int y1 = Math.Min(mapSize - landingAreaSize, cy + searchRadius);

                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        int d = H(x, y);
                        if (d <= 0) continue;

                        bool ok = true;
                        for (int yy = 0; yy < landingAreaSize && ok; yy++)
                        {
                            for (int xx = 0; xx < landingAreaSize; xx++)
                            {
                                if (H(x + xx, y + yy) != d) { ok = false; break; }
                            }
                        }

                        if (!ok) continue;

                        // pick the highest flat block (your platform is a high plateau)
                        if (d > bestDepth)
                        {
                            bestDepth = d;
                            bestTopLeftX = x;
                            bestTopLeftY = y;
                            bestCenterX = x + landingAreaSize / 2;
                            bestCenterY = y + landingAreaSize / 2;
                        }
                    }
                }

                if (bestDepth < 0)
                {
                    // fallback
                    bestDepth = H(cx, cy);
                    bestCenterX = cx;
                    bestCenterY = cy;
                }

                return (bestCenterX, bestCenterY, bestDepth, bestTopLeftX, bestTopLeftY);
            }

            var landing = FindLandingCenterLocal();
            int landingX = landing.centerX;
            int landingY = landing.centerY;
            int landingTopLeftX = landing.topLeftX;
            int landingTopLeftY = landing.topLeftY;

            Debug.WriteLine($"Landing center=({landingX},{landingY}) depth={landing.depth} TL=({landingTopLeftX},{landingTopLeftY})");

            // ------------------------------------------------------------
            // Helper: exclude landing platform (plus buffer ring)
            // ------------------------------------------------------------
            bool IsInsideLandingPlatformOrBuffer(int x, int y)
            {
                int minX = landingTopLeftX - landingBufferTiles;
                int minY = landingTopLeftY - landingBufferTiles;
                int maxX = landingTopLeftX + landingAreaSize - 1 + landingBufferTiles;
                int maxY = landingTopLeftY + landingAreaSize - 1 + landingBufferTiles;

                return x >= minX && x <= maxX && y >= minY && y <= maxY;
            }

            // ------------------------------------------------------------
            // 1) Candidate filter (USE YOUR EXISTING HELPERS)
            // ------------------------------------------------------------
            bool IsOkTile(int x, int y)
            {
                if (x < 2 || y < 2 || x >= mapSize - 2 || y >= mapSize - 2) return false;

                // Never place on platform or its buffer ring
                if (IsInsideLandingPlatformOrBuffer(x, y)) return false;

                int d = H(x, y);

                // ✅ your helpers
                if (IsWaterOrCoastHeight(d, maxHeight)) return false;
                if (!IsDryByEnum(d, maxHeight)) return false;

                // make sure the rendered quad corner isn't touching water/coast
                if (!CornerIsAwayFromWater(x, y, TileAnchor.TopLeft, map, maxHeight)) return false;

                // small water buffer (same as you use elsewhere)
                if (HasWaterWithinRadiusChebyshev(map, x, y, radius: 1, refMax: maxHeight)) return false;

                return true;
            }

            // ------------------------------------------------------------
            // 2) Build pools (Highlands preferred, else Grassland)
            // ------------------------------------------------------------
            var highPool = new List<(int x, int y, int h)>();
            var grassPool = new List<(int x, int y, int h)>();

            int okCount = 0;

            for (int y = 2; y < mapSize - 2; y++)
            {
                for (int x = 2; x < mapSize - 2; x++)
                {
                    if (!IsOkTile(x, y)) continue;

                    int d = H(x, y);
                    okCount++;

                    var t = GamePlayHelpers.GetTerrainType(d, maxHeight);
                    if (t == GamePlayHelpers.TerrainType.Highlands) highPool.Add((x, y, d));
                    else if (t == GamePlayHelpers.TerrainType.Grassland) grassPool.Add((x, y, d));
                }
            }

            Debug.WriteLine($"Candidates OK={okCount} highlands={highPool.Count} grassland={grassPool.Count}");

            FisherYatesShuffle(highPool, random);
            FisherYatesShuffle(grassPool, random);

            // ------------------------------------------------------------
            // 3) Placement (1 near platform + rest global)
            // ------------------------------------------------------------
            var towers = new List<(int x, int y, int height)>(totalTowers);
            var used = new HashSet<(int x, int y)>();

            bool TryAdd(int x, int y, int h)
            {
                if (HasOccupiedWithinRadius(used, x, y, spacing, mapSize, mapSize))
                    return false;

                used.Add((x, y));
                towers.Add((x, y, h));
                return true;
            }

            // 3a) Near-platform tower: find nearest Highlands within radius 17, fallback Grassland
            bool placedNear = false;

            bool TryFindNearestNear(GamePlayHelpers.TerrainType wanted, out (int x, int y, int h) best)
            {
                int bestD2 = int.MaxValue;
                best = default;
                bool found = false;

                int x0 = Math.Max(2, landingX - nearRadius);
                int x1 = Math.Min(mapSize - 3, landingX + nearRadius);
                int y0 = Math.Max(2, landingY - nearRadius);
                int y1 = Math.Min(mapSize - 3, landingY + nearRadius);

                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        int dx = x - landingX;
                        int dy = y - landingY;
                        int d2 = dx * dx + dy * dy;
                        if (d2 > nearRadius * nearRadius) continue;

                        if (!IsOkTile(x, y)) continue;

                        int d = H(x, y);
                        if (GamePlayHelpers.GetTerrainType(d, maxHeight) != wanted) continue;

                        if (d2 < bestD2)
                        {
                            bestD2 = d2;
                            best = (x, y, d);
                            found = true;
                        }
                    }
                }

                return found;
            }

            // Highlands near
            if (TryFindNearestNear(GamePlayHelpers.TerrainType.Highlands, out var nearHi))
            {
                placedNear = TryAdd(nearHi.x, nearHi.y, nearHi.h);
                Debug.WriteLine($"Near tower (Highlands) => ({nearHi.x},{nearHi.y}) depth={nearHi.h} placed={placedNear}");
            }

            // Grassland fallback near
            if (!placedNear && TryFindNearestNear(GamePlayHelpers.TerrainType.Grassland, out var nearGr))
            {
                placedNear = TryAdd(nearGr.x, nearGr.y, nearGr.h);
                Debug.WriteLine($"Near tower (Grassland) => ({nearGr.x},{nearGr.y}) depth={nearGr.h} placed={placedNear}");
            }

            if (!placedNear)
                Debug.WriteLine("WARNING: No near-platform tower candidate found in radius 17 (excluding platform).");

            // 3b) Fill global
            void FillFrom(List<(int x, int y, int h)> pool, string label)
            {
                int before = towers.Count;

                foreach (var (x, y, h) in pool)
                {
                    if (towers.Count >= totalTowers) break;
                    TryAdd(x, y, h);
                }

                Debug.WriteLine($"FillFrom {label}: added={towers.Count - before}, now={towers.Count}/{totalTowers}");
            }

            FillFrom(highPool, "Highlands");
            FillFrom(grassPool, "Grassland");

            Debug.WriteLine($"TOWERS PLACED: {towers.Count}/{totalTowers}");
            for (int i = 0; i < towers.Count; i++)
                Debug.WriteLine($"To  w er #{i + 1}: ({towers[i].x},{towers[i].y}) depth={towers[i].height}");

            Debug.WriteLine("=== EXIT FindTowerPlacements ===");
            return towers;
        }

        //------------------------------------------------------------- End Tower Placement

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
                                boxDepth = maxDepth + 20//Add some padding
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
