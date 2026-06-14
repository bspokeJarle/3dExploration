using Domain;

namespace _3DSpesificsUnitTests.CommonGlobalState;

[TestClass]
public class ScreenOverlayChoiceTests
{
    [TestMethod]
    public void MoveChoiceSelection_UpdatesBodyAndWraps()
    {
        var overlay = new ScreenOverlayState();

        overlay.SetChoiceOptions(
            ScreenOverlayChoiceAction.PlanetLostRecovery,
            "Choose:",
            "CONTINUE",
            "RESET PLANET");

        Assert.AreEqual(0, overlay.SelectedChoiceIndex);
        StringAssert.Contains(overlay.Body, "> CONTINUE");
        StringAssert.Contains(overlay.Body, "  RESET PLANET");

        overlay.MoveChoiceSelection(1);

        Assert.AreEqual(1, overlay.SelectedChoiceIndex);
        StringAssert.Contains(overlay.Body, "  CONTINUE");
        StringAssert.Contains(overlay.Body, "> RESET PLANET");

        overlay.MoveChoiceSelection(1);

        Assert.AreEqual(0, overlay.SelectedChoiceIndex);
        StringAssert.Contains(overlay.Body, "> CONTINUE");
    }
}
