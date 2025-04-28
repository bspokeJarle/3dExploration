using _3dRotations.Helpers;
using _3dTesting._3dWorld;
using Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
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

                    // --- Nytt: CrashBox-sjekk ---
                    var crashBox = TryCreateCrashBoxFromSurfaceData(viewPort, i, j, tileSize);
                    if (crashBox != null)
                    {
                        viewPortCrashBoxes.Add(crashBox);
                    }

                    // --- Eksisterende logikk: bygge terreng (ikke endret) ---
                    var ZPostition1 = currentTile.mapDepth;
                    var ZPostition2 = viewPort[i, j + 1].mapDepth;
                    var ZPostition3 = viewPort[i + 1, j + 1].mapDepth;
                    var ZPostition4 = viewPort[i + 1, j].mapDepth;

                    var color1 = GetTileColorGradient((ZPostition1 + ZPostition2) / 2, maxHeight);
                    var color2 = GetTileColorGradient((ZPostition1 + ZPostition2) / 2, maxHeight);

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
            surface.CrashBoxes.AddRange(GetMainSurfaceCrashBox()); // Legg til hoved-crashboxen
            return surface;
        }

        private List<List<IVector3>> GetMainSurfaceCrashBox()
        {
            return new List<List<IVector3>>
            {
                new List<IVector3>
                {
                    new Vector3 { x = -750, y = 310, z = -1500 },  // Need a bit of thickness to the surface
                    new Vector3 { x = 750, y = 900, z = 1500 }      // We need a pretty big box for the surface
                }
            };
        }

        private List<IVector3>? TryCreateCrashBoxFromSurfaceData(
           SurfaceData[,] viewPort,
           int i, int j,
           int tileSize)
        {
            var currentTile = viewPort[i, j];

            if (currentTile.crashBox == null)
                return null;

            var crashInfo = currentTile.crashBox.Value;

            int maxI = viewPort.GetLength(0);
            int maxJ = viewPort.GetLength(1);

            int width = crashInfo.width;
            int height = crashInfo.height;

            // Get the real world position from the triangle
            // We simulate what triangle.vert1.x and triangle.vert1.z would be

            int tilesHalf = viewPort.GetLength(0) / 2;

            var minX = (j - tilesHalf) * tileSize;  // Centering around (0,0)
            var minZ = (i - tilesHalf) * tileSize;

            var maxX = minX + width * tileSize;
            var maxZ = minZ + height * tileSize;

            // 🔥 Now calculate real min/max height
            int maxHeightFound = int.MinValue;

            for (int zi = i; zi < Math.Min(i + height, maxI); zi++)
            {
                for (int xj = j; xj < Math.Min(j + width, maxJ); xj++)
                {
                    int h = viewPort[zi, xj].mapDepth;
                    if (h > maxHeightFound) maxHeightFound = h;
                }
            }

            const int seaLevel = 0; // or adjust if needed
            // Add small padding
            const int paddingXZ = 5;
            const int paddingY = 10;

            // Expand min and max points slightly
            var min = new Vector3
            {
                x = minX - paddingXZ,
                y = seaLevel,
                z = minZ - paddingXZ
            };

            var max = new Vector3
            {
                x = maxX + paddingXZ,
                y = maxHeightFound + paddingY,
                z = maxZ + paddingXZ
            };
            return new List<IVector3> { min, max };
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
