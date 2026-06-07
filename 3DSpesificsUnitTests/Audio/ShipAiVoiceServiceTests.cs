using Domain;
using GameAiAndControls.Audio.Services;

namespace _3DSpesificsUnitTests.Audio;

[TestClass]
public class ShipAiVoiceServiceTests
{
    [TestMethod]
    public void TrySpeak_CollisionWarning_PlaysExistingWarningSound()
    {
        DateTime now = new(2026, 1, 1, 12, 0, 0);
        var service = new ShipAiVoiceService(() => now, new Random(0));
        var audio = new CapturingAudioPlayer();
        var registry = new FakeSoundRegistry("ship_collision_warning");

        bool played = service.TrySpeak(ShipAiVoiceCue.CollisionWarning, audio, registry);

        Assert.IsTrue(played);
        Assert.AreEqual(1, audio.PlayedSoundIds.Count);
        Assert.AreEqual("ship_collision_warning", audio.PlayedSoundIds[0]);
    }

    [TestMethod]
    public void TrySpeak_WhenCooldownActive_DoesNotRepeatBonusVoice()
    {
        DateTime now = new(2026, 1, 1, 12, 0, 0);
        var service = new ShipAiVoiceService(() => now, new Random(0));
        var audio = new CapturingAudioPlayer();
        var registry = new FakeSoundRegistry("ship_ai_clean_loop");

        Assert.IsTrue(service.TrySpeak(ShipAiVoiceCue.CleanLoop, audio, registry));

        now = now.AddSeconds(2);
        Assert.IsFalse(service.TrySpeak(ShipAiVoiceCue.CleanLoop, audio, registry));
        Assert.AreEqual(1, audio.PlayedSoundIds.Count);
    }

    [TestMethod]
    public void TrySpeak_WarningCanInterruptRecentBonusVoice()
    {
        DateTime now = new(2026, 1, 1, 12, 0, 0);
        var service = new ShipAiVoiceService(() => now, new Random(0));
        var audio = new CapturingAudioPlayer();
        var registry = new FakeSoundRegistry("ship_ai_clean_loop", "ship_collision_warning");

        Assert.IsTrue(service.TrySpeak(ShipAiVoiceCue.CleanLoop, audio, registry));

        now = now.AddSeconds(2);
        Assert.IsTrue(service.TrySpeak(ShipAiVoiceCue.CollisionWarning, audio, registry));
        Assert.AreEqual("ship_collision_warning", audio.PlayedSoundIds[^1]);
    }

    [TestMethod]
    public void TrySpeak_WhenCueHasNoRegisteredSound_DoesNotPlay()
    {
        DateTime now = new(2026, 1, 1, 12, 0, 0);
        var service = new ShipAiVoiceService(() => now, new Random(0));
        var audio = new CapturingAudioPlayer();
        var registry = new FakeSoundRegistry("ship_collision_warning");

        bool played = service.TrySpeak(ShipAiVoiceCue.CleanLoop, audio, registry);

        Assert.IsFalse(played);
        Assert.AreEqual(0, audio.PlayedSoundIds.Count);
    }

    [TestMethod]
    public void TrySpeak_WhenMultipleLinesAreAvailable_DoesNotRepeatPreviousLine()
    {
        DateTime now = new(2026, 1, 1, 12, 0, 0);
        var service = new ShipAiVoiceService(() => now, new Random(0));
        var audio = new CapturingAudioPlayer();
        var registry = new FakeSoundRegistry("ship_ai_clean_loop", "ship_ai_great_flying");

        Assert.IsTrue(service.TrySpeak(ShipAiVoiceCue.CleanLoop, audio, registry));
        string firstLine = audio.PlayedSoundIds[0];

        now = now.AddSeconds(9);
        Assert.IsTrue(service.TrySpeak(ShipAiVoiceCue.CleanLoop, audio, registry));

        Assert.AreNotEqual(firstLine, audio.PlayedSoundIds[1]);
    }

    private sealed class FakeSoundRegistry : ISoundRegistry
    {
        private readonly HashSet<string> _soundIds;

        public FakeSoundRegistry(params string[] soundIds)
        {
            _soundIds = new HashSet<string>(soundIds, StringComparer.OrdinalIgnoreCase);
        }

        public SoundDefinition Get(string id)
        {
            if (!TryGet(id, out var definition))
                throw new KeyNotFoundException(id);

            return definition;
        }

        public bool TryGet(string id, out SoundDefinition definition)
        {
            if (!_soundIds.Contains(id))
            {
                definition = null!;
                return false;
            }

            definition = new SoundDefinition
            {
                Id = id,
                Usage = id,
                File = $"{id}.wav",
                Settings = new SoundSettings { Volume = 1f }
            };
            return true;
        }
    }

    private sealed class CapturingAudioPlayer : IAudioPlayer
    {
        public List<string> PlayedSoundIds { get; } = new();

        public IAudioInstance Play(SoundDefinition definition, AudioPlayMode mode, AudioPlayOptions? options = null)
        {
            PlayedSoundIds.Add(definition.Id);
            return new CapturingAudioInstance(definition.Id, mode == AudioPlayMode.SegmentedLoop);
        }

        public void PlayOneShot(SoundDefinition definition, AudioPlayOptions? options = null) =>
            Play(definition, AudioPlayMode.OneShot, options);

        public void Stop(IAudioInstance instance, bool playEndSegment) => instance.Stop(playEndSegment);
        public void StopAll() { }
        public void PlayMusic(SoundDefinition definition, float? volumeOverride = null) { }
        public void StopMusic() { }
        public void Update(double deltaTimeSeconds) { }
    }

    private sealed class CapturingAudioInstance : IAudioInstance
    {
        public CapturingAudioInstance(string soundId, bool isLooping)
        {
            SoundId = soundId;
            IsLooping = isLooping;
            IsPlaying = true;
        }

        public Guid Id { get; } = Guid.NewGuid();
        public string SoundId { get; }
        public bool IsPlaying { get; private set; }
        public bool IsLooping { get; }

        public void SetVolume(float volume) { }
        public void SetSpeed(float speed) { }
        public void SetWorldPosition(System.Numerics.Vector3 position) { }
        public void Stop(bool playEndSegment) => IsPlaying = false;
    }
}
