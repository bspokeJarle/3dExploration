
using _3dTesting._3dWorld;
using _3dRotations.World.Objects;
using static Domain._3dSpecificsImplementations;
using GameAiAndControls.Controls;
using _3dRotations.Helpers;
using System.Diagnostics;
using Domain;
using System.Collections.Generic;

namespace _3dRotations.Scene.Scene1
{
    public class Scene1
    {
        Surface Surface = new();
        public void SetupScene1(_3dWorld world)
        {            
            //Add ship as first inhabitant
            var ship = Ship.CreateShip(Surface);
            //Generate 2D map for the surface, maxtrees and maxhouses set
            Surface.Create2DMap(500,50);

            //Test if this makes the ship tilt and rotate like it should
            ship.RotationOffsetY = 65;
            ship.Rotation = new Vector3 { };
            ship.WorldPosition = new Vector3 { };
            ship.Position = new Vector3 { };
            ship.ObjectName = "Ship";
            world.WorldInhabitants.Add(ship);

            //Add three seeders
            var seeder = Seeder.CreateSeeder(Surface);
            //Initialize the seeder rotation
            seeder.Rotation = new Vector3 { };
            seeder.WorldPosition = new Vector3 { x = 96000, y = 0, z = 96000 };
            seeder.Position = new Vector3 { x = -150, y = -200, z = 0 };
            seeder.ObjectName = "Seeder";
            seeder.Movement = new SeederControls();            
            world.WorldInhabitants.Add(seeder);

            var seeder2 = Seeder.CreateSeeder(Surface);
            //Initialize the seeder rotation
            seeder2.Rotation = new Vector3 { };
            seeder2.WorldPosition = new Vector3 { x = 93750, y = 0, z = 93000 };
            seeder2.Position = new Vector3 { x = -150, y = -100, z = 0 };
            seeder2.ObjectName = "Seeder";
            seeder2.Movement = new SeederControls();
            world.WorldInhabitants.Add(seeder2);

            var seeder3 = Seeder.CreateSeeder(Surface);
            //Initialize the seeder rotation
            seeder3.Rotation = new Vector3 { };
            seeder3.WorldPosition = new Vector3 { x = 94000, y = 0, z = 90000 };
            seeder3.Position = new Vector3 { x = -150, y = -100, z = 0 };
            seeder3.ObjectName = "Seeder";
            seeder3.Movement = new SeederControls();
            world.WorldInhabitants.Add(seeder3);

            var treePlacements = SurfaceGeneration.FindTreePlacementAreas(Surface.Global2DMap,Surface.GlobalMapSize(),Surface.TileSize(),Surface.MaxHeight());
            foreach (var treePlacement in treePlacements)
            {
                //Debug.WriteLine($"Tree placement: {treePlacement.x} {treePlacement.y}");
                //TODO: We need design more trees
                var tree = Tree.CreateTree(Surface);
                //The tree don't need a world position, it's a surface based object
                tree.WorldPosition = new Vector3 { x = 0, y = 0, z = 0 };
                tree.SurfaceBasedId = Surface.Global2DMap[treePlacement.y, treePlacement.x].mapId;
                //The offsets of landbased objects need to similar to that of the surface, apart from some fine tuning
                tree.Position = new Vector3 { x = 75, y = 425 , z = 300 };
                tree.ObjectName = "Tree";
                tree.Movement = new TreeControls();
                if (tree.SurfaceBasedId>0) world.WorldInhabitants.Add(tree);
            }

            var housePlacements = SurfaceGeneration.FindHousePlacementAreas(Surface.Global2DMap, Surface.GlobalMapSize(), Surface.MaxHeight(), treePlacements);
            foreach (var housePlacement in housePlacements)
            {
                //Debug.WriteLine($"House placement: {housePlacement.x} {housePlacement.y}");

                var house = House.CreateHouse(Surface);
                //Initialize the house rotation
                //The tree don't need a world position, it's a surface based object
                house.WorldPosition = new Vector3 { x = 0, y = 0, z = 0 };
                //Find the surface based id for the tree
                //TODO: Find a good place in the map for the tree
                //Alogrithm to find a good place for the trees and spread them around
                //Temp fix now to get the tree on the surface
                house.SurfaceBasedId = Surface.Global2DMap[housePlacement.y, housePlacement.x].mapId;
                house.Position = new Vector3 { x = 75, y = 450, z = 300 };
                house.ObjectName = "House";
                house.Movement = new HouseControls();
                if (house.SurfaceBasedId>0) world.WorldInhabitants.Add(house);
            }

            //Get the surface viewport based on the global Map Position
            var surfaceObject = (_3dObject)Surface.GetSurfaceViewPort();
            surfaceObject.ObjectName = "Surface";
            //This position and rotation is for the onscreen object, not the map position
            surfaceObject.Position = new Vector3 { x = 75, y = 500, z = 300 };
            surfaceObject.Rotation = new Vector3 { x = 70, y = 0, z = 0 };
            surfaceObject.WorldPosition = new Vector3 { };
            //Add crashboxes to the surface, add more crashboxes to the surface for large objects (Mountains etc)
            surfaceObject.CrashBoxes = new List<List<IVector3>>
            {
                new List<IVector3>
                {
                    new Vector3 { x = -750, y = -200, z = -750 },  // Mer negativ Y = strekker seg under bakken
                    new Vector3 { x = 750, y = 800, z = 750 }      // Høyere topp og større XZ-spenn
                }
            };
            surfaceObject.Movement = new GroundControls();
            surfaceObject.ParentSurface = Surface;
            world.WorldInhabitants.Add(surfaceObject);
        }
    }
}
