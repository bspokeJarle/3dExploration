using System;
using System.Collections.Generic;
using _3dTesting._3dWorld;
using CommonUtilities.CommonGlobalState;
using Domain;
using GameAiAndControls.Controls;
using static Domain._3dSpecificsImplementations;

namespace _3dRotations.World.Objects
{
    public class SeederGuidanceArrow
    {
        // ----------------------------------------------------
        //  GEOMETRY PARAMETERS
        // ----------------------------------------------------

        // Arrow points in +X direction
        private static float shaftLength = 20f;
        private static float shaftHalfWidth = 4.5f;
        private static float shaftHalfHeight = 2.5f;

        private static float headLength = 16f;
        private static float headHalfWidth = 11f;
        private static float headHalfHeight = 5.5f;

        private static float tailInset = 4f;
        private static float bevelInset = 2.2f;

        private static readonly Vector3 BodyCenter = new Vector3 { x = 0, y = 0, z = 0 };

        // ----------------------------------------------------
        //  COLORS
        // ----------------------------------------------------

        private static string cyanTop = "5FFBFF";
        private static string cyanLight = "3BE7F0";
        private static string cyanMid = "19C9D8";
        private static string cyanDark = "0D7F8D";
        private static string cyanDeep = "074B57";

        // ----------------------------------------------------
        //  OBJECT CREATION
        // ----------------------------------------------------

        public static _3dObject CreateSeederGuidanceArrow(ISurface parentSurface)
        {
            var body = ArrowBody();
            var head = ArrowHead();
            var bevels = ArrowBevels();

            var arrow = new _3dObject
            {
                ObjectId = GameState.ObjectIdCounter++,
                ObjectOffsets = new Vector3 { x = 0, y = 0, z = 0 },
                Rotation = new Vector3 { x = 0, y = 0, z = 0 },
                ParentSurface = parentSurface,
                HasShadow = false
            };

            AddPart(arrow, "SeederGuidanceArrowBody", body, true);
            AddPart(arrow, "SeederGuidanceArrowHead", head, true);
            AddPart(arrow, "SeederGuidanceArrowBevels", bevels, true);

            arrow.Movement = new SeederGuidanceArrowControl();
            return arrow;
        }

        // ----------------------------------------------------
        //  SHAFT / BODY
        // ----------------------------------------------------

        public static List<ITriangleMeshWithColor>? ArrowBody()
        {
            var tris = new List<ITriangleMeshWithColor>();

            float xBack = -shaftLength * 0.5f;
            float xFront = shaftLength * 0.5f;

            var v1 = new Vector3 { x = xBack, y = -shaftHalfWidth, z = shaftHalfHeight };
            var v2 = new Vector3 { x = xBack, y = shaftHalfWidth, z = shaftHalfHeight };
            var v3 = new Vector3 { x = xFront, y = shaftHalfWidth, z = shaftHalfHeight };
            var v4 = new Vector3 { x = xFront, y = -shaftHalfWidth, z = shaftHalfHeight };

            var v5 = new Vector3 { x = xBack, y = -shaftHalfWidth, z = -shaftHalfHeight };
            var v6 = new Vector3 { x = xBack, y = shaftHalfWidth, z = -shaftHalfHeight };
            var v7 = new Vector3 { x = xFront, y = shaftHalfWidth, z = -shaftHalfHeight };
            var v8 = new Vector3 { x = xFront, y = -shaftHalfWidth, z = -shaftHalfHeight };

            // Top
            AddQuadOutward(tris, v1, v2, v3, v4, BodyCenter, cyanTop);

            // Bottom
            AddQuadOutward(tris, v8, v7, v6, v5, BodyCenter, cyanDeep);

            // Left
            AddQuadOutward(tris, v5, v1, v4, v8, BodyCenter, cyanDark);

            // Right
            AddQuadOutward(tris, v2, v6, v7, v3, BodyCenter, cyanMid);

            // Back
            AddQuadOutward(tris, v5, v6, v2, v1, BodyCenter, cyanDark);

            return tris;
        }

        // ----------------------------------------------------
        //  ARROW HEAD
        // ----------------------------------------------------

        public static List<ITriangleMeshWithColor>? ArrowHead()
        {
            var tris = new List<ITriangleMeshWithColor>();

            float shaftFrontX = shaftLength * 0.5f;
            float headBaseX = shaftFrontX - 2f;
            float tipX = shaftFrontX + headLength;

            // Base ring of the head
            var topLeft = new Vector3 { x = headBaseX, y = -headHalfWidth, z = headHalfHeight };
            var topRight = new Vector3 { x = headBaseX, y = headHalfWidth, z = headHalfHeight };
            var bottomRight = new Vector3 { x = headBaseX, y = headHalfWidth, z = -headHalfHeight };
            var bottomLeft = new Vector3 { x = headBaseX, y = -headHalfWidth, z = -headHalfHeight };

            // Tip
            var tip = new Vector3 { x = tipX, y = 0, z = 0 };

            // Top face split
            tris.Add(CreateTriangleOutward(topLeft, topRight, tip, BodyCenter, cyanLight));

            // Bottom face split
            tris.Add(CreateTriangleOutward(bottomRight, bottomLeft, tip, BodyCenter, cyanDeep));

            // Left face split
            tris.Add(CreateTriangleOutward(bottomLeft, topLeft, tip, BodyCenter, cyanDark));

            // Right face split
            tris.Add(CreateTriangleOutward(topRight, bottomRight, tip, BodyCenter, cyanMid));

            // Back face of head to close geometry
            AddQuadOutward(tris, bottomLeft, bottomRight, topRight, topLeft, BodyCenter, cyanDark);

            return tris;
        }

