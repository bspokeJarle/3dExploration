using Domain;
using static Domain._3dSpecificsImplementations;


namespace CommonUtilities._3DHelpers
{
    public static class Common3dObjectHelpers
    {
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
            var globalMapPosition = inhabitant.ParentSurface.GlobalMapPosition;
            var inhabitantPosition = inhabitant.WorldPosition;

            float distance = (float)GetDistance(globalMapPosition, (Vector3)inhabitantPosition);

            return Math.Abs(distance) <= 1400;
        }


        public static double GetDistance(Vector3 point1, Vector3 point2)
        {
            float dx = point1.x - point2.x;
            float dy = point1.y - point2.y;
            float dz = point1.z - point2.z;

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
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
            var result = inhabitants
            .Where(i => i.CheckInhabitantVisibility())
            .Select(inhabitant =>
             {
                 var copy = Common3dObjectHelpers.DeepCopySingleObject(inhabitant);

                 // Restore crashboxes as well
                 copy.CrashBoxes = CopyCrashboxes(inhabitant.CrashBoxes);

                 return (_3dObject)copy;
             }).ToList();

            return result;
        }

        public static I3dObject DeepCopySingleObject(I3dObject original)
        {
            var objectParts = original.ObjectParts.Select(part =>
            {
                var triangles = part.Triangles.Select(triangle => new TriangleMeshWithColor
                {
                    vert1 = new Vector3 { x = triangle.vert1.x, y = triangle.vert1.y, z = triangle.vert1.z },
                    vert2 = new Vector3 { x = triangle.vert2.x, y = triangle.vert2.y, z = triangle.vert2.z },
                    vert3 = new Vector3 { x = triangle.vert3.x, y = triangle.vert3.y, z = triangle.vert3.z },
                    normal1 = new Vector3 { x = triangle.normal1.x, y = triangle.normal1.y, z = triangle.normal1.z },
                    normal2 = new Vector3 { x = triangle.normal2.x, y = triangle.normal2.y, z = triangle.normal2.z },
                    normal3 = new Vector3 { x = triangle.normal3.x, y = triangle.normal3.y, z = triangle.normal3.z },
                    landBasedPosition = triangle.landBasedPosition,
                    angle = triangle.angle,
                    Color = triangle.Color,
                    noHidden = triangle.noHidden
                }).ToList();

                return new _3dObjectPart
                {
                    PartName = part.PartName,
                    Triangles = triangles.Select(t => (ITriangleMeshWithColor)t).ToList(),
                    IsVisible = part.IsVisible
                };
            }).ToList();

            var copy = new _3dObject
            {
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
                ObjectParts = objectParts.Cast<I3dObjectPart>().ToList(),
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
                CrashBoxes = new List<List<IVector3>>() // tom liste – eksploderende objekter trenger ikke kollisjon
            };

            return copy;
        }

        public static List<List<IVector3>> CopyCrashboxes(List<List<IVector3>> original)
        {
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
      
    }
}
