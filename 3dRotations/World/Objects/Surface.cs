using _3dRotations.Helpers;
using _3dTesting.Helpers;
using Domain;
using System;
using System.Collections.Generic;
using static Domain._3dSpecificsImplementations;
using CommonUtilities.CommonSetup;
using CommonUtilities.CommonGlobalState;


namespace _3dRotations.World.Objects
{
    public class Surface : ISurface
    {
        public Vector3 GlobalMapRotation { get; set; } = new Vector3 { x = 0, y = 0, z = 0 };
        public List<ITriangleMeshWithColor> RotatedSurfaceTriangles  { get; set; }
        public HashSet<long?> LandBasedIds { get; set; } = new HashSet<long?>();

        const bool debugSurfaceBasedObjects = false; // Set to true to debug surface based objects

        public int SurfaceWidth() {  return SurfaceSetup.surfaceWidth; }
        public int GlobalMapSize() { return MapSetup.globalMapSize; }
        public int ViewPortSize() { return SurfaceSetup.viewPortSize; }
        public int TileSize() { return SurfaceSetup.tileSize; }
        public int MaxHeight() { return MapSetup.maxHeight; }

        public I3dObject GetSurfaceViewPort()
        {
            var newSurface = new List<ITriangleMeshWithColor>();
            var surface = new _3dObject { ObjectId = GameState.ObjectIdCounter++ };
            var viewPortCrashBoxes = new List<List<IVector3>>(); // Ny liste for ViewPort-crashboxes

            var viewPort = SurfaceGeneration.Return2DViewPort(ViewPortSize(), (int)GameState.SurfaceState.GlobalMapPosition.x, (int)GameState.SurfaceState.GlobalMapPosition.z, GameState.SurfaceState.Global2DMap, TileSize());
            var ZRemainer = GameState.SurfaceState.GlobalMapPosition.z % TileSize();
            var XRemainer = GameState.SurfaceState.GlobalMapPosition.x % TileSize();
            var YRemainer = GameState.SurfaceState.GlobalMapPosition.y;

            var YPosition = -(TileSize() * ViewPortSize() / 2);
            var worldPosition = new Vector3 { x = (GameState.SurfaceState.GlobalMapPosition.x - TileSize()), y = 0, z = (GameState.SurfaceState.GlobalMapPosition.z - TileSize()) };
            for (int i = 1; i < (ViewPortSize() / 1.5) + 2; i++)
            {
                worldPosition.z += TileSize();
                YPosition += TileSize();
                var XPosition = -(TileSize() * ViewPortSize() / 2);

                for (int j = 1; j < ViewPortSize() - 1; j++)
                {
                    worldPosition.x += TileSize();
                    XPosition += TileSize();

                    var currentTile = viewPort[i, j];
                    var surfaceId = currentTile.mapId;

                    // --- Build terrain from map
                    var ZPostition1 = currentTile.mapDepth;
                    var ZPostition2 = viewPort[i, j + 1].mapDepth;
                    var ZPostition3 = viewPort[i + 1, j + 1].mapDepth;
                    var ZPostition4 = viewPort[i + 1, j].mapDepth;

                    var color1 = GetTileColorGradient((ZPostition1 + ZPostition2) / 2, MaxHeight());
                    var color2 = GetTileColorGradient((ZPostition1 + ZPostition2) / 2, MaxHeight());
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
                            x = (XPosition - XRemainer) - TileSize(),
                            y = YPosition - ZRemainer,
                            z = 0 // Sealevel
                        };

                        var max = new Vector3
                        {
                            x = XPosition + ((box.width * TileSize()) - XRemainer) - TileSize(),
                            y = YPosition + (box.height * TileSize()) - ZRemainer,
                            z = 40 + currentTile.mapDepth // Max map depth
                        };

                        var crashBoxCorners = _3dObjectHelpers.GenerateCrashBoxCorners(min, max);
                        viewPortCrashBoxes.Add(crashBoxCorners);
                    }

                    var triangle1 = new TriangleMeshWithColor
                    {
                        Color = color1,
                        landBasedPosition = surfaceId,
                        vert1 = { x = XPosition - XRemainer, y = YPosition - ZRemainer, z = ZPostition1 - YRemainer },
                        vert2 = { x = XPosition + TileSize() - XRemainer, y = YPosition - ZRemainer, z = ZPostition2 - YRemainer },
                        vert3 = { x = XPosition + TileSize() - XRemainer, y = YPosition + TileSize() - ZRemainer, z = ZPostition3 - YRemainer }
                    };

                    var triangle2 = new TriangleMeshWithColor
                    {
                        Color = color2,
                        landBasedPosition = surfaceId,
                        vert1 = { x = XPosition - XRemainer, y = YPosition - ZRemainer, z = ZPostition1 - YRemainer },
                        vert2 = { x = XPosition + TileSize() - XRemainer, y = YPosition + TileSize() - ZRemainer, z = ZPostition3 - YRemainer },
                        vert3 = { x = XPosition - XRemainer, y = YPosition + TileSize() - ZRemainer, z = ZPostition4 - YRemainer }
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

        public void Create2DMap(int? maxTrees,int? maxHouses)
        {
            //Gets the pseudo random map in 2d
            GameState.SurfaceState.Global2DMap = SurfaceGeneration.ReturnPseudoRandomMap(MapSetup.globalMapSize, maxHeight: out MapSetup.maxHeight,maxTrees,maxHouses);
            GameState.SurfaceState.GlobalMapBitmap = SurfaceGeneration.GenerateTerrainBitmapSource(GameState.SurfaceState.Global2DMap, MapSetup.globalMapSize, MapSetup.maxHeight);
        }
    }
}
