using _3dTesting._3dWorld;
using _3dTesting._Coordinates;
using _3dTesting.Helpers;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting._3dRotation
{
    public class _3dTo2d
    {
        //Class Description:
        //3dTo2d handles the conversion of 3d objects to 2d objects
        //It also handles the perspective adjustment and positioning of the objects on the screen
        List<_2dTriangleMesh> screenCoordinates { get; set; }
        const int perspectiveAdjustment = 1500;
        int defaultObjectZoom = 2; // Adjusted for more consistent object size

        const int screenSizeX = 1500;
        const int screenSizeY = 1024;
        const int screenCenterX = screenSizeX / 2;
        const int screenCenterY = screenSizeY / 2;

        public List<_2dTriangleMesh> convertTo2dFromObjects(List<_3dObject> inhabitants)
        {
            screenCoordinates = new List<_2dTriangleMesh>();
            var landbasedId = 0;
            foreach (var obj in inhabitants)
            {                
                var localWorldPosition = obj.GetLocalWorldPosition();
                if (obj.CheckInhabitantVisibility() == false || obj == null) continue;

                double objPosX = 0;
                double objPosY = 0;
                double objPosZ = 0;

                if (localWorldPosition == null)
                {
                    if (obj.SurfaceBasedId > 0)
                    {
                        landbasedId = (int)obj.SurfaceBasedId;
                        var landBasedPosition = (TriangleMeshWithColor)obj.ParentSurface.RotatedSurfaceTriangles.Where(t => t.landBasedPosition == obj.SurfaceBasedId).FirstOrDefault();
                        if (landBasedPosition != null)
                        {
                            //Land based objects must be centered around a surface position and then placed according to an offset
                            _3dObjectHelpers.CenterObjectAt(obj, landBasedPosition.vert1);
                            objPosX = screenCenterX;
                            objPosY = screenCenterY;
                            objPosZ = 0;
                        }
                        //Skip if object is landbased and no surface position is found
                        else continue;
                    }
                    else
                    {
                        objPosX = screenCenterX + obj.Position.x;
                        objPosY = screenCenterY + obj.Position.y;
                        objPosZ = 0;
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
                        var perspectiveFactor = perspectiveAdjustment / (-triangle.vert1.z + objPosZ + perspectiveAdjustment);
                        var xc = (triangle.vert1.x * perspectiveFactor * defaultObjectZoom) + objPosX;
                        var yc = (triangle.vert1.y * perspectiveFactor * defaultObjectZoom) + objPosY;

                        var perspectiveFactor2 = perspectiveAdjustment / (-triangle.vert2.z + objPosZ + perspectiveAdjustment);
                        var xc2 = (triangle.vert2.x * perspectiveFactor2 * defaultObjectZoom) + objPosX ;
                        var yc2 = (triangle.vert2.y * perspectiveFactor2 * defaultObjectZoom) + objPosY;

                        var perspectiveFactor3 = perspectiveAdjustment / (-triangle.vert3.z + objPosZ + perspectiveAdjustment);
                        var xc3 = (triangle.vert3.x * perspectiveFactor3 * defaultObjectZoom) + objPosX;
                        var yc3 = (triangle.vert3.y * perspectiveFactor3 * defaultObjectZoom) + objPosY;

                        var xFactor = (xc + xc2 + xc3) / 3;
                        if (xFactor < -(screenSizeX * 0.2) || xFactor > (screenSizeX * 1.2)) continue;

                        var yFactor = (yc + yc2 + yc3) / 3;
                        if (yFactor < -(screenSizeY * 0.2) || yFactor > (screenSizeY * 1.2)) continue;

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
                                CalculatedZ = ((triangle.vert1.z + triangle.vert2.z + triangle.vert3.z) / 3) + (_3dObjectHelpers.GetDeepestZ(triangle) + obj.Position.z),
                                Normal = triangle.normal1.z,
                                TriangleAngle = triangle.angle,
                                Color = triangle.Color,
                                PartName = parts.PartName
                            });
                        }
                    }
                }
            }
            return screenCoordinates;
        }
    }
}
