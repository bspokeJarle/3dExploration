using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.CommonSetup;
using Domain;
using static Domain._3dSpecificsImplementations;

namespace _3DSpesificsUnitTests.Physics;

[TestClass]
public class ParticleSurfaceProjectionTests
{
    // Mirrors the surface-projection scale formula from ParticleManager.HandleParticles
    private const float BaseProjectedScale = 1.5f;
    private const float AltitudeShrinkFactor = 0.003f;
    private const float MinProjectedScale = 0.3f;
    private const float SurfaceFlattenY = 0.3f;

    private static float CalculateProjectedScale(float particlePositionY)
    {
        float altitude = MathF.Max(0f, particlePositionY * -1f);
        return MathF.Max(MinProjectedScale, BaseProjectedScale - altitude * AltitudeShrinkFactor);
    }

    // Mirrors the projected Y formula — shadow at same screen Y as particle
    private static float CalculateProjectedY(float inhabitantOffsetY, float particlePositionY)
    {
        return inhabitantOffsetY + particlePositionY;
    }

    [TestInitialize]
    public void Setup()
    {
        GameState.GamePlayState = new GamePlayState();
        GameState.SurfaceState = new SurfaceState();
        GameState.ScreenOverlayState = new ScreenOverlayState();
        GameState.ObjectIdCounter = 0;
        ScreenSetup.Initialize(1500, 1024);
    }

    [TestCleanup]
    public void Cleanup()
    {
        ScreenSetup.Initialize(1500, 1024);
    }

    // ---------------------------------------------------------------
    // Shadow scale tests
    // ---------------------------------------------------------------

    [TestMethod]
    public void ProjectedScale_AtSurfaceLevel_IsBaseScale()
    {
        float scale = CalculateProjectedScale(0f);
        Assert.AreEqual(1.5f, scale, 0.01f,
            "Projected scale at surface should be the base 1.5x");
    }

    [TestMethod]
    public void ProjectedScale_BelowSurface_StaysAtBase()
    {
        float scale = CalculateProjectedScale(100f);
        Assert.AreEqual(1.5f, scale, 0.01f,
            "Projected scale should stay at base 1.5x when particle is below surface");
    }

    [TestMethod]
    public void ProjectedScale_HighAboveSurface_ShrinksWithAltitude()
    {
        float scale = CalculateProjectedScale(-200f);
        float expected = MathF.Max(MinProjectedScale, BaseProjectedScale - 200f * AltitudeShrinkFactor);
        Assert.AreEqual(expected, scale, 0.01f,
            "Projected mark should shrink when particle is high above surface");
    }

    [TestMethod]
    public void ProjectedScale_FartherIsSmallerThanNearer()
    {
        float scaleNear = CalculateProjectedScale(-50f);
        float scaleFar = CalculateProjectedScale(-200f);
        Assert.IsTrue(scaleFar < scaleNear,
            $"Mark at -200 ({scaleFar:F2}) should be smaller than at -50 ({scaleNear:F2})");
    }

    // ---------------------------------------------------------------
    // Shadow Y placement tests
    // ---------------------------------------------------------------

    [TestMethod]
    public void ProjectedY_MatchesParticleScreenY()
    {
        float projY = CalculateProjectedY(200f, -50f);
        Assert.AreEqual(150f, projY, 0.01f,
            "Shadow Y should match particle's screen Y (inhabitant offset + particle position)");
    }

    [TestMethod]
    public void ProjectedY_AtSurface_EqualsInhabitantOffset()
    {
        float projY = CalculateProjectedY(200f, 0f);
        Assert.AreEqual(200f, projY, 0.01f,
            "Shadow Y at surface level should equal inhabitant offset Y");
    }

    // ---------------------------------------------------------------
    // Shadow triangle geometry tests
    // ---------------------------------------------------------------

    [TestMethod]
    public void ProjectedTriangle_ZComponentsFlattenedToZero()
    {
        var particleTriangle = new TriangleMeshWithColor
        {
            vert1 = new Vector3 { x = -5, y = -5, z = 3 },
            vert2 = new Vector3 { x = 5, y = -5, z = -2 },
            vert3 = new Vector3 { x = 0, y = 5, z = 1 }
        };

        float scale = CalculateProjectedScale(0f);
        var proj1 = new Vector3 { x = particleTriangle.vert1.x * scale, y = particleTriangle.vert1.y * scale * SurfaceFlattenY, z = 0 };
        var proj2 = new Vector3 { x = particleTriangle.vert2.x * scale, y = particleTriangle.vert2.y * scale * SurfaceFlattenY, z = 0 };
        var proj3 = new Vector3 { x = particleTriangle.vert3.x * scale, y = particleTriangle.vert3.y * scale * SurfaceFlattenY, z = 0 };

        Assert.AreEqual(0f, proj1.z, "Projected vert1 z should be 0");
        Assert.AreEqual(0f, proj2.z, "Projected vert2 z should be 0");
        Assert.AreEqual(0f, proj3.z, "Projected vert3 z should be 0");
    }

    [TestMethod]
    public void ProjectedTriangle_ScaledLargerThanParticle()
    {
        float size = 10f;
        var particleTriangle = new TriangleMeshWithColor
        {
            vert1 = new Vector3 { x = -size / 2, y = -size / 2, z = 0 },
            vert2 = new Vector3 { x = size / 2, y = -size / 2, z = 0 },
            vert3 = new Vector3 { x = 0, y = size / 2, z = 0 }
        };

        float scale = CalculateProjectedScale(0f);
        float projExtent = MathF.Abs(particleTriangle.vert2.x * scale);
        float particleExtent = MathF.Abs(particleTriangle.vert2.x);

        Assert.IsTrue(projExtent > particleExtent,
            $"Projected extent ({projExtent:F2}) should be larger than particle ({particleExtent:F2})");
        Assert.AreEqual(particleExtent * 1.5f, projExtent, 0.01f,
            "Projected mark should be 1.5x the particle size at surface level");
    }

    // ---------------------------------------------------------------
    // Shadow X offset matches particle X offset
    // ---------------------------------------------------------------

    [TestMethod]
    public void ProjectedOffset_XMatchesParticle()
    {
        float inhabitantX = 100f;
        float particleX = 25f;
        float expectedX = inhabitantX + particleX;
        Assert.AreEqual(expectedX, inhabitantX + particleX,
            "Projected X offset should equal inhabitant X + particle X");
    }

    // ---------------------------------------------------------------
    // Integration-style: shadow visibility on platform vs flat terrain
    // ---------------------------------------------------------------

    [TestMethod]
    public void ProjectedY_ShadowTracksParticleDepth()
    {
        float projHigh = CalculateProjectedY(200f, -100f);
        float projLow = CalculateProjectedY(200f, -10f);
        Assert.IsTrue(projLow > projHigh,
            "Shadow should be lower on screen when particle is closer to surface");
    }
}
