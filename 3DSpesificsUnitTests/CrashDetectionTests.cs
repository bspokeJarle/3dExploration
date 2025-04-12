using Microsoft.VisualStudio.TestTools.UnitTesting;
using _3dTesting.Helpers;
using Domain;
using System.Collections.Generic;
using static Domain._3dSpecificsImplementations;
using _3dTesting._3dWorld;
using _3dRotations.World.Objects;
using _3dTesting._3dRotation;

namespace _3DSpesificsUnitTests
{
    [TestClass]
    public class CrashBoxHelperTests
    {
        [TestMethod]
        public void CenterCrashBoxesAt_SurfaceBasedObject_SetsCorrectPosition()
        {
            var obj = new _3dObject
            {
                CrashBoxes = new List<List<IVector3>>
                {
                    new List<IVector3> { new Vector3 { x = -10, y = -10, z = -10 }, new Vector3 { x = 10, y = 10, z = 10 } }
                },
                SurfaceBasedId = 1,
                ParentSurface = new Surface
                {
                    RotatedSurfaceTriangles = new List<ITriangleMeshWithColor>
                    {
                        new TriangleMeshWithColor
                        {
                            landBasedPosition = 1,
                            vert1 = new Vector3 { x = 100, y = 200, z = 300 }
                        }
                    }
                }
            };

            ObjectPlacementHelpers.CenterCrashBoxesAt(obj, new Vector3 { x = 100, y = 200, z = 300 });

            var center = obj.CrashBoxes[0][0];
            Assert.IsTrue(center.x >= 89.9 && center.x <= 90.1);
            Assert.IsTrue(center.y >= 200.0 && center.y <= 200.1);
            Assert.IsTrue(center.z >= 289.9 && center.z <= 290.1);
        }

        [TestMethod]
        public void CenterCrashBoxesAt_NonSurfaceObject_UsesOffsets()
        {
            var obj = new _3dObject
            {
                CrashBoxes = new List<List<IVector3>>
                {
                    new List<IVector3> { new Vector3 { x = -5, y = 0, z = -5 }, new Vector3 { x = 5, y = 10, z = 5 } }
                },
                ObjectOffsets = new Vector3 { x = 50, y = 60, z = 70 }
            };

            ObjectPlacementHelpers.CenterCrashBoxesAt(obj, obj.ObjectOffsets);

            var center = obj.CrashBoxes[0][0];
            Assert.IsTrue(center.x >= 45 && center.x <= 46);
            Assert.IsTrue(center.y >= 60 && center.y <= 61);
            Assert.IsTrue(center.z >= 65 && center.z <= 66);
        }

        [TestMethod]
        public void CenterCrashBoxesAt_DynamicObject_UsesWorldPosition()
        {
            var obj = new _3dObject
            {
                CrashBoxes = new List<List<IVector3>>
                {
                    new List<IVector3> { new Vector3 { x = -20, y = -5, z = -10 }, new Vector3 { x = 20, y = 5, z = 10 } }
                },
                ObjectName = "Particle",
                WorldPosition = new Vector3 { x = 25, y = 35, z = 45 }
            };

           ObjectPlacementHelpers.CenterCrashBoxesAt(obj, obj.WorldPosition);

            var center = obj.CrashBoxes[0][0];
            Assert.IsTrue(center.x >= 5 && center.x <= 6);
            Assert.IsTrue(center.y >= 35 && center.y <= 36);
            Assert.IsTrue(center.z >= 35 && center.z <= 36);
        }
        //TODO: Gotta make this work later
        /*[TestMethod]
        public void CrashBox_ShouldBeAlignedWith2DCenter_SurfaceBased()
        {
            // Arrange
            var obj = TestObjectFactory.CreateSurfaceBasedTestObject();
            var to2d = new _3dTo2d();

            ObjectPlacementHelpers.CenterCrashBoxesAt(obj, new Vector3 { x = 100, y = 0, z = 200 });
            var centerBefore = ObjectPlacementHelpers.GetCrashBoxCenter(obj.CrashBoxes);

            // Act
            var result = to2d.convertTo2dFromObjects(new List<_3dObject> { obj });

            // Assert
            Assert.IsTrue(result.Count > 0);
            var avgX = result.Average(t => (t.X1 + t.X2 + t.X3) / 3.0);
            var avgZ = result.Average(t => t.CalculatedZ);
            Assert.AreEqual(centerBefore.x, avgX, 15.0, "Crashbox X center not close to 2D X");
            Assert.AreEqual(centerBefore.z, avgZ, 15.0, "Crashbox Z center not close to 2D Z");
        }

        [TestMethod]
        public void CrashBox_ShouldBeAlignedWith2DCenter_Dynamic()
        {
            var obj = TestObjectFactory.CreateDynamicTestObject();
            var to2d = new _3dTo2d();

            ObjectPlacementHelpers.CenterCrashBoxesAt(obj, new Vector3 { x = 0, y = 0, z = 0 });
            var centerBefore = ObjectPlacementHelpers.GetCrashBoxCenter(obj.CrashBoxes);

            var result = to2d.convertTo2dFromObjects(new List<_3dObject> { obj });

            Assert.IsTrue(result.Count > 0);
            var avgX = result.Average(t => (t.X1 + t.X2 + t.X3) / 3.0);
            var avgZ = result.Average(t => t.CalculatedZ);
            Assert.AreEqual(centerBefore.x, avgX, 15.0);
            Assert.AreEqual(centerBefore.z, avgZ, 15.0);
        }
        */
    }
}
