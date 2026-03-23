using _3dRotations.Helpers;
using _3dTesting.Helpers;
using CommonUtilities._3DHelpers;
using Domain;
using System;
using System.Collections.Generic;
using static Domain._3dSpecificsImplementations;
using CommonUtilities.CommonSetup;
using CommonUtilities.CommonGlobalState;
using CommonUtilities.GamePlayHelpers;


namespace _3dRotations.World.Objects
{
    public class Surface : ISurface
    {
        public Vector3 GlobalMapRotation { get; set; } = new Vector3 { x = 0, y = 0, z = 0 };
        public List<ITriangleMeshWithColor> RotatedSurfaceTriangles  { get; set; }
        public HashSet<long?> LandBasedIds { get; set; } = new HashSet<long?>();
        private const float ShipShadowRadius = 105f;
        private const float ShipShadowDarkenFactor = 0.5f;

        const bool debugSurfaceBasedObjects = false; // Set to true to debug surface based objects

        public int SurfaceWidth() {  return SurfaceSetup.surfaceWidth; }
        public int GlobalMapSize() { return MapSetup.globalMapSize; }
        public int ViewPortSize() { return SurfaceSetup.viewPortSize; }
        public int TileSize() { return SurfaceSetup.tileSize; }
        public int MaxHeight() { return MapSetup.maxHeight; }

        public I3dObject GetSurfaceViewPort()
        {
            var globalMapPosition = GameState.SurfaceState.GlobalMapPosition;
            var global2DMap = GameState.SurfaceState.Global2DMap!;
            int tileSize = TileSize();
            int viewPortSize = ViewPortSize();
            int maxHeight = MaxHeight();
            int rowLimit = (int)(viewPortSize / 1.5) + 2;
            int tileCount = (rowLimit - 1) * (viewPortSize - 2);

            var newSurface = new List<ITriangleMeshWithColor>(tileCount * 2);
            var surface = new _3dObject { ObjectId = GameState.ObjectIdCounter++ };
            var viewPortCrashBoxes = new List<List<IVector3>>(tileCount / 4); // Ny liste for ViewPort-crashboxes

            int mapSize = global2DMap.GetLength(0);
            int mapZIndex = ((int)globalMapPosition.z / tileSize) % mapSize;
            int mapXIndex = ((int)globalMapPosition.x / tileSize) % mapSize;
            if (mapZIndex < 0)
            {
                mapZIndex += mapSize;
            }

            if (mapXIndex < 0)
            {
                mapXIndex += mapSize;
            }

            var ZRemainer = globalMapPosition.z % tileSize;
            var XRemainer = globalMapPosition.x % tileSize;
            var YRemainer = globalMapPosition.y;
            var shadowCasters = GetShadowCasters();
            bool hasShadowCasters = shadowCasters.Count > 0;
            float halfTileSize = tileSize * 0.5f;

            var YPosition = -(tileSize * viewPortSize / 2);
            for (int i = 1; i < rowLimit; i++)
            {
                int currentMapY = (mapZIndex + i) % mapSize;
                int nextMapY = (currentMapY + 1) % mapSize;
                YPosition += TileSize();
                var XPosition = -(tileSize * viewPortSize / 2);

                for (int j = 1; j < viewPortSize - 1; j++)
                {
                    int currentMapX = (mapXIndex + j) % mapSize;
                    int nextMapX = (currentMapX + 1) % mapSize;
                    XPosition += tileSize;

                    var currentTile = global2DMap[currentMapY, currentMapX];
                    var surfaceId = currentTile.mapId;

                    // --- Build terrain from map
                    var ZPostition1 = currentTile.mapDepth;
                    var ZPostition2 = global2DMap[currentMapY, nextMapX].mapDepth;
                    var ZPostition3 = global2DMap[nextMapY, nextMapX].mapDepth;
                    var ZPostition4 = global2DMap[nextMapY, currentMapX].mapDepth;

                    var color1 = GetTileColorGradient((ZPostition1 + ZPostition2) / 2, maxHeight);
                    var color2 = color1;
                    if (currentTile.isInfected)
                    {
                        color1 = "FF0000"; // Red for infected tiles
                        color2 = "FF0000"; // Red for infected tiles
                    }

                    if (currentTile.hasLandbasedObject && debugSurfaceBasedObjects)
                    {
                        //Just for debugging, tiles with trees or houses need a different color
                        color1 = "FF0000"; // Red for land-based tiles
                        color2 = "FF0000"; // Red for land-based tiles
                    }

                    // Create SurfaceCrashbox directly here if needed
                    if (currentTile.crashBox != null)
                    {
                        var box = currentTile.crashBox.Value;

                        var min = new Vector3
                        {
                            x = (XPosition - XRemainer) - tileSize,
                            y = YPosition - ZRemainer,
                            z = 0 // Sealevel
                        };

                        var max = new Vector3
                        {
                            x = XPosition + ((box.width * tileSize) - XRemainer) - tileSize,
                            y = YPosition + (box.height * tileSize) - ZRemainer,
                            z = 40 + currentTile.mapDepth // Max map depth
                        };

                        var crashBoxCorners = _3dObjectHelpers.GenerateCrashBoxCorners(min, max);
                        viewPortCrashBoxes.Add(crashBoxCorners);
                    }

                    if (hasShadowCasters)
                    {
                        float tileCenterX = (XPosition + halfTileSize) - XRemainer;
                        float tileCenterY = (YPosition + halfTileSize) - ZRemainer;
                        float shadowFactor = GetShadowFactor(tileCenterX, tileCenterY, shadowCasters);
                        if (shadowFactor > 0f)
                        {
                            float darkenFactor = 1f - ((1f - ShipShadowDarkenFactor) * shadowFactor);
                            color1 = DarkenHexColor(color1, darkenFactor);
                            color2 = color1;
                        }
                    }

                    var triangle1 = new TriangleMeshWithColor
                    {
                        Color = color1,
                        landBasedPosition = surfaceId,
                        vert1 = { x = XPosition - XRemainer, y = YPosition - ZRemainer, z = ZPostition1 - YRemainer },
                        vert2 = { x = XPosition + tileSize - XRemainer, y = YPosition - ZRemainer, z = ZPostition2 - YRemainer },
                        vert3 = { x = XPosition + tileSize - XRemainer, y = YPosition + tileSize - ZRemainer, z = ZPostition3 - YRemainer }
                    };

                    var triangle2 = new TriangleMeshWithColor
                    {
                        Color = color2,
                        landBasedPosition = surfaceId,
                        vert1 = { x = XPosition - XRemainer, y = YPosition - ZRemainer, z = ZPostition1 - YRemainer },
                        vert2 = { x = XPosition + tileSize - XRemainer, y = YPosition + tileSize - ZRemainer, z = ZPostition3 - YRemainer },
                        vert3 = { x = XPosition - XRemainer, y = YPosition + tileSize - ZRemainer, z = ZPostition4 - YRemainer }
                    };

                    newSurface.Add(triangle1);
                    newSurface.Add(triangle2);
                }
            }

            surface.ObjectParts.Add(new _3dObjectPart { PartName = "Surface", Triangles = newSurface, IsVisible = true });
            surface.CrashBoxes = viewPortCrashBoxes;
            surface.CrashBoxes.AddRange(GetMainSurfaceCrashBox());
            return surface;
        }


