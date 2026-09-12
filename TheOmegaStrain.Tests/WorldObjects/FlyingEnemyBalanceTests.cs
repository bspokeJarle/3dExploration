using TheOmegaStrain.Common.CommonGlobalState;
using TheOmegaStrain.Domain;
using TheOmegaStrain.Game.Scenes.Scene1;
using TheOmegaStrain.Game.Scenes.Scene2;
using TheOmegaStrain.Game.Scenes.Scene3;
using TheOmegaStrain.Game.Scenes.Scene4;
using TheOmegaStrain.Game.Scenes.Scene5;
using TheOmegaStrain.Game.Scenes.Scene6;
using TheOmegaStrain.Game.Scenes.Scene7;
using TheOmegaStrain.Game.Scenes.Scene8;
using TheOmegaStrain.Game.Scenes.SceneSimulation;
using TheOmegaStrain.Game.World.Objects;
using TheOmegaStrain.Gameplay.Controls.KamikazeDroneControls;

namespace TheOmegaStrain.Tests.WorldObjects;

[TestClass]
public class FlyingEnemyBalanceTests
{
    [TestMethod]
    public void CreateKamikazeDrone_PassesSpeedMultiplierToIndependentController()
    {
        var slowerDrone = KamikazeDrone.CreateKamikazeDrone(null!, speedMultiplier: 0.90f);
        var fasterDrone = KamikazeDrone.CreateKamikazeDrone(null!, speedMultiplier: 1.20f);

        var slowerControls = (KamikazeDroneControls)slowerDrone.Movement!;
        var fasterControls = (KamikazeDroneControls)fasterDrone.Movement!;

        Assert.AreNotSame(slowerControls, fasterControls);
        Assert.AreEqual(0.90f, slowerControls.SpeedMultiplier, 0.001f);
        Assert.AreEqual(1.20f, fasterControls.SpeedMultiplier, 0.001f);
    }

    [TestMethod]
    public void CampaignScenes_UseExpectedDroneSpeedProgression()
    {
        Assert.AreEqual(0.90f, new Scene1().KamikazeDroneSpeedMultiplier, 0.001f);
        Assert.AreEqual(0.95f, new Scene2().KamikazeDroneSpeedMultiplier, 0.001f);
        Assert.AreEqual(0.975f, new Scene3().KamikazeDroneSpeedMultiplier, 0.001f);
        Assert.AreEqual(1.00f, new Scene4().KamikazeDroneSpeedMultiplier, 0.001f);
        Assert.AreEqual(1.00f, new Scene5().KamikazeDroneSpeedMultiplier, 0.001f);
        Assert.AreEqual(1.00f, new Scene6().KamikazeDroneSpeedMultiplier, 0.001f);
        Assert.AreEqual(1.00f, new Scene7().KamikazeDroneSpeedMultiplier, 0.001f);
        Assert.AreEqual(1.00f, new Scene8().KamikazeDroneSpeedMultiplier, 0.001f);
    }

    [DataTestMethod]
    [DataRow(0, 1.00f)]
    [DataRow(1, 1.02f)]
    [DataRow(5, 1.10f)]
    [DataRow(10, 1.20f)]
    [DataRow(50, 1.20f)]
    public void SimulationRound_IncreasesDroneSpeedByTwoPercentWithCap(int round, float expected)
    {
        int originalRound = GameState.GamePlayState.SimulationRound;
        try
        {
            GameState.GamePlayState.SimulationRound = round;
            Assert.AreEqual(expected, new SceneSimulation().KamikazeDroneSpeedMultiplier, 0.001f);
        }
        finally
        {
            GameState.GamePlayState.SimulationRound = originalRound;
        }
    }

    [TestMethod]
    public void DroneMovementSpeed_UsesConfiguredMultiplier()
    {
        var ninetyPercent = new KamikazeDroneControls(0.90f);
        var fullSpeed = new KamikazeDroneControls(1.00f);

        Assert.AreEqual(
            fullSpeed.MovementSpeedPerSecond * 0.90f,
            ninetyPercent.MovementSpeedPerSecond,
            0.001f);
    }

    [TestMethod]
    public void SpaceSwanCrashBoxes_AreFifteenPercentLargerThanPreviousBounds()
    {
        const float previousCrashboxScale = 1.5f;
        const float expectedIncrease = 1.15f;
        var noseBox = SpaceSwan.SwanCrashBoxes()![0];

        float width = noseBox.Max(point => point.x) - noseBox.Min(point => point.x);
        float authoredNoseWidth = 34f - 12f;

        Assert.AreEqual(
            authoredNoseWidth * previousCrashboxScale * expectedIncrease,
            width,
            0.001f);
    }
}
