using _3dTesting._3dRotation;
using _3dTesting._Coordinates;
using _3dTesting.Helpers;
using CommonUtilities._3DHelpers;
using Domain;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.MainWindowClasses
{
    public class GameWorldManager
    {
        private readonly _3dTo2d From3dTo2d = new();
        private readonly _3dRotate Rotate3d = new();
        private readonly ParticleManager particleManager = new();
        public string DebugMessage { get; set; }
        private bool enableLocalLogging = false;
        public bool FadeOutWorld { get; set; } = false;
        public bool FadeInWorld { get; set; } = false;

        public List<_2dTriangleMesh> UpdateWorld(_3dWorld._3dWorld world,ref List<_2dTriangleMesh> projectedCoordinates, ref List<_2dTriangleMesh> crashBoxCoordinates)
        {
            var activeWorld = Common3dObjectHelpers.DeepCopy3dObjects(world.WorldInhabitants.Cast<_3dObject>().ToList());
            var particleObjectList = new List<_3dObject>();
            var renderedList = new List<_3dObject>();
            DebugMessage = string.Empty;

            foreach (var inhabitant in activeWorld)
            {
                if (!inhabitant.CheckInhabitantVisibility()) continue;

                inhabitant.Movement?.MoveObject(inhabitant);
                inhabitant.CrashBoxes = RotateAllCrashboxes(inhabitant.CrashBoxes, (Vector3)inhabitant.Rotation);

                foreach (var part in inhabitant.ObjectParts)
                {
                    part.Triangles = RotateMesh(part.Triangles, (Vector3)inhabitant.Rotation);

                    // Store the RotatedSurfaceTriangles
                    if (inhabitant.ObjectName == "Surface")
                    {
                        inhabitant.ParentSurface.RotatedSurfaceTriangles = part.Triangles;

                        // Build fast lookup set
                        inhabitant.ParentSurface.LandBasedIds = [.. part.Triangles.Select(t => t.landBasedPosition)];
                    }

                    SetMovementGuides(inhabitant, part, part.Triangles);
                }

                if (inhabitant.ObjectName == "Surface")
                    DebugMessage += $" Surface: Y Pos: {inhabitant.ObjectOffsets.y}";

                if (inhabitant.ObjectName == "Ship")
                    DebugMessage += $" Ship: Y Pos: {inhabitant.ObjectOffsets.y + 300} Z Rotation: { inhabitant.Rotation.z }";

                particleManager.HandleParticles(inhabitant, particleObjectList);
                renderedList.Add(inhabitant);
            }

            if (particleObjectList.Count > 0)
            {
                renderedList.AddRange(particleObjectList);
                DebugMessage += $" Number of Particles on screen {particleObjectList.Count}";
            }

            var ship = activeWorld.FirstOrDefault(x => x.ObjectName == "Ship");
            if (ship != null && ship.ImpactStatus.ObjectHealth<=0 && !FadeOutWorld)
            {
                //When the ship health is 0, start the fade out effect (explosion will also be triggered)
                FadeOutWorld = true;
            }
            if (ship!=null && ship.ImpactStatus.HasExploded)
            {
                //When the ship has exploded, start the fade in effect, should take longer than fade out
                FadeOutWorld = false;
                FadeInWorld = true;
                //Dispose the ship movement to free resources
                ship.Movement.Dispose();
                world.WorldInhabitants.Clear();
                //When explosion has happened, reset the scene
                world.SceneHandler.ResetActiveScene(world);
                return [];
            }

            projectedCoordinates = From3dTo2d.ConvertTo2dFromObjects(renderedList, false);
            var crashBoxDebuggedObjects = renderedList.Where(x => x.CrashBoxDebugMode == true).ToList();
            //Check if there are any crashboxes to debug
            if (crashBoxDebuggedObjects.Count>0) crashBoxCoordinates = From3dTo2d.ConvertTo2dFromObjects(crashBoxDebuggedObjects, true);
            else crashBoxCoordinates = new List<_2dTriangleMesh>();
            CrashDetection.HandleCrashboxes(renderedList);
            return projectedCoordinates;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private List<List<IVector3>> RotateAllCrashboxes(List<List<IVector3>> crashboxes, Vector3 rotation)
        {
            var rotatedCrashboxes = new List<List<IVector3>>(crashboxes.Count);
            foreach (var crashbox in crashboxes)
            {
                var rotated = new List<IVector3>(crashbox.Count);
                foreach (var point in crashbox)
                {
                    rotated.Add(RotatePoint((Vector3)point, rotation));
                }
                rotatedCrashboxes.Add(rotated);
            }
            return rotatedCrashboxes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IVector3 RotatePoint(Vector3 point, Vector3 rotation)
        {
            var rotatedPoint = Rotate3d.RotatePoint(rotation.z, point, 'Z');
            rotatedPoint = Rotate3d.RotatePoint(rotation.y, rotatedPoint, 'Y');
            rotatedPoint = Rotate3d.RotatePoint(rotation.x, rotatedPoint, 'X');
            return rotatedPoint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private List<ITriangleMeshWithColor> RotateMesh(List<ITriangleMeshWithColor> mesh, Vector3 rotation)
        {
            var rotatedMesh = Rotate3d.RotateMesh(mesh, rotation.z, 'Z');
            rotatedMesh = Rotate3d.RotateMesh(rotatedMesh, rotation.y, 'Y');
            rotatedMesh = Rotate3d.RotateMesh(rotatedMesh, rotation.x, 'X');
            return rotatedMesh;
        }

        private void SetMovementGuides(_3dObject inhabitant, I3dObjectPart part, List<ITriangleMeshWithColor> rotatedMesh)
        {
            switch (part.PartName)
            {
                case "SeederParticlesStartGuide":
                case "JetMotor":
                    inhabitant.Movement.SetStartGuideCoordinates(rotatedMesh.First() as TriangleMeshWithColor, null);
                    break;
                case "SeederParticlesGuide":
                case "JetMotorDirectionGuide":
                    if (enableLocalLogging) Logger.Log($"MainLoop Set Guide after rotation: {rotatedMesh.First().vert1.x + ", " + rotatedMesh.First().vert1.y + ", " + rotatedMesh.First().vert1.z} Inhabitant:{inhabitant.ObjectName} ");
                    inhabitant.Movement.SetStartGuideCoordinates(null, rotatedMesh.First() as TriangleMeshWithColor);
                    break;
            }
        }
    }
}
