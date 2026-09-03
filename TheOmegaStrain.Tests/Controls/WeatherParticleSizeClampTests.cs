using TheOmegaStrain.Gameplay.Controls.Weather;

namespace TheOmegaStrain.Tests.Controls;

[TestClass]
public class WeatherParticleSizeClampTests
{
    [TestMethod]
    public void ClampApparentSize_LeavesDistantParticlesUntouched()
    {
        // A small projection scale means the particle is far away and already tiny on screen.
        float worldSize = 2f;
        float scale = 0.1f;

        float clamped = WorldWeatherField.ClampApparentSize(worldSize, scale, maxApparentSize: 4.5f);

        Assert.AreEqual(worldSize, clamped, 0.0001f);
    }

    [TestMethod]
    public void ClampApparentSize_LimitsOnScreenSizeWhenFlyingIntoParticle()
    {
        float worldSize = 2f;
        float scale = 8f;
        float maxApparentSize = 4.5f;

        float clamped = WorldWeatherField.ClampApparentSize(worldSize, scale, maxApparentSize);

        Assert.IsTrue(clamped < worldSize, "Close particles must shrink in world space.");
        Assert.AreEqual(maxApparentSize, clamped * scale, 0.0001f);
    }

    [TestMethod]
    public void ClampApparentSize_IgnoresInvalidInput()
    {
        Assert.AreEqual(3f, WorldWeatherField.ClampApparentSize(3f, scale: 8f, maxApparentSize: 0f), 0.0001f);
        Assert.AreEqual(3f, WorldWeatherField.ClampApparentSize(3f, scale: 0f, maxApparentSize: 4.5f), 0.0001f);
    }

    [TestMethod]
    public void GetApparentSizeShrink_IsNeutralForDistantParticles()
    {
        float shrink = WorldWeatherField.GetApparentSizeShrink(worldSize: 2f, scale: 0.1f, maxApparentSize: 34f);

        Assert.AreEqual(1f, shrink, 0.0001f);
    }

    [TestMethod]
    public void GetApparentSizeShrink_ScalesEveryDimensionEqually()
    {
        // A slanted raindrop must keep its shape: clamping length alone flattens the streak.
        float length = 20f;
        float halfWidth = 0.6f;
        float slant = 7f;
        float scale = 8f;
        float maxApparentLength = 34f;

        float shrink = WorldWeatherField.GetApparentSizeShrink(length, scale, maxApparentLength);

        Assert.IsTrue(shrink < 1f, "A close drop must shrink.");
        Assert.AreEqual(maxApparentLength, length * shrink * scale, 0.001f);

        // Proportions preserved.
        Assert.AreEqual(length / halfWidth, (length * shrink) / (halfWidth * shrink), 0.001f);
        Assert.AreEqual(slant / length, (slant * shrink) / (length * shrink), 0.001f);
    }
}
