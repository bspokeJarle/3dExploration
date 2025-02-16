using _3dRotations.Helpers;
using _3dTesting._3dWorld;
using Domain;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media.Imaging;
using static Domain._3dSpecificsImplementations;


namespace _3dRotations.World.Objects
{
    public class Surface : ISurface
    {
        public Vector3 GlobalMapPosition { get; set; } = new Vector3 { x = 30000, y = 0, z = 30000 };
        public Vector3 GlobalMapRotation { get; set; } = new Vector3 { x = 0, y = 0, z = 0 };
        public int[,]? Global2DMap { get; set; } = new int[globalMapSize, globalMapSize];
        public BitmapSource? GlobalMapBitmap { get; set; }

        const int surfaceWidth = 1275;
        const int globalMapSize = 2500;
        const int viewPortSize = surfaceWidth / tileSize;
        public const int tileSize = 75;
        public int maxHeight = 75;

        public I3dObject GetSurfaceViewPort()
        {
            // TODO: only return surface that is visible in the viewport
            var newSurface = new List<ITriangleMeshWithColor>();
            var surface = new _3dObject();
            var viewPort = SurfaceGeneration.Return2DViewPort(viewPortSize, (int)GlobalMapPosition.x, (int)GlobalMapPosition.z, Global2DMap, tileSize);
            var ZRemainer = GlobalMapPosition.z % tileSize;
            var XRemainer = GlobalMapPosition.x % tileSize;
            var YRemainer = GlobalMapPosition.y;

            var YPosition = -(tileSize * viewPortSize / 2);
            for (int i = 1; i < viewPortSize + 2; i++)
            {
                YPosition += tileSize;
                var XPosition = -(tileSize * viewPortSize / 2);
                //Iterate through the map on x axis
                for (int j = 1; j < viewPortSize - 1; j++)
                {
                    XPosition += tileSize;
                    //Setup the coordinates for the square
                    var XPosition2 = XPosition + tileSize;
                    var YPosition2 = YPosition + tileSize;
                    var ZPostition1 = viewPort[i, j];
                    var ZPostition2 = viewPort[i, j + 1];
                    var ZPostition3 = viewPort[i + 1, j + 1];
                    var ZPostition4 = viewPort[i + 1, j];

                    string color1;
                    string color2;

                    //Get the color gradient
                    color1 = GetTileColorGradient(ZPostition1, maxHeight);
                    color2 = GetTileColorGradient(ZPostition2, maxHeight);

                    //Make a square, all squares are made of two triangles and hinged in the left hand upper corner
                    var triangle1 = new TriangleMeshWithColor { Color = color1, vert1 = { x = XPosition-XRemainer, y = YPosition-ZRemainer, z = ZPostition1-YRemainer }, vert2 = { x = XPosition2-XRemainer, y = YPosition-ZRemainer, z = ZPostition2 - YRemainer }, vert3 = { x = XPosition2-XRemainer, y = YPosition2-ZRemainer, z = ZPostition3 - YRemainer } };
                    var triangle2 = new TriangleMeshWithColor { Color = color2, vert1 = { x = XPosition-XRemainer, y = YPosition-ZRemainer, z = ZPostition1 - YRemainer }, vert2 = { x = XPosition2-XRemainer, y = YPosition2-ZRemainer, z = ZPostition3 - YRemainer }, vert3 = { x = XPosition-XRemainer, y = YPosition2-ZRemainer, z = ZPostition4 - YRemainer } };
                    //Add the square to the map
                    newSurface.Add(triangle1);
                    newSurface.Add(triangle2);
                }
            }
            surface.ObjectParts.Add(new _3dObjectPart { PartName = "Surface", Triangles = newSurface, IsVisible = true });            
            return surface;
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



        public void Create2DMap()
        {
            //Gets the pseudo random map in 2d
            Global2DMap = SurfaceGeneration.ReturnPseudoRandomMap(globalMapSize, maxHeight: out maxHeight);
            GlobalMapBitmap = SurfaceGeneration.GenerateTerrainBitmapSource(Global2DMap, globalMapSize, maxHeight);
        }
    }
}
