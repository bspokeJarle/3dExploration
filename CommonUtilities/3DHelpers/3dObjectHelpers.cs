using CommonUtilities.CommonGlobalState;
using Domain;
using static Domain._3dSpecificsImplementations;


namespace CommonUtilities._3DHelpers
{
    public static class Common3dObjectHelpers
    {
        private static Vector3? CopyVector(IVector3? vector)
        {
            if (vector == null)
            {
                return null;
            }

            return new Vector3(vector.x, vector.y, vector.z);
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

            const float maxDistance = 1400f;
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

            foreach (var inhabitant in inhabitants)
            {
                if (!inhabitant.CheckInhabitantVisibility())
                {
                    continue;
                }

                var copy = Common3dObjectHelpers.DeepCopySingleObject(inhabitant);

                // Restore crashboxes as well
                copy.CrashBoxes = CopyCrashboxes(inhabitant.CrashBoxes);

                result.Add((_3dObject)copy);
            }

            return result;
        }

        public static I3dObject DeepCopySingleObject(I3dObject original)
        {
            var objectParts = new List<I3dObjectPart>(original.ObjectParts.Count);

            foreach (var part in original.ObjectParts)
            {
                var triangles = new List<ITriangleMeshWithColor>(part.Triangles.Count);

                foreach (var triangle in part.Triangles)
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

                    triangles.Add(triangleCopy);
                }

                objectParts.Add(new _3dObjectPart
                {
                    PartName = part.PartName,
                    Triangles = triangles,
                    IsVisible = part.IsVisible
                });
            }

            var copy = new _3dObject
            {
                ObjectId = original.ObjectId,
                ObjectOffsets = new Vector3
                {
                    x = original.ObjectOffsets.x,
                    y = original.ObjectOffsets.y,
                    z = original.ObjectOffsets.z
                },
                Rotation = new Vector3
                {
                    x = original.Rotation.x,
                    y = original.Rotation.y,
                    z = original.Rotation.z
                },
                WorldPosition = new Vector3
                {
                    x = original.WorldPosition.x,
                    y = original.WorldPosition.y,
                    z = original.WorldPosition.z
                },
                ObjectParts = objectParts,
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
                CrashBoxes = original.CrashBoxes,
                CrashBoxesFollowRotation = original.CrashBoxesFollowRotation,
                CalculatedWorldOffset = original.CalculatedWorldOffset
            };

            return copy;
        }

        public static List<List<IVector3>> CopyCrashboxes(List<List<IVector3>> original)
        {
            if (original == null)
                return new List<List<IVector3>>();
            var result = new List<List<IVector3>>(original.Count);
            foreach (var box in original)
            {
                var copiedBox = new List<IVector3>(box.Count);
                foreach (var point in box)
                {
                    copiedBox.Add(new Vector3
                    {
                        x = point.x,
                        y = point.y,
                        z = point.z
                    });
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