        private List<List<IVector3>> GetMainSurfaceCrashBox()
        {
            //var min = new Vector3 { x = -1200, y = -600, z = -1000 };
            //var max = new Vector3 { x = 1200, y = 1500, z = 400 };

            var min = new Vector3 { x = -500, y = -100, z = 1000 };
            var max = new Vector3 { x = 500, y = 1000, z = -350 };

            return new List<List<IVector3>>
            {
                _3dObjectHelpers.GenerateCrashBoxCorners(min, max)
            };
        }

        private static string GetTileColorGradient(int height, int maxHeight)
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

            return $"{Math.Clamp(red, 0, 255):X2}{Math.Clamp(green, 0, 255):X2}{Math.Clamp(blue, 0, 255):X2}";
        }

        public void Create2DMap(int? maxTrees, int? maxHouses, GameModes gameMode,string? surfaceFile)
        {
            GameState.SurfaceState.SurfaceFilePath = surfaceFile;
            // ------------------------------------------------------------
            // LIVE
            // ------------------------------------------------------------
            if (gameMode == GameModes.Live)
            {
                GameState.SurfaceState.Global2DMap =
                    SurfaceGeneration.ReturnPseudoRandomMap(
                        MapSetup.globalMapSize,
                        maxHeight: out MapSetup.maxHeight,
                        maxTrees,
                        maxHouses);

                GameState.SurfaceState.SurfaceHash = 0; // not replay eligible
            }

            // ------------------------------------------------------------
            // PLAYBACK
            // ------------------------------------------------------------
            else if (gameMode == GameModes.Playback)
            {
                if (GameplayHelpers.SurfaceIO.SurfaceIO.TryLoad(
                        surfaceFile,
                        out var loadedMap,
                        out var hash))
                {
                    GameState.SurfaceState.Global2DMap = loadedMap;
                    GameState.SurfaceState.SurfaceHash = hash;
                    MapSetup.maxHeight = GetActualMaxHeight(loadedMap);
                }
                else
                {
                    // Fail-safe: generate instead and effectively disable replay
                    GameState.SurfaceState.Global2DMap =
                        SurfaceGeneration.ReturnPseudoRandomMap(
                            MapSetup.globalMapSize,
                            maxHeight: out MapSetup.maxHeight,
                            maxTrees,
                            maxHouses);

                    GameState.SurfaceState.SurfaceHash = 0;
                }
            }

            // ------------------------------------------------------------
            // RECORD
            // ------------------------------------------------------------
            else if (gameMode == GameModes.Record)
            {
                 GameState.SurfaceState.Global2DMap =
                    SurfaceGeneration.ReturnPseudoRandomMap(
                        MapSetup.globalMapSize,
                        maxHeight: out MapSetup.maxHeight,
                        maxTrees,
                        maxHouses);

                var hash = GameplayHelpers.SurfaceIO.SurfaceIO.Save(
                    surfaceFile,
                    GameState.SurfaceState.Global2DMap);

                GameState.SurfaceState.SurfaceHash = hash;
            }
            // ------------------------------------------------------------
            // Always build bitmap from surface
            // ------------------------------------------------------------
            //We need to generate Ecomaps if they are not generated already, as they are used for AI
            if (GameState.SurfaceState.ScreenEcoMetas[0,0].BioTiles==null)
            {
                GameState.SurfaceState.ScreenEcoMetas = SurfaceGeneration.GenerateEcoMap(
                    GameState.SurfaceState.Global2DMap);
            }

            int mapSize = GameState.SurfaceState.Global2DMap.GetLength(0); // siden kartet er square
            SurfaceGeneration.GenerateTerrainBitmapSource(
                GameState.SurfaceState.Global2DMap,
                mapSize,
                MapSetup.maxHeight);
        }

