using CommonUtilities.CommonSetup;

namespace _3DSpesificsUnitTests.Controls;

[TestClass]
public class MotherShipDifficultySetupTests
{
    [TestMethod]
    public void HigherAggression_MakesMothershipsFasterAndMoreAccurate()
    {
        float aggression = 1.5f;

        Assert.IsTrue(MotherShipDifficultySetup.ScaleTurnSpeed(45f, aggression) > 45f);
        Assert.IsTrue(MotherShipDifficultySetup.ScaleTravelSpeed(120f, aggression) > 120f);
        Assert.IsTrue(MotherShipDifficultySetup.ScaleCooldown(5f, aggression) < 5f);
        Assert.IsTrue(MotherShipDifficultySetup.ScaleUpdateInterval(0.5f, aggression) < 0.5f);
        Assert.IsTrue(MotherShipDifficultySetup.ScaleChargeWindow(2f, aggression) < 2f);
    }

    [TestMethod]
    public void Aggression_IsClampedToPlayableRange()
    {
        Assert.AreEqual(
            MotherShipDifficultySetup.MinAggression,
            MotherShipDifficultySetup.GetAggression(0.01f));

        Assert.AreEqual(
            MotherShipDifficultySetup.MaxAggression,
            MotherShipDifficultySetup.GetAggression(99f));
    }
}
