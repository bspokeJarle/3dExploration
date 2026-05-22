using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonSetup;
using Domain;
using System.Runtime.CompilerServices;
using static Domain._3dSpecificsImplementations;


namespace CommonUtilities._3DHelpers
{
    public static class Common3dObjectHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3? CopyVector(IVector3? vector)
        {
            if (vector == null)
            {
                return null;
            }

            return new Vector3(vector.x, vector.y, vector.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3 CopyRequiredVector(IVector3 vector)
        {
            return new Vector3(vector.x, vector.y, vector.z);
        }

        // -----------------------------------------------------------------
        //  HEADING HELPERS
        //  Shared heading logic for pointing objects toward a target.
        //  Z rotation is applied first to geometry (+X forward).
        //  After Z rotation by θ: tip at (cosθ, sinθ, 0).
        //  After X=70 camera tilt: screenX ∝ cosθ, screenY ∝ sinθ.
        //  Z=0→right, Z=90→down, Z=180→left, Z=270→up.
        //  World-to-screen: screen right = world +X, screen down = world +Z.
        //  Therefore heading Z = atan2(dz, dx).
        // -----------------------------------------------------------------

        /// <summary>
        /// Computes the Z rotation (heading) that points an object's +X forward
        /// along the given world-space XZ direction. Base X rotation = 70 (camera tilt).
        /// Returns (Xrotation, Yrotation, Zrotation).
        /// </summary>
        public static (float X, float Y, float Z) GetHeadingFromDirection(float dx, float dz)
        {
            float len = MathF.Sqrt(dx * dx + dz * dz);
            if (len < 1e-4f)
                return (70f, 0f, 0f);

            float headingDeg = MathF.Atan2(dz, dx) * (180f / MathF.PI);
            return (70f, 0f, headingDeg);
        }

        /// <summary>
        /// Computes the heading rotation to point from a source position toward
        /// a target position in the world XZ plane.
        /// Returns (Xrotation, Yrotation, Zrotation).
        /// </summary>
        public static (float X, float Y, float Z) GetHeadingToTarget(IVector3 source, IVector3 target)
        {
            return GetHeadingFromDirection(target.x - source.x, target.z - source.z);
        }

        /// <summary>
        /// Normalizes an angle to the range (-180, 180].
        /// </summary>
        public static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;
            return angle;
        }

        /// <summary>
        /// Smoothly moves a current angle toward a target angle by at most maxDelta degrees.
        /// </summary>
        public static float MoveAngleTowards(float current, float target, float maxDelta)
        {
            float delta = NormalizeAngle(target - current);
            if (MathF.Abs(delta) <= maxDelta)
                return current + delta;
            return current + MathF.Sign(delta) * maxDelta;
        }

        public static float DotNormalized(IVector3 a, IVector3 b)
        {
            //Returns 1.0 if the vectors are perfectly aligned
            float magA = (float)Math.Sqrt(a.x * a.x + a.y * a.y + a.z * a.z);
            float magB = (float)Math.Sqrt(b.x * b.x + b.y * b.y + b.z * b.z);

            if (magA < 1e-6f || magB < 1e-6f)
                return 0f;

            return (a.x * b.x + a.y * b.y + a.z * b.z) / (magA * magB);
        }
        public static IVector3 GetLocalWorldPosition(this _3dObject inhabitant)
        {
            var globalMapPosition = GameState.SurfaceState.GlobalMapPosition;
            //Some objects will always be in location, they have no world position, just return
            if (inhabitant.WorldPosition.x == 0 && inhabitant.WorldPosition.y == 0 && inhabitant.WorldPosition.z == 0) return null;
            //Some objects fly around, they have this world position, so they appear when you are at that location in the map
            var localWorldPosition = new Vector3
            {
                x = globalMapPosition.x - inhabitant.WorldPosition.x,
                y = globalMapPosition.y - inhabitant.WorldPosition.y,
                z = globalMapPosition.z - inhabitant.WorldPosition.z
            };
            return localWorldPosition;
        }

