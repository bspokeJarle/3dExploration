using System;
using System.Collections.Generic;
using _3dTesting._3dWorld;
using _3dTesting.Helpers;
using CommonUtilities.CommonGlobalState;
using Domain;
using GameAiAndControls.Ai;
using static Domain._3dSpecificsImplementations;

namespace _3dRotations.World.Objects
{
    public static class PowerUp
    {
        private const float ZoomRatio = 1f;

        // Plus sign dimensions – roughly seeder-sized (~70 unit span)
        private const float ArmLength = 35f;
        private const float ArmWidth = 8f;
        private const float ArmDepth = 12f;

        // Blue color palette
        private const string FrontColor = "4488FF";
        private const string BackColor = "3366CC";
        private const string TopColor = "66AAFF";
        private const string BottomColor = "2255BB";
        private const string SideLightColor = "5599EE";
        private const string SideDarkColor = "2266DD";

        public static _3dObject CreatePowerup(ISurface parentSurface)
        {
            var body = PlusSignBody();
            var crash = PlusSignCrashBoxes();

            var powerup = new _3dObject
            {
                ObjectId = GameState.ObjectIdCounter++,
                ObjectOffsets = new Vector3 { x = 0, y = 0, z = 0 },
                Rotation = new Vector3 { x = 0, y = 0, z = 0 },
                WorldPosition = new Vector3 { x = 0, y = 0, z = 0 },
                Particles = new ParticlesAI(),
                ParentSurface = parentSurface,
                ObjectName = "PowerUp",
                CrashBoxDebugMode = false,
                ImpactStatus = new ImpactStatus { ObjectName = "PowerUp" }
            };

            if (body != null)
            {
                powerup.ObjectParts.Add(new _3dObjectPart
                {
                    PartName = "PowerUpBody",
                    Triangles = body,
                    IsVisible = true
                });
            }

            if (crash != null)
                powerup.CrashBoxes = crash;

            _3dObjectHelpers.ApplyScaleToObject(powerup, ZoomRatio);

            return powerup;
        }

        // ----------------------------------------------------
        //  CRASH BOX – enlarged for easy pickup
        // ----------------------------------------------------

        private const float CrashBoxSizeMultiplier = 6f;

        public static List<List<IVector3>>? PlusSignCrashBoxes()
        {
            float extent = ArmLength * CrashBoxSizeMultiplier;
            float depth = ArmDepth * CrashBoxSizeMultiplier;
            var min = new Vector3 { x = -extent, y = -depth, z = -extent };
            var max = new Vector3 { x = extent, y = depth, z = extent };

            return new List<List<IVector3>>
            {
                _3dObjectHelpers.GenerateCrashBoxCorners(min, max)
            };
        }

        // ----------------------------------------------------
        //  PLUS SIGN GEOMETRY
        // ----------------------------------------------------
        //
        // Two intersecting rectangular prisms forming a 3D cross:
        //   Horizontal arm: extends along X, narrow in Z
        //   Vertical arm:   extends along Z, narrow in X
        //
        // 24 triangles per arm × 2 arms = 48 triangles total.
        // Outward normals enforced via CreateTriangleOutward.
        //

        public static List<ITriangleMeshWithColor>? PlusSignBody()
        {
            var tris = new List<ITriangleMeshWithColor>();

            // Horizontal arm (extends in X, narrow in Z)
            AddBox(tris,
                -ArmLength, ArmLength,
                -ArmWidth, ArmWidth,
                -ArmDepth, ArmDepth,
                FrontColor, BackColor, TopColor, BottomColor, SideLightColor, SideDarkColor);

            // Vertical arm (extends in Z, narrow in X)
            AddBox(tris,
                -ArmWidth, ArmWidth,
                -ArmLength, ArmLength,
                -ArmDepth, ArmDepth,
                FrontColor, BackColor, TopColor, BottomColor, SideDarkColor, SideLightColor);

            return tris;
        }

