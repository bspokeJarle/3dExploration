using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using _3dTesting._3dRotation;
using Domain;
using static Domain._3dSpecificsImplementations;
using System.Windows.Media.Imaging;
using CommonUtilities._3DHelpers;

namespace _3dTesting.Helpers
{
    public static class GameHelpers
    {
        private static _3dRotationCommon Rotate3d = new _3dRotationCommon();

        /// <summary>
        /// Applies the rotation offset to prevent flipping when rotating.
        /// </summary>
        public static void ApplyRotationOffset(ref ITriangleMeshWithColor triangle, int? offsetX, int? offsetY, int? offsetZ)
        {
            if (offsetX > 0) { triangle.vert1.x += (float)offsetX; triangle.vert2.x += (float)offsetX; triangle.vert3.x += (float)offsetX; }
            if (offsetY > 0) { triangle.vert1.y += (float)offsetY; triangle.vert2.y += (float)offsetY; triangle.vert3.y += (float)offsetY; }
            if (offsetZ > 0) { triangle.vert1.z += (float)offsetZ; triangle.vert2.z += (float)offsetZ; triangle.vert3.z += (float)offsetZ; }
        }

        public static Vector3 ComputeCentroid(_3dObject inhabitant)
        {
            if (inhabitant.ObjectParts == null || inhabitant.ObjectParts.Count == 0)
                return new Vector3(0, 0, 0);

            float sumX = 0, sumY = 0, sumZ = 0;
            int totalVertices = 0;

            foreach (var part in inhabitant.ObjectParts)
            {
                foreach (var triangle in part.Triangles)
                {
                    sumX += triangle.vert1.x + triangle.vert2.x + triangle.vert3.x;
                    sumY += triangle.vert1.y + triangle.vert2.y + triangle.vert3.y;
                    sumZ += triangle.vert1.z + triangle.vert2.z + triangle.vert3.z;
                    totalVertices += 3;
                }
            }

            if (totalVertices == 0) return new Vector3(0, 0, 0);

            return new Vector3(sumX / totalVertices, sumY / totalVertices, sumZ / totalVertices);
        }

        /// <summary>
        /// Updates the minimap overlay with the correct cropped portion.
        /// </summary>
        public static void UpdateMapOverlay(System.Windows.Controls.Image mapOverlay, BitmapSource surfaceMapBitmap, int mapX, int mapY)
        {
            if (mapX == 0 || mapY == 0) return; // Avoid division by zero or invalid map coordinates 
            if (surfaceMapBitmap != null && mapOverlay != null)
            {
                //TODO: Get values from the setup later, hardcoded for now
                mapOverlay.Source = new CroppedBitmap(surfaceMapBitmap, new Int32Rect((mapX - 2000) / 75, (mapY - 2000) / 75, 72, 72));
            }
        }

        public static void UpdateShipStatistics(System.Windows.Shapes.Rectangle healthRectangle, _3dObject ship)
        {  
            if (ship==null||ship.ImpactStatus==null) return;
            if (ship.ImpactStatus.ObjectHealth > 0)
            {
                healthRectangle.Width = ship.ImpactStatus.ObjectHealth.Value * 2;
            }
            else healthRectangle.Width = 1;
        }


        /// <summary>
        /// Converts a standard System.Drawing.Bitmap to a WPF BitmapSource.
        /// </summary>
        public static BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }

        /// <summary>
        /// Rotates a mesh in all three axes.
        /// </summary>
        public static List<ITriangleMeshWithColor> RotateMesh(List<ITriangleMeshWithColor> mesh, Vector3 rotation)
        {
            var rotatedMesh = Rotate3d.RotateZMesh(mesh, rotation.z);
            rotatedMesh = Rotate3d.RotateYMesh(rotatedMesh, rotation.y);
            rotatedMesh = Rotate3d.RotateXMesh(rotatedMesh, rotation.x);
            return rotatedMesh;
        }

        /// <summary>
        /// Creates a deep copy of all objects in the world.
        /// </summary>
        public static List<_3dObject> DeepCopyObjects(List<_3dObject> worldInhabitants)
        {
            return Common3dObjectHelpers.DeepCopy3dObjects(worldInhabitants);
        }

        /// <summary>
        /// Checks if two crashboxes are colliding.
        /// </summary>
        public static bool CheckCollision(List<IVector3> crashboxA, List<IVector3> crashboxB)
        {
            return _3dObjectHelpers.CheckCollisionBoxVsBox(
                crashboxA.Select(v => new Vector3 { x = v.x, y = v.y, z = v.z }).ToList(),
                crashboxB.Select(v => new Vector3 { x = v.x, y = v.y, z = v.z }).ToList());
        }
    }
}