        public static Vector3 GetAudioPosition(this _3dObject inhabitant)
        {
            var objectOffsets = CopyVector(inhabitant?.ObjectOffsets) ?? new Vector3();
            var localWorldPosition = inhabitant?.GetLocalWorldPosition();

            if (localWorldPosition == null)
            {
                return objectOffsets;
            }

            return new Vector3
            {
                x = -localWorldPosition.x + objectOffsets.x,
                y = -localWorldPosition.y + objectOffsets.y,
                z = localWorldPosition.z + objectOffsets.z
            };
        }
        public static bool CheckInhabitantVisibility(this _3dObject inhabitant)
        {
            // 1. Land-based check
            if (inhabitant.SurfaceBasedId > 0 && inhabitant.ParentSurface?.LandBasedIds != null)
            {
                return inhabitant.ParentSurface.LandBasedIds.Contains(inhabitant.SurfaceBasedId);
            }

            // 2. Always-visible (onscreen) objects — world position (0, 0, 0)
            if (inhabitant.WorldPosition.x == 0 &&
                inhabitant.WorldPosition.y == 0 &&
                inhabitant.WorldPosition.z == 0)
            {
                return true;
            }

            // 3. Distance-based visibility check
            var globalMapPosition = GameState.SurfaceState.GlobalMapPosition;
            var inhabitantPosition = inhabitant.WorldPosition;

            const float maxDistance = ScreenSetup.ObjectVisibilityDistance;
            float maxDistanceSq = maxDistance * maxDistance;
            float distanceSq = GetDistanceSquared(globalMapPosition, inhabitantPosition);

            return distanceSq <= maxDistanceSq;
        }


