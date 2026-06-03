using _3dTesting.MainWindowClasses;
using Domain;

namespace _3DSpesificsUnitTests.SceneManagement;

[TestClass]
public class OverlayLayoutTests
{
    [TestMethod]
    public void CalculatePanelHeight_TopAnchoredOverlayStaysInsideScreenAfterYOffset()
    {
        const double screenHeight = 1080.0;
        double yOffset = screenHeight * 0.18;

        double panelHeight = OverlayHandler.CalculatePanelHeight(
            desiredHeight: 1400.0,
            screenHeight,
            yOffset,
            ScreenOverlayAnchor.Top);

        Assert.IsTrue(yOffset + panelHeight <= screenHeight,
            "Top-anchored overlay panel should not extend below the screen after applying its top offset.");
        Assert.IsTrue(panelHeight < screenHeight * 0.90,
            "The height cap should account for the top offset, not only the total screen height.");
    }
}
