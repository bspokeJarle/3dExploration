﻿using _3dTesting._3dWorld;
using Domain;
using System.Collections.Generic;
using static Domain._3dSpecificsImplementations;

namespace _3dRotations.World.Objects
{
    public static class House
    {
        private static float houseWidth = 30f;
        private static float houseDepth = 25f;
        private static float houseHeight = 22f;
        private static float roofHeight = 8f;
        private static float garageWidth = 12f;
        private static float garageDepth = 10f;
        private static float garageHeight = 8f;

        public static _3dObject CreateHouse(ISurface parentSurface)
        {
            var houseWalls = HouseWalls();
            var houseRoof = HouseRoof();
            var garageStructure = GarageStructure();
            var garageRoof = GarageRoof();
            var garageDoor = GarageDoor();
            var houseWindows = HouseWindows();
            var houseDoor = HouseDoor();
            var garageWindow = GarageWindow();
            var houseDetails = HouseDetails();
            var houseCrashBox = HouseCrashBoxes();

            var house = new _3dObject();

            if (houseWalls != null)
                house.ObjectParts.Add(new _3dObjectPart { PartName = "HouseWalls", Triangles = houseWalls, IsVisible = true });

            if (houseRoof != null)
                house.ObjectParts.Add(new _3dObjectPart { PartName = "HouseRoof", Triangles = houseRoof, IsVisible = true });

            if (garageStructure != null)
                house.ObjectParts.Add(new _3dObjectPart { PartName = "Garage", Triangles = garageStructure, IsVisible = true });

            if (garageRoof != null)
                house.ObjectParts.Add(new _3dObjectPart { PartName = "GarageRoof", Triangles = garageRoof, IsVisible = true });

            if (garageDoor != null)
                house.ObjectParts.Add(new _3dObjectPart { PartName = "GarageDoor", Triangles = garageDoor, IsVisible = true });

            if (houseWindows != null)
                house.ObjectParts.Add(new _3dObjectPart { PartName = "HouseWindows", Triangles = houseWindows, IsVisible = true });

            if (houseDoor != null)
                house.ObjectParts.Add(new _3dObjectPart { PartName = "HouseDoor", Triangles = houseDoor, IsVisible = true });

            if (garageWindow != null)
                house.ObjectParts.Add(new _3dObjectPart { PartName = "GarageWindow", Triangles = garageWindow, IsVisible = true });

            if (houseDetails != null)
                house.ObjectParts.Add(new _3dObjectPart { PartName = "HouseDetails", Triangles = houseDetails, IsVisible = true });

            house.Position = new Vector3 { };
            house.Rotation = new Vector3 { x = 0, y = 0, z = 0 };
            house.ParentSurface = parentSurface;
            if (houseCrashBox != null) house.CrashBoxes = houseCrashBox;

            return house;
        }

        public static List<ITriangleMeshWithColor>? HouseDetails()
        {
            var details = new List<ITriangleMeshWithColor>();
            string doorHandleColor = "D4A017"; // Gullfarge for dørhåndtak
            float handleSize = 1.2f;
            float handleOffset = 2.5f;

            // Dørhåndtak
            var h1 = new Vector3 { x = -handleSize / 2, y = -houseDepth / 2 - 1.2f, z = handleOffset };
            var h2 = new Vector3 { x = handleSize / 2, y = -houseDepth / 2 - 1.2f, z = handleOffset };
            var h3 = new Vector3 { x = handleSize / 2, y = -houseDepth / 2 - 1.2f, z = handleOffset + handleSize };
            var h4 = new Vector3 { x = -handleSize / 2, y = -houseDepth / 2 - 1.2f, z = handleOffset + handleSize };

            details.Add(new TriangleMeshWithColor { Color = doorHandleColor, vert1 = h1, vert2 = h2, vert3 = h3 });
            details.Add(new TriangleMeshWithColor { Color = doorHandleColor, vert1 = h1, vert2 = h3, vert3 = h4 });

            return details;
        }