        public static double GetDistance(Vector3 point1, Vector3 point2)
        {
            float dx = point1.x - point2.x;
            float dy = point1.y - point2.y;
            float dz = point1.z - point2.z;

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public static float GetDistanceSquared(IVector3 point1, IVector3 point2)
        {
            float dx = point1.x - point2.x;
            float dy = point1.y - point2.y;
            float dz = point1.z - point2.z;

            return dx * dx + dy * dy + dz * dz;
        }

        public struct CosSin
        {
            public float CosRes { get; set; }
            public float SinRes { get; set; }
        }
        public static CosSin ConvertFromAngleToCosSin(this float angle)
        {
            var radian = Math.PI * angle / 180.0;
            var sinRes = Math.Sin(radian);
            var cosRes = Math.Cos(radian);
            return new CosSin { CosRes = (float)cosRes, SinRes = (float)sinRes };
        }

        public static List<ITriangleMeshWithColor> ConvertToTrianglesWithColor(List<TriangleMesh> triangles, string color)
        {
            var triangleswithcolor = new List<ITriangleMeshWithColor>();
            foreach (var triangle in triangles)
            {
                triangleswithcolor.Add(new TriangleMeshWithColor
                {
                    vert1 = new Vector3 { x = triangle.vert1.x, y = triangle.vert1.y, z = triangle.vert1.z },
                    vert2 = new Vector3 { x = triangle.vert2.x, y = triangle.vert2.y, z = triangle.vert2.z },
                    vert3 = new Vector3 { x = triangle.vert3.x, y = triangle.vert3.y, z = triangle.vert3.z },
                    normal1 = new Vector3 { x = triangle.normal1.x, y = triangle.normal1.y, z = triangle.normal1.z },
                    normal2 = new Vector3 { x = triangle.normal2.x, y = triangle.normal2.y, z = triangle.normal2.z },
                    normal3 = new Vector3 { x = triangle.normal3.x, y = triangle.normal3.y, z = triangle.normal3.z },
                    angle = triangle.angle,
                    Color = color
                });
            }
            return triangleswithcolor;
        }

        public static List<TriangleMeshWithColor> DeepCopyTriangles(List<TriangleMeshWithColor> originalList)
        {
            List<TriangleMeshWithColor> copiedList = new List<TriangleMeshWithColor>();

            foreach (var original in originalList)
            {
                var copy = new TriangleMeshWithColor
                {
                    Color = original.Color,
                    normal1 = new Vector3 { x = original.normal1.x, y = original.normal1.y, z = original.normal1.z },
                    normal2 = new Vector3 { x = original.normal2.x, y = original.normal2.y, z = original.normal2.z },
                    normal3 = new Vector3 { x = original.normal3.x, y = original.normal3.y, z = original.normal3.z },
                    vert1 = new Vector3 { x = original.vert1.x, y = original.vert1.y, z = original.vert1.z },
                    vert2 = new Vector3 { x = original.vert2.x, y = original.vert2.y, z = original.vert2.z },
                    vert3 = new Vector3 { x = original.vert3.x, y = original.vert3.y, z = original.vert3.z },
                    landBasedPosition = original.landBasedPosition,
                    angle = original.angle,
                    noHidden = original.noHidden
                };

                copiedList.Add(copy);
            }

            return copiedList;
        }

        public static List<_3dObject> DeepCopy3dObjects(List<_3dObject> inhabitants)
        {
            var result = new List<_3dObject>(inhabitants.Count);
            DeepCopy3dObjects(inhabitants, result);
            return result;
        }

        public static void DeepCopy3dObjects(List<_3dObject> inhabitants, List<_3dObject> result)
        {
            result.Clear();

            if (result.Capacity < inhabitants.Count)
                result.Capacity = inhabitants.Count;

            for (int i = 0; i < inhabitants.Count; i++)
            {
                result.Add(DeepCopyObject(inhabitants[i], copyCrashboxes: true));
            }
        }

        public static I3dObject DeepCopySingleObject(I3dObject original)
        {
            return original is _3dObject concrete
                ? DeepCopyObject(concrete, copyCrashboxes: false)
                : DeepCopyObject(original, copyCrashboxes: false);
        }

        private static _3dObject DeepCopyObject(_3dObject original, bool copyCrashboxes)
        {
            var copy = new _3dObject
            {
                ObjectId = original.ObjectId,
                ObjectOffsets = CopyRequiredVector(original.ObjectOffsets),
                Rotation = CopyRequiredVector(original.Rotation),
                WorldPosition = CopyRequiredVector(original.WorldPosition),
                ObjectParts = CopyObjectParts(original.ObjectParts),
                Movement = original.Movement,
                Particles = original.Particles,
                ImpactStatus = original.ImpactStatus,
                Mass = original.Mass,
                ObjectName = original.ObjectName,
                ParentSurface = original.ParentSurface,
                RotationOffsetX = original.RotationOffsetX,
                RotationOffsetY = original.RotationOffsetY,
                RotationOffsetZ = original.RotationOffsetZ,
                SurfaceBasedId = original.SurfaceBasedId,
                CrashBoxDebugMode = original.CrashBoxDebugMode,
                WeaponSystems = original.WeaponSystems,
                CrashBoxes = copyCrashboxes ? CopyCrashboxes(original.CrashBoxes) : original.CrashBoxes,
                CrashBoxNames = original.CrashBoxNames,
                CrashBoxesFollowRotation = original.CrashBoxesFollowRotation,
                CalculatedCrashOffset = original.CalculatedCrashOffset,
                HasShadow = original.HasShadow,
                HasPowerUp = original.HasPowerUp,
                ZSortBias = original.ZSortBias
            };

            return copy;
        }

        private static _3dObject DeepCopyObject(I3dObject original, bool copyCrashboxes)
        {
            var copy = new _3dObject
            {
                ObjectId = original.ObjectId,
                ObjectOffsets = CopyRequiredVector(original.ObjectOffsets),
                Rotation = CopyRequiredVector(original.Rotation),
                WorldPosition = CopyRequiredVector(original.WorldPosition),
                ObjectParts = CopyObjectParts(original.ObjectParts),
                Movement = original.Movement,
                Particles = original.Particles,
                ImpactStatus = original.ImpactStatus,
                Mass = original.Mass,
                ObjectName = original.ObjectName,
                ParentSurface = original.ParentSurface,
                RotationOffsetX = original.RotationOffsetX,
                RotationOffsetY = original.RotationOffsetY,
                RotationOffsetZ = original.RotationOffsetZ,
                SurfaceBasedId = original.SurfaceBasedId,
                CrashBoxDebugMode = original.CrashBoxDebugMode,
                WeaponSystems = original.WeaponSystems,
                CrashBoxes = copyCrashboxes ? CopyCrashboxes(original.CrashBoxes) : original.CrashBoxes,
                CrashBoxNames = original.CrashBoxNames,
                CrashBoxesFollowRotation = original.CrashBoxesFollowRotation,
                CalculatedCrashOffset = original.CalculatedCrashOffset,
                HasShadow = original.HasShadow,
                HasPowerUp = original.HasPowerUp,
                ZSortBias = original.ZSortBias
            };

            return copy;
        }

        private static List<I3dObjectPart> CopyObjectParts(List<I3dObjectPart> originalParts)
        {
            var objectParts = new List<I3dObjectPart>(originalParts.Count);

            for (int partIndex = 0; partIndex < originalParts.Count; partIndex++)
            {
                var part = originalParts[partIndex];
                var triangles = part.Triangles;
                var copiedTriangles = new List<ITriangleMeshWithColor>(triangles.Count);

                for (int triangleIndex = 0; triangleIndex < triangles.Count; triangleIndex++)
                {
                    copiedTriangles.Add(CopyTriangle(triangles[triangleIndex]));
                }

                objectParts.Add(new _3dObjectPart
                {
                    PartName = part.PartName,
                    Triangles = copiedTriangles,
                    IsVisible = part.IsVisible
                });
            }

            return objectParts;
        }

        private static TriangleMeshWithColor CopyTriangle(ITriangleMeshWithColor triangle)
        {
            var mesh = triangle as TriangleMesh;
            var triangleCopy = new TriangleMeshWithColor
            {
                landBasedPosition = triangle.landBasedPosition,
                angle = triangle.angle,
                Color = triangle.Color,
                noHidden = triangle.noHidden
            };

            var vert1 = CopyVector(mesh != null ? mesh.Vert1Raw : triangle.vert1);
            if (vert1 != null)
            {
                triangleCopy.vert1 = vert1;
            }

            var vert2 = CopyVector(mesh != null ? mesh.Vert2Raw : triangle.vert2);
            if (vert2 != null)
            {
                triangleCopy.vert2 = vert2;
            }

            var vert3 = CopyVector(mesh != null ? mesh.Vert3Raw : triangle.vert3);
            if (vert3 != null)
            {
                triangleCopy.vert3 = vert3;
            }

            var normal1 = CopyVector(mesh != null ? mesh.Normal1Raw : triangle.normal1);
            if (normal1 != null)
            {
                triangleCopy.normal1 = normal1;
            }

            var normal2 = CopyVector(mesh != null ? mesh.Normal2Raw : triangle.normal2);
            if (normal2 != null)
            {
                triangleCopy.normal2 = normal2;
            }

            var normal3 = CopyVector(mesh != null ? mesh.Normal3Raw : triangle.normal3);
            if (normal3 != null)
            {
                triangleCopy.normal3 = normal3;
            }

            return triangleCopy;
        }

        public static List<List<IVector3>> CopyCrashboxes(List<List<IVector3>> original)
        {
            if (original == null || original.Count == 0)
                return new List<List<IVector3>>();

            var result = new List<List<IVector3>>(original.Count);

            for (int boxIndex = 0; boxIndex < original.Count; boxIndex++)
            {
                var box = original[boxIndex];
                var copiedBox = new List<IVector3>(box.Count);

                for (int pointIndex = 0; pointIndex < box.Count; pointIndex++)
                {
                    copiedBox.Add(CopyRequiredVector(box[pointIndex]));
                }

                result.Add(copiedBox);
            }

            return result;
        }

        public static Vector3 GetCenterOfBox(List<Vector3> points)
        {
            if (points == null || points.Count == 0)
                return new Vector3();

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (var p in points)
            {
                minX = Math.Min(minX, p.x);
                maxX = Math.Max(maxX, p.x);

                minY = Math.Min(minY, p.y);
                maxY = Math.Max(maxY, p.y);

                minZ = Math.Min(minZ, p.z);
                maxZ = Math.Max(maxZ, p.z);
            }

            return new Vector3
            {
                x = (minX + maxX) / 2f,
                y = (minY + maxY) / 2f,
                z = (minZ + maxZ) / 2f
            };
        }

    }
}
