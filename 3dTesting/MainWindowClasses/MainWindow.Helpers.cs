﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using _3dTesting._3dRotation;
using _3dTesting._3dWorld;
using Domain;
using static Domain._3dSpecificsImplementations;
using System.Windows.Media.Imaging;



namespace _3dTesting.Helpers
{
    public static class GameHelpers
    {
        private static _3dRotate Rotate3d = new _3dRotate();

        /// <summary>
        /// Applies the rotation offset to prevent flipping when rotating.
        /// </summary>
        public static void ApplyRotationOffset(ref ITriangleMeshWithColor triangle, int? offsetX, int? offsetY, int? offsetZ)
        {
            if (offsetX > 0) { triangle.vert1.x += (float)offsetX; triangle.vert2.x += (float)offsetX; triangle.vert3.x += (float)offsetX; }
            if (offsetY > 0) { triangle.vert1.y += (float)offsetY; triangle.vert2.y += (float)offsetY; triangle.vert3.y += (float)offsetY; }
            if (offsetZ > 0) { triangle.vert1.z += (float)offsetZ; triangle.vert2.z += (float)offsetZ; triangle.vert3.z += (float)offsetZ; }
        }

        /// <summary>
        /// Updates the minimap overlay with the correct cropped portion.
        /// </summary>
        public static void UpdateMapOverlay(System.Windows.Controls.Image mapOverlay, BitmapSource surfaceMapBitmap, int mapX, int mapY)
        {
            if (surfaceMapBitmap != null && mapOverlay != null)
            {
                mapOverlay.Source = new CroppedBitmap(surfaceMapBitmap, new Int32Rect(mapX / 75, mapY / 75, 200, 200));
            }
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
            return _3dObjectHelpers.DeepCopy3dObjects(worldInhabitants);
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
