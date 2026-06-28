using CommonUtilities.CommonGlobalState;
using CommonUtilities.CommonGlobalState.States;
using CommonUtilities.Persistence;

namespace _3DSpesificsUnitTests.Persistence;

[TestClass]
public class GameSettingsPersistenceTests
{
    private string _originalLocalFolder = string.Empty;
    private string _testLocalFolder = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _originalLocalFolder = PersistenceSetup.LocalFolder;
        _testLocalFolder = Path.Combine(Path.GetTempPath(), "OmegaStrainSettingsPersistenceTests", Guid.NewGuid().ToString("N"));
        PersistenceSetup.LocalFolder = _testLocalFolder;
        PersistenceSetup.Initialize();
    }

    [TestCleanup]
    public void Cleanup()
    {
        PersistenceSetup.LocalFolder = _originalLocalFolder;
        try
        {
            if (Directory.Exists(_testLocalFolder))
                Directory.Delete(_testLocalFolder, recursive: true);
        }
        catch
        {
        }
    }

    [TestMethod]
    public void SaveAndLoadSettings_RoundTripsAudioAndGraphicsValues()
    {
        var settings = new GameSettingsState
        {
            MasterVolumePercent = 80,
            MusicVolumePercent = 70,
            EffectsVolumePercent = 60,
            VoiceVolumePercent = 50,
            GraphicsQuality = GraphicsQualityPreset.High,
            ParticleDensityPercent = 130,
            GlowEffectsEnabled = true,
            EnhancedWeatherEnabled = true,
            EnhancedShadowsEnabled = false
        };

        GameSettingsPersistence.SaveSettings(settings);
        var loaded = GameSettingsPersistence.LoadSettings();

        Assert.AreEqual(80, loaded.MasterVolumePercent);
        Assert.AreEqual(70, loaded.MusicVolumePercent);
        Assert.AreEqual(60, loaded.EffectsVolumePercent);
        Assert.AreEqual(50, loaded.VoiceVolumePercent);
        Assert.AreEqual(GraphicsQualityPreset.High, loaded.GraphicsQuality);
        Assert.AreEqual(130, loaded.ParticleDensityPercent);
        Assert.IsTrue(loaded.GlowEffectsEnabled);
        Assert.IsTrue(loaded.EnhancedWeatherEnabled);
        Assert.IsFalse(loaded.EnhancedShadowsEnabled);
    }

    [TestMethod]
    public void LoadIntoGameState_UsesDefaultSettingsWhenFileIsMissing()
    {
        GameState.SettingsState = new GameSettingsState { MasterVolumePercent = 10 };

        GameSettingsPersistence.LoadIntoGameState();

        Assert.AreEqual(100, GameState.SettingsState.MasterVolumePercent);
        Assert.AreEqual(GraphicsQualityPreset.Balanced, GameState.SettingsState.GraphicsQuality);
    }
}
