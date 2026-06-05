using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using GameAudioInstances;

namespace _3DSpesificsUnitTests.Audio;

[TestClass]
public class ShipAiVoiceSoundRegistrationTests
{
    private static readonly (string Id, string File, string Usage)[] ShipAiVoiceSounds =
    [
        ("ship_ai_clean_loop", "OmegaStrain_clean_loop.mp3", "ShipAiVoiceCleanLoop"),
        ("ship_ai_great_flying", "OmegaStrain_great_flying.mp3", "ShipAiVoiceGreatFlying"),
        ("ship_ai_low_altitude_bonus", "OmegaStrain_low_altitude_bonus.mp3", "ShipAiVoiceLowAltitudeBonus"),
        ("ship_ai_planet_bonus_complete", "OmegaStrain_planet_bonus_reached.mp3", "ShipAiVoicePlanetBonusComplete")
    ];

    [TestMethod]
    public void ShipAiVoiceSounds_AreRegisteredAndCopiedToOutput()
    {
        var repoRoot = FindRepoRoot();
        Assert.IsNotNull(repoRoot, "Could not locate repository root from test working dir.");

        var soundsJson = Path.Combine(repoRoot!, "3dTesting", "Soundeffects", "sounds.json");
        var audioBasePath = Path.GetDirectoryName(soundsJson)!;
        var registry = new JsonSoundRegistry(soundsJson);
        var frontendProject = XDocument.Load(Path.Combine(repoRoot!, "3dTesting", "Frontend.csproj"));

        foreach (var voiceSound in ShipAiVoiceSounds)
        {
            Assert.IsTrue(
                registry.TryGet(voiceSound.Id, out var definition),
                $"{voiceSound.Id} is missing from sounds.json.");

            Assert.AreEqual(voiceSound.File, definition.File);
            Assert.AreEqual(voiceSound.Usage, definition.Usage);
            Assert.IsFalse(definition.Settings.Is3D, $"{voiceSound.Id} should play as cockpit AI voice, not as world audio.");
            Assert.IsTrue(
                File.Exists(Path.Combine(audioBasePath, voiceSound.File)),
                $"{voiceSound.File} is missing.");

            var copied = frontendProject
                .Descendants("None")
                .Any(e => string.Equals((string?)e.Attribute("Update"), $@"Soundeffects\{voiceSound.File}", StringComparison.OrdinalIgnoreCase) &&
                          e.Elements("CopyToOutputDirectory").Any(c => string.Equals(c.Value, "Always", StringComparison.OrdinalIgnoreCase)));

            Assert.IsTrue(copied, $"{voiceSound.File} must be copied to output.");
        }
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "TheOmegaStrain.sln")))
                return dir.FullName;

            dir = dir.Parent;
        }

        return null;
    }
}
