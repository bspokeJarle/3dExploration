using TheOmegaStrain.Common.CommonGlobalState;
using TheOmegaStrain.Common.CommonSetup;
using TheOmegaStrain.Common.OmegaEngineAdapters;
using TheOmegaStrain.Domain;
using TheOmegaStrain.Game.World.Objects;

namespace TheOmegaStrain.Tests.OmegaEngineAdapters;

[TestClass]
public class FlyingObjectSurfaceClearanceHelpersTests
{
    [TestMethod]
    public void ClearanceState_HoldsLiftThenReleasesGraduallyAndRemainsIndependent()
    {
        var state = new FlyingObjectSurfaceClearanceState();
        var other = new FlyingObjectSurfaceClearanceState();
        Assert.AreEqual(100f, state.Update(100f, 0.01f));
        for (int i = 0; i < 19; i++)
            Assert.AreEqual(100f, state.Update(0f, 0.1f));
        Assert.AreEqual(0f, other.Update(0f, 0.1f));
        for (int i = 0; i < 4; i++) state.Update(0f, 0.1f);
        Assert.IsTrue(state.RetainedLift > 0f && state.RetainedLift < 100f);
        Assert.AreEqual(150f, state.Update(150f, 0.01f), "Higher terrain must lift immediately.");
    }

    [TestInitialize]
    public void Setup()
    {
        OmegaWorldViewSetup.ConfigurePitch(63f);
        GameState.SurfaceState = new SurfaceState
        {
            GlobalMapPosition = new Vector3(),
            SurfaceViewportObject = new OmegaObject3D
            {
                ObjectId = 1,
                ObjectOffsets = new Vector3()
            }
        };
    }

    [DataTestMethod]
    [DataRow(63f)]
    [DataRow(70f)]
    public void ApplyMinimumClearance_UsesLocalRotatedTileAtBothCameraAngles(float pitchDegrees)
    {
        OmegaWorldViewSetup.ConfigurePitch(pitchDegrees);
        const float targetZ = 100f;
        float radians = pitchDegrees * MathF.PI / 180f;
        float projectedTileDepth = SurfaceSetup.tileSize * MathF.Sin(radians);
        float centreGroundY = targetZ * MathF.Cos(radians) / MathF.Sin(radians);
        float highestSampledGroundY = (targetZ - projectedTileDepth) *
            MathF.Cos(radians) / MathF.Sin(radians);
        var surface = CreateSurfacePlane(pitchDegrees);
        var drone = new OmegaObject3D
        {
            ObjectId = 2,
            ObjectName = "KamikazeDrone",
            IsOnScreen = true,
            WorldPosition = new Vector3 { x = 1f },
            ObjectOffsets = new Vector3 { z = targetZ, y = centreGroundY - 20f },
            ParentSurface = surface
        };

        bool applied = FlyingObjectSurfaceClearanceHelpers.ApplyMinimumClearance(drone);

        Assert.IsTrue(applied);
        Assert.AreEqual(
            highestSampledGroundY - TerrainAvoidanceSetup.KamikazeDroneMinimumSurfaceClearance,
            drone.ObjectOffsets.y,
            0.01f);
    }

    [TestMethod]
    public void ApplyMinimumClearance_DoesNotRestrictFreeFlightAboveFloor()
    {
        var surface = CreateSurfacePlane(70f);
        var drone = new OmegaObject3D
        {
            ObjectId = 3,
            ObjectName = "KamikazeDrone",
            IsOnScreen = true,
            WorldPosition = new Vector3 { x = 1f },
            ObjectOffsets = new Vector3 { z = 100f, y = -500f },
            ParentSurface = surface
        };
        float originalY = drone.ObjectOffsets.y;

        bool applied = FlyingObjectSurfaceClearanceHelpers.ApplyMinimumClearance(drone);

        Assert.IsFalse(applied);
        Assert.AreEqual(originalY, drone.ObjectOffsets.y);
    }

    [TestMethod]
    public void ApplyMinimumClearance_SamplesRisingTerrainAcrossObjectFootprint()
    {
        var surface = CreateSurfacePlane(63f);
        var drone = new OmegaObject3D
        {
            ObjectId = 4,
            ObjectName = "KamikazeDrone",
            IsOnScreen = true,
            WorldPosition = new Vector3 { x = 1f },
            ObjectOffsets = new Vector3 { z = 220f, y = -20f },
            ParentSurface = surface,
            ObjectParts = new List<I3dObjectPart>
            {
                new OmegaObjectPart3D
                {
                    IsVisible = true,
                    Triangles = new List<ITriangleMeshWithColorAndTexture>
                    {
                        new TriangleMeshWithColor
                        {
                            vert1 = new Vector3 { z = -60f },
                            vert2 = new Vector3 { x = 60f },
                            vert3 = new Vector3 { z = 60f }
                        }
                    }
                }
            }
        };

        bool applied = FlyingObjectSurfaceClearanceHelpers.ApplyMinimumClearance(drone);

        Assert.IsTrue(applied, "The highest ground under the body footprint must enforce the floor.");
    }

