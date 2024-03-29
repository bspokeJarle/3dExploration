﻿using Domain;
using System;
using System.Collections.Generic;
using static Domain._3dSpecificsImplementations;

namespace _3dRotations.Helpers
{
    public static class SurfaceGeneration
    {
        const int mapSize = 25;
        const int tileSize = 50;
        const int zFactor = 5;
        public static int[,]? surfaceValues = new int[mapSize, mapSize];
        public static List<ITriangleMeshWithColor> Generate()
        {
            //todo: Improvements to be made:
            //todo1: Too many mountains, surface should have more flat areas, look at neighbours when randomizing the area so height progression is more even
            //todo2: Add more colors to the surface, make it look more like a real map
            var random = new Random();
            var newSurface = new List<ITriangleMeshWithColor>();
            //First, generate a random map
            for (int i = 0; i < mapSize; i++)
            {
                for (int j = 0; j < mapSize; j++)
                {
                    surfaceValues[i, j] = random.Next(0, 10);
                }
            }

            //Ierate 10 times to smooth the map
            for (int l = 0; l < 20; l++)
            {
                //Iterate through the map on y axis
                for (int i = 1; i < mapSize - 1; i++)
                {
                    //Iterate through the map on x axis
                    for (int j = 1; j < mapSize - 1; j++)
                    {
                        var numberOfNeighbours = 0;
                        if (surfaceValues[i - 1, -1 + j] != 0) numberOfNeighbours++;
                        if (surfaceValues[i - 1, j] != 0) numberOfNeighbours++;
                        if (surfaceValues[i - 1, j + 1] != 0) numberOfNeighbours++;
                        if (surfaceValues[i, j - 1] != 0) numberOfNeighbours++;
                        if (surfaceValues[i, j + 1] != 0) numberOfNeighbours++;
                        if (surfaceValues[i + 1, -1 + j] != 0) numberOfNeighbours++;
                        if (surfaceValues[i + 1, j] != 0) numberOfNeighbours++;
                        if (surfaceValues[i + 1, j + 1] != 0) numberOfNeighbours++;
                        if (numberOfNeighbours == 0)
                        {
                            surfaceValues[i, j] = (random.Next(1, 9) * zFactor);
                        }
                        else if (numberOfNeighbours >= 1 && numberOfNeighbours <= 5) surfaceValues[i, j] = 0;
                        else if (numberOfNeighbours > 5) surfaceValues[i, j] = (random.Next(1, 9) * zFactor);
                    }
                }
            }
            //Return the surface as a list of triangles
            var YPosition = -(tileSize * mapSize / 2);
            for (int i = 1; i < mapSize - 1; i++)
            {
                YPosition += tileSize;
                var XPosition = -(tileSize * mapSize / 2);
                //Iterate through the map on x axis
                for (int j = 1; j < mapSize - 1; j++)
                {
                    XPosition += tileSize;
                    //Setup the coordinates for the square
                    var XPosition2 = XPosition + tileSize;
                    var YPosition2 = YPosition + tileSize;
                    var ZPostition1 = surfaceValues[i, j];
                    var ZPostition2 = surfaceValues[i, j + 1];
                    var ZPostition3 = surfaceValues[i + 1, j + 1];
                    var ZPostition4 = surfaceValues[i + 1, j];

                    //Make a square, all squares are made of two triangles and hinged in the left hand upper corner
                    var color1 = "007700";
                    var color2 = "007700";

                    var accZ = ZPostition1 + ZPostition2 + ZPostition3 + ZPostition4;
                    //TODO: temporary solution, make a better color scheme
                    if (accZ == 0 || accZ < 5) color1 = "0000ff";
                    if (accZ == 0 || accZ < 5) color2 = "0000ff";
                    if (ZPostition1 > 5 && ZPostition1 < 10) color1 = "004400";
                    if (ZPostition2 > 5 && ZPostition2 < 10) color2 = "004400";
                    if (ZPostition1 > 10 && ZPostition1 < 15) color1 = "009900";
                    if (ZPostition2 > 10 && ZPostition2 < 15) color2 = "009900";
                    if (ZPostition1 > 15 && ZPostition1 < 20) color1 = "00BB00";
                    if (ZPostition2 > 15 && ZPostition2 < 20) color2 = "00BB00";
                    if (ZPostition1 > 20 && ZPostition1 < 25) color1 = "993300";
                    if (ZPostition2 > 20 && ZPostition2 < 25) color2 = "993300";
                    if (ZPostition1 > 25) color1 = "552200";
                    if (ZPostition2 > 25) color2 = "552200";


                    var triangle1 = new TriangleMeshWithColor { Color = color1, vert1 = { x = XPosition, y = YPosition, z = ZPostition1 }, vert2 = { x = XPosition2, y = YPosition, z = ZPostition2 }, vert3 = { x = XPosition2, y = YPosition2, z = ZPostition3 } };
                    var triangle2 = new TriangleMeshWithColor { Color = color2, vert1 = { x = XPosition, y = YPosition, z = ZPostition1 }, vert2 = { x = XPosition2, y = YPosition2, z = ZPostition3 }, vert3 = { x = XPosition, y = YPosition2, z = ZPostition4 } };
                    //Add the square to the map
                    newSurface.Add(triangle1);
                    newSurface.Add(triangle2);
                }
            }
            return newSurface;
        }
    }
}
