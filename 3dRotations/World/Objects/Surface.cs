using _3dRotations.Helpers;
using _3dTesting.Helpers;
using Domain;
using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using static Domain._3dSpecificsImplementations;


namespace _3dRotations.World.Objects
{
    public class Surface : ISurface
    {
        public Vector3 GlobalMapPosition { get; set; } = new Vector3 { x = 95100, y = 0, z = 95200 };
        public Vector3 GlobalMapRotation { get; set; } = new Vector3 { x = 0, y = 0, z = 0 };
        public SurfaceData[,]? Global2DMap { get; set; } = new SurfaceData[globalMapSize, globalMapSize];
        public BitmapSource? GlobalMapBitmap { get; set; }
        public List<ITriangleMeshWithColor> RotatedSurfaceTriangles  { get; set; }
        public HashSet<long?> LandBasedIds { get; set; } = new HashSet<long?>();

        const bool debugSurfaceBasedObjects = false; // Set to true to debug surface based objects
        const int surfaceWidth = 1350;
        const int globalMapSize = 2500+(surfaceWidth/tileSize);
        const int viewPortSize = surfaceWidth / tileSize;
        const int tileSize = 75;
        int maxHeight = 75; //Height elevation for the map

        public int SurfaceWidth() {  return surfaceWidth; }
        public int GlobalMapSize() { return globalMapSize; }
        public int ViewPortSize() { return viewPortSize; }
        public int TileSize() { return tileSize; }
        public int MaxHeight() { return maxHeight; }

        public I3dObject GetSurfaceViewPort()
        {
            var newSurface = new List<ITriangleMeshWithColor>();
            var surface = new _3dObject();
            var viewPortCrashBoxes = new List<List<IVector3>>(); // Ny liste for ViewPort-crashboxes

            var viewPort = SurfaceGeneration.Return2DViewPort(viewPortSize, (int)GlobalMapPosition.x, (int)GlobalMapPosition.z, Global2DMap, tileSize);
            var ZRemainer = GlobalMapPosition.z % tileSize;
            var XRemainer = GlobalMapPosition.x % tileSize;
            var YRemainer = GlobalMapPosition.y;

            var YPosition = -(tileSize * viewPortSize / 2);
            var worldPosition = new Vector3 { x = (GlobalMapPosition.x - 75), y = 0, z = (GlobalMapPosition.z - 75) };

            for (int i = 1; i < (viewPortSize / 1.5) + 2; i++)
            {
                worldPosition.z += tileSize;
                YPosition += tileSize;
                var XPosition = -(tileSize * viewPortSize / 2);

                for (int j = 1; j < viewPortSize - 1; j++)
                {
                    worldPosition.x += tileSize;
                    XPosition += tileSize;

                    var currentTile = viewPort[i, j];
                    var surfaceId = currentTile.mapId;

                    // --- Build terrain from map
                    var ZPostition1 = currentTile.mapDepth;
                    var ZPostition2 = viewPort[i, j + 1].mapDepth;
                    var ZPostition3 = viewPort[i + 1, j + 1].mapDepth;
                    var ZPostition4 = viewPort[i + 1, j].mapDepth;

                    var color1 = GetTileColorGradient((ZPostition1 + ZPostition2) / 2, maxHeight);
                    var color2 = GetTileColorGradient((ZPostition1 + ZPostition2) / 2, maxHeight);

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
                            z = 90 + currentTile.mapDepth // Max map depth
                        };

                        var crashBoxCorners = _3dObjectHelpers.GenerateCrashBoxCorners(min, max);
                        viewPortCrashBoxes.Add(crashBoxCorners);
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
            //surface.CrashBoxes = new List<List<IVector3>>();
            surface.CrashBoxes.AddRange(GetMainSurfaceCrashBox());
            return surface;
        }


        private List<List<IVector3>> GetMainSurfaceCrashBox()
        {
            var min = new Vector3 { x = -400, y = 0, z = -400 };
            var max = new Vector3 { x = 400, y = 500, z = 100 };

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
            Global2DMap = SurfaceGeneration.ReturnPseudoRandomMap(globalMapSize, maxHeight: out maxHeight,maxTrees,maxHouses);
            GlobalMapBitmap = SurfaceGeneration.GenerateTerrainBitmapSource(Global2DMap, globalMapSize, maxHeight);
        }
    }
}
