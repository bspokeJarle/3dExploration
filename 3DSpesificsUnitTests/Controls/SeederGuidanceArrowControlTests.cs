using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.CommonSetup;
using Domain;
using GameAiAndControls.Controls;
using System.Threading;
using static Domain._3dSpecificsImplementations;

namespace _3DSpesificsUnitTests.Controls;

[TestClass]
public class SeederGuidanceArrowControlTests
{
    [TestInitialize]
    public void Setup()
    {
        ScreenSetup.Initialize(1500, 1024);
        GameState.GamePlayState = new GamePlayState();
        GameState.SurfaceState = new SurfaceState();
        GameState.ShipState = new ShipState();
        GameState.ShipState.ShipWorldPosition = new Vector3 { x = 0, y = 0, z = 0 };
    }

    [TestMethod]
    public void MoveObject_PrioritizesSeeder_WhenSeederAndBomberExist()
    {
        var control = new SeederGuidanceArrowControl();
        var arrow = CreateArrow();

        // Seeder is farther away in XZ than bomber, but must still be prioritized.
        GameState.SurfaceState.AiObjects.Add(CreateAi("Seeder", 1000f, 0f, 0f, isActive: true));
        GameState.SurfaceState.AiObjects.Add(CreateAi("ZeppelinBomber", 0f, 0f, 100f, isActive: true));

        control.MoveObject(arrow, null, null);
        Thread.Sleep(20);
        control.MoveObject(arrow, null, null);

        // Bomber heading would keep arrow at ~90, while seeder heading drives it down toward 0.
        Assert.IsTrue(arrow.Rotation!.z < 85f,
            "Arrow should prioritize seeder while any seeder is alive.");
    }

    [TestMethod]
    public void MoveObject_FallsBackToBomber_WhenNoSeederOrMothershipOrActiveDrone()
    {
        var control = new SeederGuidanceArrowControl();
        var arrow = CreateArrow();

        // Bomber straight ahead in +Z from origin; heading should be +90.
        GameState.SurfaceState.AiObjects.Add(CreateAi("ZeppelinBomber", 0f, 0f, 1000f, isActive: true));
        // Inactive drone should not be considered.
        GameState.SurfaceState.AiObjects.Add(CreateAi("KamikazeDrone", 1000f, 0f, 0f, isActive: false));

        control.MoveObject(arrow, null, null);

        Assert.AreEqual(90f, arrow.Rotation!.z, 0.1f,
            "Arrow should point to bomber when it is the remaining objective enemy.");
    }

    [TestMethod]
    public void MoveObject_AnchorsArrowBelowGameOverlay_OnLowHeightScreen()
    {
        ScreenSetup.Initialize(1920, 1080);
        var control = new SeederGuidanceArrowControl();
        var arrow = CreateArrow();
        arrow.ObjectOffsets!.y = -200f;

        control.MoveObject(arrow, null, null);

        float screenY = ScreenSetup.screenSizeY / 2f + arrow.ObjectOffsets.y;
        Assert.IsTrue(screenY >= GameOverlaySetup.GuidanceArrowMinimumScreenY,
            "Guidance arrow should stay below the top HUD overlay on lower-height screens.");
    }

    [TestMethod]
    public void MoveObject_KeepsPreferredArrowY_WhenAlreadyBelowGameOverlay()
    {
        ScreenSetup.Initialize(2256, 1504);
        var control = new SeederGuidanceArrowControl();
        var arrow = CreateArrow();
        arrow.ObjectOffsets!.y = -200f;

        control.MoveObject(arrow, null, null);

        Assert.AreEqual(-200f, arrow.ObjectOffsets.y, 0.1f,
            "Guidance arrow should keep the scene's preferred placement when it is already below the HUD.");
    }

    private static _3dObject CreateArrow()
    {
        return new _3dObject
        {
            ObjectId = 1001,
            ObjectName = "SeederGuidanceArrow",
            Rotation = new Vector3 { x = WorldViewSetup.SurfaceFacingObjectPitchDegrees, y = 0f, z = 90f },
            WorldPosition = new Vector3(),
            ObjectOffsets = new Vector3(),
            ImpactStatus = new ImpactStatus()
        };
    }

    private static _3dObject CreateAi(string name, float x, float y, float z, bool isActive)
    {
        return new _3dObject
        {
            ObjectId = ++GameState.ObjectIdCounter,
            ObjectName = name,
            IsActive = isActive,
            WorldPosition = new Vector3 { x = x, y = y, z = z },
            ObjectOffsets = new Vector3(),
            Rotation = new Vector3(),
            ImpactStatus = new ImpactStatus { HasExploded = false }
        };
    }
}
