using TheOmegaStrain.Common.OmegaEngineAdapters;
using TheOmegaStrain.Common.CommonGlobalState;
using TheOmegaStrain.Common.CommonSetup;
using TheOmegaStrain.Domain;

namespace TheOmegaStrain.Tests.OmegaEngineAdapters;

[TestClass]
public class SurfacePositionSyncHelpersTests
{
    [TestInitialize]
    public void Setup()
    {
        ScreenSetup.Initialize(1500, 1024);
    }

    [TestMethod]
    public void GetMinimapMarkerWorldPosition_UsesSurfaceViewportCenterAndObjectOffset()
    {
        var obj = CreateObject(worldX: 250f, worldZ: 2000f, offsetX: 25f);
        int viewportCenterOffset = (SurfaceSetup.viewPortSize * SurfaceSetup.tileSize) / 2;

        var markerWorld = SurfacePositionSyncHelpers.GetMinimapMarkerWorldPosition(obj);

        Assert.IsNotNull(markerWorld);
        Assert.AreEqual(250f + viewportCenterOffset + 25f, markerWorld.x, 0.1f);
        Assert.AreEqual(2000f, markerWorld.z, 0.1f);
    }

    [TestMethod]
    public void GetGuidanceTargetWorldPosition_UsesShipNavigationCenterAndObjectOffset()
    {
        var obj = CreateObject(worldX: 250f, worldZ: 2000f, offsetX: 25f);

        var guidanceWorld = SurfacePositionSyncHelpers.GetGuidanceTargetWorldPosition(obj);

        Assert.IsNotNull(guidanceWorld);
        Assert.AreEqual(250f + ScreenSetup.screenSizeX / 2f + 25f, guidanceWorld.x, 0.1f);
        Assert.AreEqual(2000f, guidanceWorld.z, 0.1f);
    }

    [DataTestMethod]
    [DataRow(56f)]
    [DataRow(63f)]
    [DataRow(70f)]
    public void GetSurfacePitchHeightCorrectionY_MatchesDifferenceFromOriginalCalibration(float pitchDegrees)
    {
        const float surfaceLocalZ = 400f;
        float radians = pitchDegrees * (MathF.PI / 180f);
        float originalRadians = OmegaWorldViewSetup.OriginalWorldPitchDegrees * (MathF.PI / 180f);
        float expectedY = surfaceLocalZ * ((1f / MathF.Tan(radians)) - (1f / MathF.Tan(originalRadians)));

        float correction = SurfacePositionSyncHelpers.GetSurfacePitchHeightCorrectionY(surfaceLocalZ, pitchDegrees);

        Assert.AreEqual(expectedY, correction, 0.001f);
    }

    [TestMethod]
    public void GetSurfacePitchHeightCorrectionY_IsZeroAtOriginalCameraAngle()
    {
        float correction = SurfacePositionSyncHelpers.GetSurfacePitchHeightCorrectionY(
            1500f,
            OmegaWorldViewSetup.OriginalWorldPitchDegrees);

        Assert.AreEqual(0f, correction, 0.001f);
    }

    [DataTestMethod]
    [DataRow(56f)]
    [DataRow(63f)]
    [DataRow(70f)]
    public void GetSurfacePitchHeightCorrectionY_IsZeroAtCentreAndOppositeAtFrontAndBack(float pitchDegrees)
    {
        const float localDistance = 300f;

        float centre = SurfacePositionSyncHelpers.GetSurfacePitchHeightCorrectionY(0f, pitchDegrees);
        float back = SurfacePositionSyncHelpers.GetSurfacePitchHeightCorrectionY(localDistance, pitchDegrees);
        float front = SurfacePositionSyncHelpers.GetSurfacePitchHeightCorrectionY(-localDistance, pitchDegrees);

        Assert.AreEqual(0f, centre, 0.001f);
        Assert.AreEqual(-front, back, 0.001f);
    }

    [TestMethod]
    public void GetSurfacePitchHeightCorrectionY_ForObject_UsesPositionRelativeToSurfaceViewport()
    {
        GameState.SurfaceState.GlobalMapPosition = new Vector3 { z = 1000f };
        GameState.SurfaceState.SurfaceViewportObject = new OmegaObject3D
        {
            ObjectId = 2,
            ObjectOffsets = new Vector3 { z = 25f }
        };
        var obj = CreateObject(worldX: 0f, worldZ: 700f, offsetX: 0f);
        obj.ObjectOffsets.z = 15f;

        float correction = SurfacePositionSyncHelpers.GetSurfacePitchHeightCorrectionY(obj, 63f);

        float expectedSurfaceLocalZ = (1000f - 700f) + 15f - 25f;
        float currentRadians = 63f * MathF.PI / 180f;
        float originalRadians = OmegaWorldViewSetup.OriginalWorldPitchDegrees * MathF.PI / 180f;
        float expected = expectedSurfaceLocalZ
                         * ((1f / MathF.Tan(currentRadians)) - (1f / MathF.Tan(originalRadians)));
        Assert.AreEqual(expected, correction, 0.001f);
    }

    private static OmegaObject3D CreateObject(float worldX, float worldZ, float offsetX)
    {
        return new OmegaObject3D
        {
            ObjectId = 1,
            ObjectName = "Seeder",
            WorldPosition = new Vector3 { x = worldX, y = 0f, z = worldZ },
            ObjectOffsets = new Vector3 { x = offsetX, y = 0f, z = 0f },
            Rotation = new Vector3(),
            ImpactStatus = new ImpactStatus()
        };
    }
}
