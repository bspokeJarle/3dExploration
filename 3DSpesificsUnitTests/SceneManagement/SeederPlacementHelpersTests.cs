using _3dRotations.Helpers;
using CommonUtilities.CommonSetup;
using static Domain._3dSpecificsImplementations;

namespace _3DSpesificsUnitTests.SceneManagement;

[TestClass]
public class SeederPlacementHelpersTests
{
    [TestMethod]
    public void CreateRingSeederPositions_KeepsSomeSeedersNearCenterAndSpreadsTheRest()
    {
        var center = new Vector3 { x = 95100f, y = 0, z = 95200f };

        var positions = SeederPlacementHelpers.CreateRingSeederPositions(
            count: 17,
            center: center,
            seed: 42,
            nearSeederCount: 5,
            firstRingRadius: 7500f,
            ringRadiusStep: 11500f);

        Assert.AreEqual(17, positions.Count);

        var distances = positions.Select(p => DistanceXZ(center, p)).ToList();
        Assert.IsTrue(distances.Take(5).All(d => d < 11000f * SurfaceSetup.WorldScale),
            "The first ring should keep several seeders close enough for immediate action.");
        Assert.IsTrue(distances.Skip(5).Any(d => d > 17000f * SurfaceSetup.WorldScale),
            "Outer ring seeders should be pushed further out instead of clustering near the platform.");
        Assert.IsTrue(positions.Select(p => $"{MathF.Round(p.x)}:{MathF.Round(p.z)}").Distinct().Count() == positions.Count,
            "Seeder positions should not collapse onto duplicate points.");
    }

    [TestMethod]
    public void CreateRandomSeederPositions_UsesRequestedRadiusBand()
    {
        var center = new Vector3 { x = 95100f, y = 0, z = 95200f };

        var positions = SeederPlacementHelpers.CreateRandomSeederPositions(
            count: 4,
            center: center,
            seed: 43,
            minRadius: 16000f,
            maxRadius: 42000f);

        Assert.AreEqual(4, positions.Count);
        Assert.IsTrue(positions.All(p =>
        {
            var distance = DistanceXZ(center, p);
            return distance >= 16000f * SurfaceSetup.WorldScale &&
                   distance <= 42000f * SurfaceSetup.WorldScale;
        }));
    }

    private static float DistanceXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return MathF.Sqrt((dx * dx) + (dz * dz));
    }
}
