using TheOmegaStrain.Common.CommonGlobalState;
using TheOmegaStrain.Common.CommonGlobalState.States;
using TheOmegaStrain.Domain;
using TheOmegaStrain.Game.World.Objects;

namespace TheOmegaStrain.Tests.WorldObjects;

[TestClass]
public class MotherShipDetailGeometryTests
{
    [TestInitialize]
    public void Setup()
    {
        GameState.SurfaceState = new SurfaceState();
        GameState.ObjectIdCounter = 0;
    }

    [TestMethod]
    public void MotherShipVariants_ExposeVisibleDetailGeometry()
    {
        AssertVisibleDetailParts(
            MotherShipSmall.CreateMotherShipSmall(null!),
            "MotherShipArmorDetailPanels",
            "MotherShipWingInsetPanels",
            "MotherShipDorsalLowRidges");

        AssertVisibleDetailParts(
            MotherShipMedium.CreateMotherShipMedium(null!),
            "DorsalArmorDetails",
            "CannonInsetPanels",
            "PodSignalLights");

        AssertVisibleDetailParts(
            MotherShipLarge.CreateMotherShipLarge(null!),
            "CarrierSurfaceDetails",
            "BroadsideArmorFacets",
            "EngineGlowDetails");
    }

    private static void AssertVisibleDetailParts(OmegaObject3D ship, params string[] partNames)
    {
        foreach (string partName in partNames)
        {
            var part = ship.ObjectParts.SingleOrDefault(p => p.PartName == partName);

            Assert.IsNotNull(part, $"{ship.ObjectName} should expose detail part '{partName}'.");
            Assert.IsTrue(part.IsVisible, $"{partName} should be visible.");
            Assert.IsTrue(part.Triangles.Count > 0, $"{partName} should contain visible triangles.");

            foreach (var triangle in part.Triangles)
            {
                Assert.IsTrue(
                    TriangleAreaSquared(triangle) > 0.001f,
                    $"{partName} contains a degenerate triangle.");
            }
        }
    }

    private static float TriangleAreaSquared(ITriangleMeshWithColorAndTexture triangle)
    {
        var ax = triangle.vert2.x - triangle.vert1.x;
        var ay = triangle.vert2.y - triangle.vert1.y;
        var az = triangle.vert2.z - triangle.vert1.z;

        var bx = triangle.vert3.x - triangle.vert1.x;
        var by = triangle.vert3.y - triangle.vert1.y;
        var bz = triangle.vert3.z - triangle.vert1.z;

        var cx = ay * bz - az * by;
        var cy = az * bx - ax * bz;
        var cz = ax * by - ay * bx;

        return cx * cx + cy * cy + cz * cz;
    }
}