        public static List<ITriangleMeshWithColor>? HouseWalls()
        {
            var walls = new List<ITriangleMeshWithColor>();
            string wallColor = "D9B382"; // Mer naturlig gulaktig for veggene

            var v1 = new Vector3 { x = -houseWidth / 2, y = -houseDepth / 2, z = 0 };
            var v2 = new Vector3 { x = houseWidth / 2, y = -houseDepth / 2, z = 0 };
            var v3 = new Vector3 { x = houseWidth / 2, y = houseDepth / 2, z = 0 };
            var v4 = new Vector3 { x = -houseWidth / 2, y = houseDepth / 2, z = 0 };
            var v5 = new Vector3 { x = -houseWidth / 2, y = -houseDepth / 2, z = houseHeight };
            var v6 = new Vector3 { x = houseWidth / 2, y = -houseDepth / 2, z = houseHeight };
            var v7 = new Vector3 { x = houseWidth / 2, y = houseDepth / 2, z = houseHeight };
            var v8 = new Vector3 { x = -houseWidth / 2, y = houseDepth / 2, z = houseHeight };

            walls.Add(new TriangleMeshWithColor { Color = wallColor, vert1 = v1, vert2 = v2, vert3 = v5 });
            walls.Add(new TriangleMeshWithColor { Color = wallColor, vert1 = v2, vert2 = v6, vert3 = v5 });
            walls.Add(new TriangleMeshWithColor { Color = wallColor, vert1 = v2, vert2 = v3, vert3 = v6 });
            walls.Add(new TriangleMeshWithColor { Color = wallColor, vert1 = v3, vert2 = v7, vert3 = v6 });
            walls.Add(new TriangleMeshWithColor { Color = wallColor, vert1 = v3, vert2 = v4, vert3 = v7 });
            walls.Add(new TriangleMeshWithColor { Color = wallColor, vert1 = v4, vert2 = v8, vert3 = v7 });
            walls.Add(new TriangleMeshWithColor { Color = wallColor, vert1 = v4, vert2 = v1, vert3 = v8 });
            walls.Add(new TriangleMeshWithColor { Color = wallColor, vert1 = v1, vert2 = v5, vert3 = v8 });

            return walls;
        }


        public static List<ITriangleMeshWithColor>? HouseWindows()
        {
            var windows = new List<ITriangleMeshWithColor>();
            string windowColor = "A0C4FF"; // Lys blå farge for vinduer
            float windowWidth = 6f;
            float windowHeight = 6f;
            float offset = -houseDepth / 2 - 1.5f;

            var w1 = new Vector3 { x = -10, y = offset, z = houseHeight / 2 };
            var w2 = new Vector3 { x = 10, y = offset, z = houseHeight / 2 };
            var w3 = new Vector3 { x = 10, y = offset, z = houseHeight / 2 + windowHeight };
            var w4 = new Vector3 { x = -10, y = offset, z = houseHeight / 2 + windowHeight };

            windows.Add(new TriangleMeshWithColor { Color = windowColor, vert1 = w1, vert2 = w2, vert3 = w3 });
            windows.Add(new TriangleMeshWithColor { Color = windowColor, vert1 = w1, vert2 = w3, vert3 = w4 });

            return windows;
        }

        public static List<ITriangleMeshWithColor>? HouseDoor()
        {
            var door = new List<ITriangleMeshWithColor>();
            string doorColor = "6D4C41"; // Mørk brun farge for døren
            float doorWidth = 8f;
            float doorHeight = 12f;
            float offset = -houseDepth / 2 - 1.5f;

            var d1 = new Vector3 { x = -4, y = offset, z = 0 };
            var d2 = new Vector3 { x = 4, y = offset, z = 0 };
            var d3 = new Vector3 { x = 4, y = offset, z = doorHeight };
            var d4 = new Vector3 { x = -4, y = offset, z = doorHeight };

            door.Add(new TriangleMeshWithColor { Color = doorColor, vert1 = d1, vert2 = d2, vert3 = d3 });
            door.Add(new TriangleMeshWithColor { Color = doorColor, vert1 = d1, vert2 = d3, vert3 = d4 });

            return door;
        }
        public static List<ITriangleMeshWithColor>? GarageWindow()
        {
            var window = new List<ITriangleMeshWithColor>();
            string windowColor = "0000FF"; // Blå farge for vinduet
            float windowWidth = 5f;
            float windowHeight = 5f;

            var w1 = new Vector3 { x = -houseWidth / 2 - garageWidth + 3, y = -garageDepth / 2 + 0.1f, z = garageHeight / 3 };
            var w2 = new Vector3 { x = -houseWidth / 2 - 3, y = -garageDepth / 2 + 0.1f, z = garageHeight / 3 };
            var w3 = new Vector3 { x = -houseWidth / 2 - 3, y = -garageDepth / 2 + 0.1f, z = garageHeight / 3 + windowHeight };
            var w4 = new Vector3 { x = -houseWidth / 2 - garageWidth + 3, y = -garageDepth / 2 + 0.1f, z = garageHeight / 3 + windowHeight };

            window.Add(new TriangleMeshWithColor { Color = windowColor, vert1 = w1, vert2 = w2, vert3 = w3 });
            window.Add(new TriangleMeshWithColor { Color = windowColor, vert1 = w1, vert2 = w3, vert3 = w4 });

            return window;
        }

