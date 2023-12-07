using System;
using System.Collections.Generic;
using Domain;
using static Domain._3dSpecificsImplementations;

namespace _3dTesting._3dRotation
{
    public class _3dRotate
    {        
        private TriangleMeshWithColor CalculateNormal(TriangleMeshWithColor coord)
        {
            //P.S the left hand rule makes this possible
            //Calculate crossproduct
            //Normalizing the crossproduct vectors
            //Calculate the vector length
            //Calculate the common normal vector
            //We need this to determine wether this triangle is facing the camera or not

            //Crossproduct vectors
            var U = new Vector3 { x = (coord.vert2.x-coord.vert1.x),z=(coord.vert2.z-coord.vert1.z),y=(coord.vert2.y-coord.vert1.y) };
            var V = new Vector3 { x = (coord.vert3.x-coord.vert1.x),z=(coord.vert3.z-coord.vert1.z),y=(coord.vert3.y-coord.vert1.y) };
            //Calculate Normalized crossproduct vectors
            var Ulength = (float)Math.Sqrt((U.x * U.x) + (U.y * U.y) + (U.z * U.z));
            var Vlength = (float)Math.Sqrt((V.x * V.x) + (V.y * V.y) + (V.z * V.z));
            var NU = new Vector3 { x = (U.x / Ulength), y = (U.y / Ulength), z = (U.z / Ulength) };
            var NV = new Vector3 { x = (V.x / Vlength), y = (V.y / Vlength), z = (V.z / Vlength) };
            //Get the common normal vector
            var Nx = (NU.y * NV.z) - (NU.z * NV.y);
            var Ny = (NU.z * NV.x) - (NU.x * NV.z);
            var Nz = (NU.x * NV.y) - (NU.y * NV.x);
            coord.normal1.x = Nx;
            coord.normal1.y = Ny;
            coord.normal1.z = Nz;
            //Getting the angle of the triangle for shading purposes (as a COS(Theta) value)
            coord.angle = CalculateAngle(coord);            
            return coord;
        }
        
        private float CalculateAngle(TriangleMeshWithColor normal)
        {
            //This is the light vector
            var lp1 = new Vector3 { x = 0, y = 0, z = 0 };
            var lp2 = new Vector3 { x = 0, y = 0, z = 250 };
            //Calculate light vector crossproduct
            var lv = new Vector3 { x = (lp2.x - lp1.x), y = (lp2.y - lp1.y), z = (lp2.z - lp1.z) };
            //Calculate vector length
            var length = (float)Math.Sqrt((lv.x * lv.x) + (lv.y * lv.y) + (lv.z * lv.z));            
            //Calculate Normalized light vector NLV
            var Nlv = new Vector3 { x = (lv.x / length), y = (lv.y / length), z = (lv.z / length) };
            //N' = (L' x* N' x) + (L' y* N' y) + (L' z* N' z)
            return (Nlv.x * normal.normal1.x) + (Nlv.y * normal.normal1.y) + (Nlv.z * normal.normal1.z);
        }

        private TriangleMeshWithColor RotateOnX(float cosRes,float sinRes, TriangleMeshWithColor coord)
        {
            var triangle = new TriangleMeshWithColor
            { 
                vert1 = { x = coord.vert1.x, y = (coord.vert1.y * cosRes) - (coord.vert1.z * sinRes), z = (coord.vert1.z * cosRes) + (coord.vert1.y * sinRes) },
                vert2 = { x = coord.vert2.x, y = (coord.vert2.y * cosRes) - (coord.vert2.z * sinRes), z = (coord.vert2.z * cosRes) + (coord.vert2.y * sinRes) },
                vert3 = { x = coord.vert3.x, y = (coord.vert3.y * cosRes) - (coord.vert3.z * sinRes), z = (coord.vert3.z * cosRes) + (coord.vert3.y * sinRes) }                
            };
            triangle.Color = coord.Color;
            return CalculateNormal(triangle);            
        }

        private TriangleMeshWithColor RotateOnY(float cosRes, float sinRes, TriangleMeshWithColor coord)
        {            
            var triangle = new TriangleMeshWithColor
            {                
                vert1 = { x = (coord.vert1.x * cosRes) + (coord.vert1.z * sinRes), y = coord.vert1.y, z = (coord.vert1.z * cosRes) - (coord.vert1.x * sinRes) },
                vert2 = { x = (coord.vert2.x * cosRes) + (coord.vert2.z * sinRes), y = coord.vert2.y, z = (coord.vert2.z * cosRes) - (coord.vert2.x * sinRes) },
                vert3 = { x = (coord.vert3.x * cosRes) + (coord.vert3.z * sinRes), y = coord.vert3.y, z = (coord.vert3.z * cosRes) - (coord.vert3.x * sinRes) }
            };
            triangle.Color = coord.Color;
            return CalculateNormal(triangle);
        }
        private TriangleMeshWithColor RotateOnZ(float cosRes, float sinRes, TriangleMeshWithColor coord)
        {            
            var triangle = new TriangleMeshWithColor
            {
                vert1 = { x = coord.vert1.x * cosRes - coord.vert1.y * sinRes, y = coord.vert1.y * cosRes + coord.vert1.x * sinRes, z = coord.vert1.z },
                vert2 = { x = coord.vert2.x * cosRes - coord.vert2.y * sinRes, y = coord.vert2.y * cosRes + coord.vert2.x * sinRes, z = coord.vert2.z },
                vert3 = { x = coord.vert3.x * cosRes - coord.vert3.y * sinRes, y = coord.vert3.y * cosRes + coord.vert3.x * sinRes, z = coord.vert3.z }                
            };
            triangle.Color = coord.Color;
            return CalculateNormal(triangle);
        }
        public List<ITriangleMeshWithColor> RotateXMesh(List<ITriangleMeshWithColor> X, double angle)
        {
            var radian = Math.PI * angle / 180.0;
            var rotatedX = new List<ITriangleMeshWithColor>();
            var sinRes = Math.Sin(radian);
            var cosRes = Math.Cos(radian);
            foreach (TriangleMeshWithColor TheX in X)
            {
                rotatedX.Add(RotateOnX((float)cosRes,(float)sinRes,TheX));
            }
            return rotatedX;
        }
        public List<ITriangleMeshWithColor> RotateYMesh(List<ITriangleMeshWithColor> Y, double angle)
        {
            var radian = Math.PI * angle / 180.0;
            var rotatedY = new List<ITriangleMeshWithColor>();
            var cosRes = Math.Cos(radian);
            var sinRes = Math.Sin(radian);
            foreach (TriangleMeshWithColor TheY in Y)
            {
                rotatedY.Add(RotateOnY((float)cosRes, (float)sinRes,TheY));
            }
            return rotatedY;
        }
        public List<ITriangleMeshWithColor> RotateZMesh(List<ITriangleMeshWithColor> Z, double angle)
        {
            var radian = Math.PI * angle / 180.0;
            var rotatedZ = new List<ITriangleMeshWithColor>();
            var cosRes = Math.Cos(radian);
            var sinRes = Math.Sin(radian);
            foreach (TriangleMeshWithColor TheZ in Z)
            {
                rotatedZ.Add(RotateOnZ((float)cosRes, (float)sinRes, TheZ));
            }            
            return rotatedZ;
        }
    }
}
