using _3dTesting._3dWorld;
using _3dTesting._Coordinates;
using _3dTesting.Helpers;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting._3dRotation
{
    public class _3dTo2d
    {
        // Class Description:
        // 3dTo2d handles the conversion of 3d objects to 2d objects
        // It also handles the perspective adjustment and positioning of the objects on the screen

        private const int perspectiveAdjustment = 1500;
        private const int defaultObjectZoom = 2;

        private const int screenSizeX = 1500;
        private const int screenSizeY = 1024;
        private const int screenCenterX = screenSizeX / 2;
        private const int screenCenterY = screenSizeY / 2;

        public List<_2dTriangleMesh> convertTo2dFromObjects(List<_3dObject> inhabitants)
        {
            var screenCoordinates = new ConcurrentBag<_2dTriangleMesh>();

            Parallel.ForEach(inhabitants, obj =>
            {
                if (obj == null || !obj.CheckInhabitantVisibility()) return;

                var localWorldPosition = obj.GetLocalWorldPosition();
                double objPosX, objPosY, objPosZ;
                int landbasedId = 0;

                if (localWorldPosition == null)
                {
                    if (obj.SurfaceBasedId > 0)
                    {
                        landbasedId = (int)obj.SurfaceBasedId;
                        var landBasedPosition = obj.ParentSurface?.RotatedSurfaceTriangles
                            .FirstOrDefault(t => t.landBasedPosition == obj.SurfaceBasedId);

                        if (landBasedPosition != null)
                        {
                            _3dObjectHelpers.CenterObjectAt(obj, landBasedPosition.vert1);
                            objPosX = screenCenterX + obj.Position.x;
                            objPosY = screenCenterY + obj.Position.y;
                            objPosZ = obj.Position.z;
                        }
                        else return;
                    }
                    else
                    {
                        objPosX = screenCenterX + obj.Position.x;
                        objPosY = screenCenterY + obj.Position.y;
                        objPosZ = obj.Position.z;
                    }
                }
                else
                {
                    objPosX = screenCenterX + localWorldPosition.x + obj.Position.x;
                    objPosY = screenCenterY + localWorldPosition.y + obj.Position.y;
                    objPosZ = localWorldPosition.z + obj.Position.z;
                }

                foreach (var parts in obj.ObjectParts)
                {
                    if (!parts.IsVisible) continue;

                    foreach (var triangle in parts.Triangles)
                    {
                        var (xc, yc) = ProjectVertex((Vector3)triangle.vert1, objPosX, objPosY, objPosZ);
                        var (xc2, yc2) = ProjectVertex((Vector3)triangle.vert2, objPosX, objPosY, objPosZ);
                        var (xc3, yc3) = ProjectVertex((Vector3)triangle.vert3, objPosX, objPosY, objPosZ);

                        double xFactor = (xc + xc2 + xc3) / 3;
                        double yFactor = (yc + yc2 + yc3) / 3;

                        if (!IsOnScreen(xFactor, yFactor)) continue;

                        if (triangle.normal1.z > 0 || (triangle.noHidden ?? false))
                        {
                            screenCoordinates.Add(new _2dTriangleMesh
                            {
                                X1 = Convert.ToInt32(xc),
                                Y1 = Convert.ToInt32(yc),
                                X2 = Convert.ToInt32(xc2),
                                Y2 = Convert.ToInt32(yc2),
                                X3 = Convert.ToInt32(xc3),
                                Y3 = Convert.ToInt32(yc3),
                                CalculatedZ = ((triangle.vert1.z + triangle.vert2.z + triangle.vert3.z) / 3)
                                              + (_3dObjectHelpers.GetDeepestZ(triangle) + obj.Position.z),
                                Normal = triangle.normal1.z,
                                TriangleAngle = triangle.angle,
                                Color = triangle.Color,
                                PartName = parts.PartName
                            });
                        }
                    }
                }
            });

            return screenCoordinates.ToList();
        }

        private (double x, double y) ProjectVertex(Vector3 v, double objPosX, double objPosY, double objPosZ)
        {
            double factor = perspectiveAdjustment / (-v.z + objPosZ + perspectiveAdjustment);
            double x = (v.x * factor * defaultObjectZoom) + objPosX;
            double y = (v.y * factor * defaultObjectZoom) + objPosY;
            return (x, y);
        }

        private bool IsOnScreen(double x, double y)
        {
            return x >= -(screenSizeX * 0.2) && x <= (screenSizeX * 1.2)
                && y >= -(screenSizeY * 0.2) && y <= (screenSizeY * 1.2);
        }
    }
}