        public static List<ITriangleMeshWithColor>? HouseRoof()
        {
            var roof = new List<ITriangleMeshWithColor>();
            string roofColor = "8B0000"; // Mørk rød for taket
            float roofOverhang = 3.0f; // Overheng i alle retninger

            var v1 = new Vector3 { x = -houseWidth / 2 - roofOverhang, y = -houseDepth / 2 - roofOverhang, z = houseHeight };
            var v2 = new Vector3 { x = houseWidth / 2 + roofOverhang, y = -houseDepth / 2 - roofOverhang, z = houseHeight };
            var v3 = new Vector3 { x = houseWidth / 2 + roofOverhang, y = houseDepth / 2 + roofOverhang, z = houseHeight };
            var v4 = new Vector3 { x = -houseWidth / 2 - roofOverhang, y = houseDepth / 2 + roofOverhang, z = houseHeight };
            var top = new Vector3 { x = 0, y = 0, z = houseHeight + roofHeight };

            roof.Add(new TriangleMeshWithColor { Color = roofColor, vert1 = v1, vert2 = v2, vert3 = top });
            roof.Add(new TriangleMeshWithColor { Color = roofColor, vert1 = v2, vert2 = v3, vert3 = top });
            roof.Add(new TriangleMeshWithColor { Color = roofColor, vert1 = v3, vert2 = v4, vert3 = top });
            roof.Add(new TriangleMeshWithColor { Color = roofColor, vert1 = v4, vert2 = v1, vert3 = top });

            return roof;
        }

        public static List<ITriangleMeshWithColor>? GarageRoof()
        {
            var roof = new List<ITriangleMeshWithColor>();
            string roofColor = "AA0000"; // Red for garage roof

            var v1 = new Vector3 { x = -houseWidth / 2 - garageWidth, y = -garageDepth / 2, z = garageHeight };
            var v2 = new Vector3 { x = -houseWidth / 2, y = -garageDepth / 2, z = garageHeight };
            var v3 = new Vector3 { x = -houseWidth / 2, y = garageDepth / 2, z = garageHeight };
            var v4 = new Vector3 { x = -houseWidth / 2 - garageWidth, y = garageDepth / 2, z = garageHeight };

            roof.Add(new TriangleMeshWithColor { Color = roofColor, vert1 = v1, vert2 = v2, vert3 = v3 });
            roof.Add(new TriangleMeshWithColor { Color = roofColor, vert1 = v3, vert2 = v4, vert3 = v1 });

            return roof;
        }