    [TestMethod]
    public void ApplyMinimumClearance_SamplesSurfaceTileInFrontAndBehind()
    {
        OmegaWorldViewSetup.ConfigurePitch(63f);
        int tileDepth = (int)MathF.Round(
            SurfaceSetup.tileSize * MathF.Sin(OmegaWorldViewSetup.SurfacePitchDegrees * MathF.PI / 180f));
        var surface = CreateThreeTileSurface(tileDepth);
        var drone = new OmegaObject3D
        {
            ObjectId = 5,
            ObjectName = "KamikazeDrone",
            IsOnScreen = true,
            WorldPosition = new Vector3 { x = 1f },
            ObjectOffsets = new Vector3 { z = tileDepth * 1.5f, y = 50f },
            ParentSurface = surface
        };

        bool applied = FlyingObjectSurfaceClearanceHelpers.ApplyMinimumClearance(drone);

        Assert.IsTrue(applied, "The raised adjacent tile must be detected before the drone centre reaches it.");
        Assert.AreEqual(-70f, drone.ObjectOffsets.y, 0.01f);
    }

    [TestMethod]
    public void ConfiguredFlyingEnemiesHaveIndependentClearances()
    {
        Assert.AreEqual(200f, TerrainAvoidanceSetup.GetMinimumSurfaceClearance("Seeder"));
        Assert.AreEqual(120f, TerrainAvoidanceSetup.GetMinimumSurfaceClearance("KamikazeDrone"));
        Assert.AreEqual(130f, TerrainAvoidanceSetup.GetMinimumSurfaceClearance("ZeppelinBomber"));
        Assert.AreEqual(25f, TerrainAvoidanceSetup.GetMinimumSurfaceClearance("MotherShipSmall"));
        Assert.AreEqual(105f, TerrainAvoidanceSetup.GetMinimumSurfaceClearance("MotherShipMedium"));
        Assert.AreEqual(75f, TerrainAvoidanceSetup.GetMinimumSurfaceClearance("MotherShipLarge"));
        Assert.AreEqual(0f, TerrainAvoidanceSetup.GetMinimumSurfaceClearance("SpaceSwan"));
    }

    private static Surface CreateSurfacePlane(float pitchDegrees)
    {
        float radians = pitchDegrees * MathF.PI / 180f;
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);

        Vector3 Rotate(float x, float z) => new(x, z * cos, z * sin);

        return new Surface
        {
            RotatedSurfaceTriangles = new List<ITriangleMeshWithColorAndTexture>
            {
                new TriangleMeshWithColor
                {
                    vert1 = Rotate(-500f, 0f),
                    vert2 = Rotate(500f, 0f),
                    vert3 = Rotate(500f, 500f)
                },
                new TriangleMeshWithColor
                {
                    vert1 = Rotate(-500f, 0f),
                    vert2 = Rotate(500f, 500f),
                    vert3 = Rotate(-500f, 500f)
                }
            }
        };
    }

    private static Surface CreateThreeTileSurface(float tileDepth)
    {
        var triangles = new List<ITriangleMeshWithColorAndTexture>();
        AddTile(0f, tileDepth, 200f);
        AddTile(tileDepth, tileDepth * 2f, 200f);
        AddTile(tileDepth * 2f, tileDepth * 3f, 50f);
        return new Surface { RotatedSurfaceTriangles = triangles };

        void AddTile(float nearZ, float farZ, float groundY)
        {
            triangles.Add(new TriangleMeshWithColor
            {
                vert1 = new Vector3(-500f, groundY, nearZ),
                vert2 = new Vector3(500f, groundY, nearZ),
                vert3 = new Vector3(500f, groundY, farZ)
            });
            triangles.Add(new TriangleMeshWithColor
            {
                vert1 = new Vector3(-500f, groundY, nearZ),
                vert2 = new Vector3(500f, groundY, farZ),
                vert3 = new Vector3(-500f, groundY, farZ)
            });
        }
    }
}