        // ----------------------------------------------------
        //  BEVELS / EXTRA DEPTH
        // ----------------------------------------------------

        public static List<ITriangleMeshWithColor>? ArrowBevels()
        {
            var tris = new List<ITriangleMeshWithColor>();

            float xBack = -shaftLength * 0.5f + tailInset;
            float xFront = shaftLength * 0.5f - 1.5f;

            // Top bevel ridge
            AddLongBevel(
                tris,
                new Vector3 { x = xBack, y = 0, z = shaftHalfHeight },
                new Vector3 { x = xFront, y = 0, z = shaftHalfHeight },
                new Vector3 { x = 0, y = 0, z = 1 },
                new Vector3 { x = 0, y = 1, z = 0 },
                shaftHalfWidth - bevelInset,
                bevelInset,
                cyanLight,
                cyanMid);

            // Bottom bevel ridge
            AddLongBevel(
                tris,
                new Vector3 { x = xBack, y = 0, z = -shaftHalfHeight },
                new Vector3 { x = xFront, y = 0, z = -shaftHalfHeight },
                new Vector3 { x = 0, y = 0, z = -1 },
                new Vector3 { x = 0, y = 1, z = 0 },
                shaftHalfWidth - bevelInset,
                bevelInset,
                cyanDark,
                cyanDeep);

            return tris;
        }

        private static void AddLongBevel(
            List<ITriangleMeshWithColor> tris,
            Vector3 start,
            Vector3 end,
            Vector3 outward,
            Vector3 sideAxis,
            float halfSpan,
            float thickness,
            string topColor,
            string sideColor)
        {
            var left1 = Add(Add(start, Scale(sideAxis, -halfSpan)), Scale(outward, thickness));
            var right1 = Add(Add(start, Scale(sideAxis, halfSpan)), Scale(outward, thickness));
            var left2 = Add(Add(end, Scale(sideAxis, -halfSpan)), Scale(outward, thickness));
            var right2 = Add(Add(end, Scale(sideAxis, halfSpan)), Scale(outward, thickness));

            var baseLeft1 = Add(start, Scale(sideAxis, -halfSpan));
            var baseRight1 = Add(start, Scale(sideAxis, halfSpan));
            var baseLeft2 = Add(end, Scale(sideAxis, -halfSpan));
            var baseRight2 = Add(end, Scale(sideAxis, halfSpan));

            AddQuadOutward(tris, left1, right1, right2, left2, BodyCenter, topColor);
            AddQuadOutward(tris, baseLeft1, left1, left2, baseLeft2, BodyCenter, sideColor);
            AddQuadOutward(tris, right1, baseRight1, baseRight2, right2, BodyCenter, sideColor);
        }

        // ----------------------------------------------------
        //  HELPERS
        // ----------------------------------------------------

        private static void AddPart(_3dObject obj, string name, List<ITriangleMeshWithColor>? tris, bool visible)
        {
            if (tris == null)
                return;

            obj.ObjectParts.Add(new _3dObjectPart
            {
                PartName = name,
                Triangles = tris,
                IsVisible = visible
            });
        }

        private static void AddQuadOutward(
            List<ITriangleMeshWithColor> tris,
            Vector3 v1,
            Vector3 v2,
            Vector3 v3,
            Vector3 v4,
            Vector3 center,
            string color,
            bool noHidden = false)
        {
            tris.Add(CreateTriangleOutward(v1, v2, v3, center, color, noHidden));
            tris.Add(CreateTriangleOutward(v1, v3, v4, center, color, noHidden));
        }

        private static TriangleMeshWithColor CreateTriangleOutward(
            Vector3 v1,
            Vector3 v2,
            Vector3 v3,
            Vector3 center,
            string color,
            bool noHidden = false)
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
                var temp = v2;
                v2 = v3;
                v3 = temp;
            }

            return new TriangleMeshWithColor
            {
                Color = color,
                vert1 = v1,
                vert2 = v2,
                vert3 = v3,
                noHidden = noHidden
            };
        }

        private static Vector3 Add(Vector3 a, Vector3 b)
        {
            return new Vector3
            {
                x = a.x + b.x,
                y = a.y + b.y,
                z = a.z + b.z
            };
        }

        private static Vector3 Scale(Vector3 v, float s)
        {
            return new Vector3
            {
                x = v.x * s,
                y = v.y * s,
                z = v.z * s
            };
        }

        private static Vector3 Subtract(Vector3 a, Vector3 b)
        {
            return new Vector3
            {
                x = a.x - b.x,
                y = a.y - b.y,
                z = a.z - b.z
            };
        }

        private static Vector3 Cross(Vector3 a, Vector3 b)
        {
            return new Vector3
            {
                x = a.y * b.z - a.z * b.y,
                y = a.z * b.x - a.x * b.z,
                z = a.x * b.y - a.y * b.x
            };
        }

        private static float Dot(Vector3 a, Vector3 b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }

        private static Vector3 Normalize(Vector3 v)
        {
            float lenSq = v.x * v.x + v.y * v.y + v.z * v.z;
            if (lenSq <= 1e-6f)
                return new Vector3 { x = 0, y = 0, z = 0 };

            float invLen = 1.0f / (float)Math.Sqrt(lenSq);
            return new Vector3
            {
                x = v.x * invLen,
                y = v.y * invLen,
                z = v.z * invLen
            };
        }
    }
}