        public static List<ITriangleMeshWithColor>? GarageStructure()
        {
            var garage = new List<ITriangleMeshWithColor>();
            string garageColor = "B0A090"; // Neutral grayish tone for walls

            var v1 = new Vector3 { x = -houseWidth / 2 - garageWidth, y = -garageDepth / 2, z = 0 };
            var v2 = new Vector3 { x = -houseWidth / 2, y = -garageDepth / 2, z = 0 };
            var v3 = new Vector3 { x = -houseWidth / 2, y = garageDepth / 2, z = 0 };
            var v4 = new Vector3 { x = -houseWidth / 2 - garageWidth, y = garageDepth / 2, z = 0 };
            var v5 = new Vector3 { x = -houseWidth / 2 - garageWidth, y = -garageDepth / 2, z = garageHeight };
            var v6 = new Vector3 { x = -houseWidth / 2, y = -garageDepth / 2, z = garageHeight };
            var v7 = new Vector3 { x = -houseWidth / 2, y = garageDepth / 2, z = garageHeight };
            var v8 = new Vector3 { x = -houseWidth / 2 - garageWidth, y = garageDepth / 2, z = garageHeight };

            // Ensure proper triangle winding order (Right-Hand Rule)
            garage.Add(new TriangleMeshWithColor { Color = garageColor, vert1 = v1, vert2 = v2, vert3 = v6, noHidden = true });
            garage.Add(new TriangleMeshWithColor { Color = garageColor, vert1 = v1, vert2 = v6, vert3 = v5, noHidden = true });
            garage.Add(new TriangleMeshWithColor { Color = garageColor, vert1 = v2, vert2 = v3, vert3 = v7, noHidden = true });
            garage.Add(new TriangleMeshWithColor { Color = garageColor, vert1 = v2, vert2 = v7, vert3 = v6, noHidden = true });
            garage.Add(new TriangleMeshWithColor { Color = garageColor, vert1 = v3, vert2 = v4, vert3 = v8, noHidden = true });
            garage.Add(new TriangleMeshWithColor { Color = garageColor, vert1 = v3, vert2 = v8, vert3 = v7, noHidden = true });
            garage.Add(new TriangleMeshWithColor { Color = garageColor, vert1 = v4, vert2 = v1, vert3 = v5, noHidden = true });
            garage.Add(new TriangleMeshWithColor { Color = garageColor, vert1 = v4, vert2 = v5, vert3 = v8, noHidden = true });
            garage.Add(new TriangleMeshWithColor { Color = garageColor, vert1 = v5, vert2 = v6, vert3 = v7, noHidden = true });
            garage.Add(new TriangleMeshWithColor { Color = garageColor, vert1 = v5, vert2 = v7, vert3 = v8, noHidden = true });

            return garage;
        }

        public static List<ITriangleMeshWithColor>? GarageDoor()
        {
            var door = new List<ITriangleMeshWithColor>();
            string doorColor = "777777"; // Gray for garage door

            float doorInset = 2.5f;
            float doorHeight = garageHeight - 2.0f;
            float doorWidth = garageWidth - 6.0f; // Adjusted to fit garage structure

            var v1 = new Vector3 { x = -houseWidth / 2 - garageWidth + doorInset, y = -garageDepth / 2 + doorInset, z = 0 };
            var v2 = new Vector3 { x = -houseWidth / 2 - doorInset, y = -garageDepth / 2 + doorInset, z = 0 };
            var v3 = new Vector3 { x = -houseWidth / 2 - doorInset, y = garageDepth / 2 - doorInset, z = 0 };
            var v4 = new Vector3 { x = -houseWidth / 2 - garageWidth + doorInset, y = garageDepth / 2 - doorInset, z = 0 };
            var v5 = new Vector3 { x = -houseWidth / 2 - garageWidth + doorInset, y = -garageDepth / 2 + doorInset, z = doorHeight };
            var v6 = new Vector3 { x = -houseWidth / 2 - doorInset, y = -garageDepth / 2 + doorInset, z = doorHeight };
            var v7 = new Vector3 { x = -houseWidth / 2 - doorInset, y = garageDepth / 2 - doorInset, z = doorHeight };
            var v8 = new Vector3 { x = -houseWidth / 2 - garageWidth + doorInset, y = garageDepth / 2 - doorInset, z = doorHeight };

            door.Add(new TriangleMeshWithColor { Color = doorColor, vert1 = v1, vert2 = v2, vert3 = v6, noHidden = true });
            door.Add(new TriangleMeshWithColor { Color = doorColor, vert1 = v1, vert2 = v6, vert3 = v5, noHidden = true });
            door.Add(new TriangleMeshWithColor { Color = doorColor, vert1 = v2, vert2 = v3, vert3 = v7, noHidden = true });
            door.Add(new TriangleMeshWithColor { Color = doorColor, vert1 = v2, vert2 = v7, vert3 = v6, noHidden = true });
            door.Add(new TriangleMeshWithColor { Color = doorColor, vert1 = v3, vert2 = v4, vert3 = v8, noHidden = true });
            door.Add(new TriangleMeshWithColor { Color = doorColor, vert1 = v3, vert2 = v8, vert3 = v7, noHidden = true });

            return door;
        }

        public static List<List<IVector3>>? HouseCrashBoxes()
        {
            return new List<List<IVector3>>
            {
                new List<IVector3>
                {
                    new Vector3 { x = -houseWidth / 2, y = -houseDepth / 2, z = 0 },
                    new Vector3 { x = houseWidth / 2, y = houseDepth / 2, z = houseHeight + roofHeight }
                }
            };
        }
    }
}