        // Generates 12 triangles (6 faces) for an axis-aligned box
        private static void AddBox(
            List<ITriangleMeshWithColor> tris,
            float minX, float maxX,
            float minZ, float maxZ,
            float minY, float maxY,
            string frontCol, string backCol,
            string topCol, string bottomCol,
            string leftRightCol, string endCol)
        {
            //  f = front (y=minY), b = back (y=maxY)
            //  t = top   (z=maxZ), b = bottom (z=minZ)
            //  l = left  (x=minX), r = right  (x=maxX)
            var fbl = new Vector3 { x = minX, y = minY, z = minZ };
            var fbr = new Vector3 { x = maxX, y = minY, z = minZ };
            var ftl = new Vector3 { x = minX, y = minY, z = maxZ };
            var ftr = new Vector3 { x = maxX, y = minY, z = maxZ };
            var bbl = new Vector3 { x = minX, y = maxY, z = minZ };
            var bbr = new Vector3 { x = maxX, y = maxY, z = minZ };
            var btl = new Vector3 { x = minX, y = maxY, z = maxZ };
            var btr = new Vector3 { x = maxX, y = maxY, z = maxZ };

            var center = new Vector3
            {
                x = (minX + maxX) / 2f,
                y = (minY + maxY) / 2f,
                z = (minZ + maxZ) / 2f
            };

            // Front face (y = minY, facing -Y)
            tris.Add(CreateTriangleOutward(ftl, ftr, fbr, center, frontCol));
            tris.Add(CreateTriangleOutward(ftl, fbr, fbl, center, frontCol));

            // Back face (y = maxY, facing +Y)
            tris.Add(CreateTriangleOutward(btr, btl, bbl, center, backCol));
            tris.Add(CreateTriangleOutward(btr, bbl, bbr, center, backCol));

            // Top face (z = maxZ, facing +Z)
            tris.Add(CreateTriangleOutward(ftl, btl, btr, center, topCol));
            tris.Add(CreateTriangleOutward(ftl, btr, ftr, center, topCol));

            // Bottom face (z = minZ, facing -Z)
            tris.Add(CreateTriangleOutward(fbr, bbr, bbl, center, bottomCol));
            tris.Add(CreateTriangleOutward(fbr, bbl, fbl, center, bottomCol));

            // Right face (x = maxX)
            tris.Add(CreateTriangleOutward(ftr, btr, bbr, center, endCol));
            tris.Add(CreateTriangleOutward(ftr, bbr, fbr, center, endCol));

            // Left face (x = minX)
            tris.Add(CreateTriangleOutward(btl, ftl, fbl, center, leftRightCol));
            tris.Add(CreateTriangleOutward(btl, fbl, bbl, center, leftRightCol));
        }

        // ---- Normal helpers (same pattern as Seeder / KamikazeDrone) ----

        private static TriangleMeshWithColor CreateTriangleOutward(
            Vector3 v1, Vector3 v2, Vector3 v3,
            Vector3 center, string color)
        {
            var edge1 = Subtract(v2, v1);
            var edge2 = Subtract(v3, v1);
            var normal = Normalize(Cross(edge1, edge2));

            var mid = new Vector3
            {
                x = (v1.x + v2.x + v3.x) / 3f,
                y = (v1.y + v2.y + v3.y) / 3f,
                z = (v1.z + v2.z + v3.z) / 3f
            };

            var desired = Normalize(Subtract(mid, center));
            float dot = Dot(normal, desired);

            if (dot < 0f)
            {
                (v2, v3) = (v3, v2);
            }

            return new TriangleMeshWithColor
            {
                Color = color,
                vert1 = v1,
                vert2 = v2,
                vert3 = v3
            };
        }

        private static Vector3 Subtract(Vector3 a, Vector3 b) =>
            new Vector3 { x = a.x - b.x, y = a.y - b.y, z = a.z - b.z };

        private static Vector3 Cross(Vector3 a, Vector3 b) =>
            new Vector3
            {
                x = a.y * b.z - a.z * b.y,
                y = a.z * b.x - a.x * b.z,
                z = a.x * b.y - a.y * b.x
            };

        private static float Dot(Vector3 a, Vector3 b) =>
            a.x * b.x + a.y * b.y + a.z * b.z;

        private static Vector3 Normalize(Vector3 v)
        {
            float len = MathF.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
            if (len < 1e-6f) return v;
            return new Vector3 { x = v.x / len, y = v.y / len, z = v.z / len };
        }
    }
}