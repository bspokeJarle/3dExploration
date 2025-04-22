using _3dTesting._3dRotation;
using _3dTesting._3dWorld;
using _3dTesting._Coordinates;
using _3dTesting.Helpers;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.MainWindowClasses
{
    public class GameWorldManager
    {
        private readonly _3dTo2d From3dTo2d = new();
        private readonly _3dRotate Rotate3d = new();
        private readonly ParticleManager particleManager = new();
        public string DebugMessage { get; set; }

        public List<_2dTriangleMesh> UpdateWorld(_3dWorld._3dWorld world)
        {
            var activeWorld = _3dObjectHelpers.DeepCopy3dObjects(world.WorldInhabitants);
            var particleObjectList = new List<_3dObject>();
            var renderedList = new List<_3dObject>();
            DebugMessage = string.Empty;

            foreach (var inhabitant in activeWorld)
            {
                if (!inhabitant.CheckInhabitantVisibility()) continue;

                inhabitant.Movement?.MoveObject(inhabitant);

                bool hasRotationOffset = inhabitant.RotationOffsetX > 0 ||
                                         inhabitant.RotationOffsetY > 0 ||
                                         inhabitant.RotationOffsetZ > 0;

                if (hasRotationOffset)
                {
                    foreach (var part in inhabitant.ObjectParts)
                    {
                        for (int i = 0; i < part.Triangles.Count; i++)
                        {
                            var triangles = part.Triangles[i];
                            GameHelpers.ApplyRotationOffset(ref triangles,
                                inhabitant.RotationOffsetX,
                                inhabitant.RotationOffsetY,
                                inhabitant.RotationOffsetZ);
                        }
                    }
                }

                inhabitant.CrashBoxes = RotateAllCrashboxes(inhabitant.CrashBoxes, (Vector3)inhabitant.Rotation);

                foreach (var part in inhabitant.ObjectParts)
                {
                    part.Triangles = RotateMesh(part.Triangles, (Vector3)inhabitant.Rotation);
                    if (inhabitant.ObjectName == "Surface")
                        inhabitant.ParentSurface.RotatedSurfaceTriangles = part.Triangles;
                    SetMovementGuides(inhabitant, part, part.Triangles);
                }

                if (inhabitant.ObjectName == "Surface")
                    DebugMessage += $" Surface: Y Pos: {inhabitant.ObjectOffsets.y}";

                if (inhabitant.ObjectName == "Ship")
                    DebugMessage += $" Ship: Y Pos: {inhabitant.ObjectOffsets.y + 300} Z Rotation: { inhabitant.Rotation.z }";

                if (hasRotationOffset)
                {
                    Vector3 centroid = GameHelpers.ComputeCentroid(inhabitant);

                    foreach (var part in inhabitant.ObjectParts)
                    {
                        for (int i = 0; i < part.Triangles.Count; i++)
                        {
                            var triangles = part.Triangles[i];
                            GameHelpers.RecenterTriangle(ref triangles, centroid);
                        }
                    }
                }
                particleManager.HandleParticles(inhabitant, particleObjectList);
                renderedList.Add(inhabitant);
            }

            if (particleObjectList.Count > 0)
                renderedList.AddRange(particleObjectList);
             
            var projectedCoordinates = From3dTo2d.convertTo2dFromObjects(renderedList);
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
                    inhabitant.Movement.SetStartGuideCoordinates(null, rotatedMesh.First() as TriangleMeshWithColor);
                    break;
            }
        }
    }
}
