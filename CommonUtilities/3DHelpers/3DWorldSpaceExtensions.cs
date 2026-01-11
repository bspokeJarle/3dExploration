using Domain;
using System.Collections.Generic;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting.Helpers
{
    public static class CrashBoxWorldExtensions
    {
        /// <summary>
        /// Returns the effective world offset for crashboxes WITHOUT mutating anything.
        /// Includes:
        /// - CalculatedWorldOffset
        /// - ObjectOffsets
        /// - Surface Y offset (Surface only)
        /// </summary>
        public static Vector3 GetEffectiveCrashBoxOffset(this _3dObject obj)
        {
            var world = obj?.CalculatedWorldOffset ?? new Vector3(0, 0, 0);
            var offs = obj?.ObjectOffsets ?? new Vector3(0, 0, 0);

            float surfaceY = 0f;
            if (obj != null &&
                obj.ObjectName == "Surface" &&
                obj.ParentSurface?.GlobalMapPosition != null)
            {
                surfaceY = obj.ParentSurface.GlobalMapPosition.y;
            }

            return new Vector3
            {
                x = world.x + offs.x,
                y = world.y + offs.y + surfaceY,
                z = world.z + offs.z
            };
        }

        /// <summary>
        /// Converts ONE crashbox (local / rotated) to NEW world-space coordinates.
        /// No mutation of source data.
        /// </summary>
        public static List<IVector3> GetLocalWorldCoordinates(
            this List<IVector3> crashBox,
            _3dObject obj)
        {
            if (crashBox == null || crashBox.Count == 0)
                return new List<IVector3>();

            var offset = obj.GetEffectiveCrashBoxOffset();

            var result = new List<IVector3>(crashBox.Count);
            for (int i = 0; i < crashBox.Count; i++)
            {
                var v = crashBox[i];
                result.Add(new Vector3
                {
                    x = v.x + offset.x,
                    y = v.y + offset.y,
                    z = v.z + offset.z
                });
            }

            return result;
        }

        /// <summary>
        /// Converts ALL crashboxes to NEW world-space crashboxes.
        /// No mutation, safe to call anywhere in the pipeline.
        /// </summary>
        public static List<List<IVector3>> GetLocalWorldCoordinates(
            this List<List<IVector3>> crashBoxes,
            _3dObject obj)
        {
            if (crashBoxes == null || crashBoxes.Count == 0)
                return new List<List<IVector3>>();

            var result = new List<List<IVector3>>(crashBoxes.Count);
            for (int i = 0; i < crashBoxes.Count; i++)
            {
                result.Add(crashBoxes[i].GetLocalWorldCoordinates(obj));
            }

            return result;
        }

        /// <summary>
        /// Same as GetLocalWorldCoordinates, but returns Vector3 directly
        /// (useful for collision math / AABB checks).
        /// </summary>
        public static List<Vector3> GetLocalWorldCoordinatesV3(
            this List<IVector3> crashBox,
            _3dObject obj)
        {
            if (crashBox == null || crashBox.Count == 0)
                return new List<Vector3>();

            var offset = obj.GetEffectiveCrashBoxOffset();

            var result = new List<Vector3>(crashBox.Count);
            for (int i = 0; i < crashBox.Count; i++)
            {
                var v = crashBox[i];
                result.Add(new Vector3
                {
                    x = v.x + offset.x,
                    y = v.y + offset.y,
                    z = v.z + offset.z
                });
            }

            return result;
        }
    }
}
