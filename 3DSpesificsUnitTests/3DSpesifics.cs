using static Domain._3dSpecificsImplementations;
using _3dTesting.Helpers;

namespace _3DSpesificsUnitTests
{
    [TestClass]
    public class UnitTest
    {
        [TestClass]
        public class CrashBoxCenteringTests
        {
             [TestMethod]
            public void CenterCrashBoxAt_AdjustsMinYToTarget()
            {
                // Arrange: Eksempeldata fra loggen
                var crashBox = new List<Vector3>
            {
                new Vector3 { x = 95078, y = 420.3879f, z = 93485.42f },
                new Vector3 { x = 95072, y = 424.3879f, z = 93477.73f }
            };

                var targetPosition = new Vector3 { x = 95075, y = -298.34863f, z = 93481.57f };

                // Act
                ObjectPlacementHelpers.CenterCrashBoxAt(crashBox, targetPosition);

                // Assert: Sjekk at minste Y nå er ca lik target
                float actualMinY = float.MaxValue;
                foreach (var point in crashBox)
                    if (point.y < actualMinY)
                        actualMinY = point.y;

                Assert.IsTrue(System.Math.Abs(actualMinY - targetPosition.y) < 0.001,
                    $"MinY etter sentrering var {actualMinY}, forventet ca {targetPosition.y}");
            }
        }

        [TestMethod]
        public void ShouldDetectCollision_WhenShipOverlapsHouse()
        {
            var shipCrashBox = new List<Vector3>
            {
                new Vector3 { x = 94935, y = -124.75f, z = 93372.73f },
                new Vector3 { x = 95065, y = -124.75f, z = 93372.73f },
                new Vector3 { x = 95065, y = -20.49f,  z = 93372.73f },
                new Vector3 { x = 94935, y = -20.49f,  z = 93372.73f },

                new Vector3 { x = 94935, y = -124.75f, z = 93445.73f },
                new Vector3 { x = 95065, y = -124.75f, z = 93445.73f },
                new Vector3 { x = 95065, y = -20.49f,  z = 93445.73f },
                new Vector3 { x = 94935, y = -20.49f,  z = 93445.73f },
            };

            var houseCrashBox = new List<Vector3>
            {
                new Vector3 { x = 94935, y = -124.73f, z = 93309.80f },
                new Vector3 { x = 95065, y = -124.73f, z = 93309.80f },
                new Vector3 { x = 95065, y = -20.47f,  z = 93309.80f },
                new Vector3 { x = 94935, y = -20.47f,  z = 93309.80f },

                new Vector3 { x = 94935, y = -124.73f, z = 93382.80f },
                new Vector3 { x = 95065, y = -124.73f, z = 93382.80f },
                new Vector3 { x = 95065, y = -20.47f,  z = 93382.80f },
                new Vector3 { x = 94935, y = -20.47f,  z = 93382.80f },
            };
            bool isColliding = _3dObjectHelpers.CheckCollisionBoxVsBox(shipCrashBox, houseCrashBox);

            Assert.IsTrue(isColliding, "Expected collision between Ship and House based on overlapping dimensions, but none was detected.");
        }


        [TestMethod]
        public void ShouldNotDetectCollision_WhenBoxesAreFarApart()
        {
            var crashBoxA = new List<Vector3>
            {
                new Vector3 { x = 0, y = 0, z = 0 },
                new Vector3 { x = 10, y = 0, z = 0 },
                new Vector3 { x = 10, y = 10, z = 0 },
                new Vector3 { x = 0, y = 10, z = 0 },
                new Vector3 { x = 0, y = 0, z = 10 },
                new Vector3 { x = 10, y = 0, z = 10 },
                new Vector3 { x = 10, y = 10, z = 10 },
                new Vector3 { x = 0, y = 10, z = 10 },
            };

            var crashBoxB = new List<Vector3>
            {
                new Vector3 { x = 100, y = 100, z = 100 },
                new Vector3 { x = 110, y = 100, z = 100 },
                new Vector3 { x = 110, y = 110, z = 100 },
                new Vector3 { x = 100, y = 110, z = 100 },
                new Vector3 { x = 100, y = 100, z = 110 },
                new Vector3 { x = 110, y = 100, z = 110 },
                new Vector3 { x = 110, y = 110, z = 110 },
                new Vector3 { x = 100, y = 110, z = 110 },
            };

            bool isColliding = _3dObjectHelpers.CheckCollisionBoxVsBox(crashBoxA, crashBoxB);

            Assert.IsFalse(isColliding, "Expected no collision between distant boxes, but one was detected.");
        }
    }
}