        private static int GetActualMaxHeight(SurfaceData[,] map)
        {
            int sx = map.GetLength(0), sy = map.GetLength(1), m = 0;
            for (int i = 0; i < sx; i++)
                for (int j = 0; j < sy; j++)
                    if (map[i, j].mapDepth > m) m = map[i, j].mapDepth;
            return m;
        }

        private static string DarkenHexColor(string color, float factor)
        {
            if (string.IsNullOrWhiteSpace(color) || color.Length < 6)
            {
                return color;
            }

            factor = Math.Clamp(factor, 0f, 1f);
            color = color.TrimStart('#');

            int red = Convert.ToInt32(color.Substring(0, 2), 16);
            int green = Convert.ToInt32(color.Substring(2, 2), 16);
            int blue = Convert.ToInt32(color.Substring(4, 2), 16);

            red = (int)(red * factor);
            green = (int)(green * factor);
            blue = (int)(blue * factor);

            return $"{Math.Clamp(red, 0, 255):X2}{Math.Clamp(green, 0, 255):X2}{Math.Clamp(blue, 0, 255):X2}";
        }

        private static List<(float x, float y)> GetShadowCasters()
        {
            var globalMapPosition = GameState.SurfaceState.GlobalMapPosition;
            var surfaceOffsets = GameState.SurfaceState.SurfaceViewportObject?.ObjectOffsets;
            var shipOffsets = GameState.ShipState?.ShipObjectOffsets;
            var aiObjects = GameState.SurfaceState?.AiObjects;
            var shadowCasters = new List<(float x, float y)>(1 + (aiObjects?.Count ?? 0));

            if (shipOffsets != null && surfaceOffsets != null)
            {
                shadowCasters.Add((shipOffsets.x - surfaceOffsets.x, shipOffsets.z - surfaceOffsets.z));
            }

            if (aiObjects == null)
            {
                return shadowCasters;
            }

            for (int i = 0; i < aiObjects.Count; i++)
            {
                var aiObject = aiObjects[i];
                if (aiObject == null || !aiObject.HasShadow || aiObject.ImpactStatus?.HasExploded == true)
                {
                    continue;
                }

                var alignedWorldPosition = SurfacePositionSyncHelpers.GetSurfaceAlignedWorldPosition(aiObject);
                shadowCasters.Add((
                    alignedWorldPosition.x - globalMapPosition.x,
                    alignedWorldPosition.z - globalMapPosition.z));
            }

            return shadowCasters;
        }

        private static float GetShadowFactor(float tileCenterX, float tileCenterY, List<(float x, float y)> shadowCasters)
        {
            float strongestShadow = 0f;

            for (int i = 0; i < shadowCasters.Count; i++)
            {
                var shadowCaster = shadowCasters[i];
                float shadowFactor = GetTileShadowFactor(tileCenterX, tileCenterY, shadowCaster.x, shadowCaster.y, ShipShadowRadius);
                if (shadowFactor > strongestShadow)
                {
                    strongestShadow = shadowFactor;
                }
            }

            return strongestShadow;
        }

        private static float GetTileShadowFactor(float tileCenterX, float tileCenterY, float shadowCenterX, float shadowCenterY, float shadowRadius)
        {
            float dx = tileCenterX - shadowCenterX;
            float dy = tileCenterY - shadowCenterY;
            float distanceSquared = (dx * dx) + (dy * dy);
            float shadowRadiusSquared = shadowRadius * shadowRadius;
            if (distanceSquared >= shadowRadiusSquared)
            {
                return 0f;
            }

            float distance = MathF.Sqrt(distanceSquared);
            float normalized = 1f - (distance / shadowRadius);
            return Math.Clamp(normalized, 0f, 1f);
        }

        private static float GetDistanceSquared(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            float dz = a.z - b.z;
            return (dx * dx) + (dy * dy) + (dz * dz);
        }

        private static Vector3 Midpoint(Vector3 a, Vector3 b)
        {
            return new Vector3
            {
                x = (a.x + b.x) * 0.5f,
                y = (a.y + b.y) * 0.5f,
                z = (a.z + b.z) * 0.5f
            };
        }

    }
}
