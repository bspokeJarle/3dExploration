using TheOmegaStrain.Common.GamePlayHelpers;

namespace TheOmegaStrain.Tests.CommonHelpers;

[TestClass]
public class WeatherFlashColorHelpersTests
{
    [TestMethod]
    public void GetBackgroundColor_IsBlackWithoutFlash()
    {
        var (r, g, b) = WeatherFlashColorHelpers.GetBackgroundColor(0f, 0f);

        Assert.AreEqual(0, r);
        Assert.AreEqual(0, g);
        Assert.AreEqual(0, b);
    }

    [TestMethod]
    public void GetBackgroundColor_LightningIsBlueDominant()
    {
        var (r, g, b) = WeatherFlashColorHelpers.GetBackgroundColor(lightning: 1f, impact: 0f);

        Assert.IsTrue(b > g && g > r, $"Lightning flash should be blue dominant, got {r},{g},{b}.");
        Assert.IsTrue(b > 0, "Lightning must brighten the sky.");
    }

    [TestMethod]
    public void GetBackgroundColor_ImpactIsRedDominant()
    {
        var (r, g, b) = WeatherFlashColorHelpers.GetBackgroundColor(lightning: 0f, impact: 1f);

        Assert.IsTrue(r > g && g > b, $"Impact flash should be red dominant, got {r},{g},{b}.");
    }

    [TestMethod]
    public void GetBackgroundColor_BrightensWithIntensity()
    {
        var dim = WeatherFlashColorHelpers.GetBackgroundColor(lightning: 0.25f, impact: 0f);
        var bright = WeatherFlashColorHelpers.GetBackgroundColor(lightning: 1f, impact: 0f);

        Assert.IsTrue(bright.Blue > dim.Blue, "Stronger lightning must produce a brighter sky.");
    }
}
