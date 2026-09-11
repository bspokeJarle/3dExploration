using TheOmegaStrain.Game.World.Objects;
using TheOmegaStrain.Domain;

namespace TheOmegaStrain.Tests.WorldObjects;

[TestClass]
public class SeederGuideTests
{
    [TestMethod]
    public void ParticleGuides_AreCenteredWithStartClearOfUnderside()
    {
        var start = GetCentroid(Seeder.ParticlesStartGuide()![0]);
        var guide = GetCentroid(Seeder.ParticlesDirectionGuide()![0]);

        Assert.AreEqual(0f, start.x, 0.001f, "Seeder particle start guide should be centered on local X.");
        Assert.AreEqual(0f, start.y, 0.001f, "Seeder particle start guide should be centered on local Y.");
        Assert.AreEqual(-38f, start.z, 0.001f, "Seeder particle start guide should sit below the bottom center module.");

        Assert.AreEqual(start.x, guide.x, 0.001f, "Seeder particle end guide should stay centered on local X.");
        Assert.AreEqual(start.y, guide.y, 0.001f, "Seeder particle end guide should stay centered on local Y.");
        Assert.AreEqual(-133f, guide.z, 0.001f, "Seeder particle direction guide should retain its established position.");
        Assert.IsTrue(guide.z < start.z, "Seeder particle direction guide should remain below the start guide.");
    }

    [TestMethod]
    public void ParticleGuides_ArePointAnchorsOnSeederCenterLine()
    {
        var start = Seeder.ParticlesStartGuide()![0];
        var guide = Seeder.ParticlesDirectionGuide()![0];

        AssertPointAnchor(start, -38f);
        AssertPointAnchor(guide, -133f);
    }

    [TestMethod]
    public void CreatedSeeder_StartGuideIsAtScaledVisualBottomCenter()
    {
        var seeder = Seeder.CreateSeeder(null!);
        var start = seeder.ObjectParts.Single(p => p.PartName == "SeederParticlesStartGuide").Triangles![0];
        float visibleBottomZ = seeder.ObjectParts
            .Where(p => p.IsVisible && p.Triangles != null)
            .SelectMany(p => p.Triangles!)
            .SelectMany(t => new[] { t.vert1, t.vert2, t.vert3 })
            .Min(v => v.z);

        Assert.AreEqual(visibleBottomZ - 30f, start.vert1.z, 0.001f,
            "Scaled Seeder particle start should include the additional twenty-unit downward offset.");
        AssertPointAnchor(start, start.vert1.z);
    }

    private static void AssertPointAnchor(ITriangleMeshWithColorAndTexture triangle, float expectedZ)
    {
        AssertVertex(triangle.vert1, expectedZ);
        AssertVertex(triangle.vert2, expectedZ);
        AssertVertex(triangle.vert3, expectedZ);
    }

    private static void AssertVertex(IVector3 vertex, float expectedZ)
    {
        Assert.AreEqual(0f, vertex.x, 0.001f, "Seeder particle guide vertex should be centered on local X.");
        Assert.AreEqual(0f, vertex.y, 0.001f, "Seeder particle guide vertex should be centered on local Y.");
        Assert.AreEqual(expectedZ, vertex.z, 0.001f, "Seeder particle guide vertex should sit on the expected local Z.");
    }

    private static Vector3 GetCentroid(ITriangleMeshWithColorAndTexture triangle)
    {
        return new Vector3
        {
            x = (triangle.vert1.x + triangle.vert2.x + triangle.vert3.x) / 3f,
            y = (triangle.vert1.y + triangle.vert2.y + triangle.vert3.y) / 3f,
            z = (triangle.vert1.z + triangle.vert2.z + triangle.vert3.z) / 3f
        };
    }
}
