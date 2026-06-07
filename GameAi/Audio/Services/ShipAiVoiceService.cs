using Domain;
using System;
using System.Collections.Generic;

namespace GameAiAndControls.Audio.Services
{
    public enum ShipAiVoiceCue
    {
        CollisionWarning,
        CleanLoop,
        CollisionLoop,
        LowAltitudeRisk,
        PlanetBonusComplete
    }

    public enum ShipAiVoicePriority
    {
        Bonus = 0,
        Warning = 1,
        Critical = 2
    }

    public sealed class ShipAiVoiceService
    {
        private sealed class CueConfig
        {
            public CueConfig(
                ShipAiVoicePriority priority,
                double cooldownSeconds,
                double interruptCooldownSeconds,
                float? volumeOverride,
                params string[] soundIds)
            {
                Priority = priority;
                CooldownSeconds = cooldownSeconds;
                InterruptCooldownSeconds = interruptCooldownSeconds;
                VolumeOverride = volumeOverride;
                SoundIds = soundIds;
            }

            public ShipAiVoicePriority Priority { get; }
            public double CooldownSeconds { get; }
            public double InterruptCooldownSeconds { get; }
            public float? VolumeOverride { get; }
            public IReadOnlyList<string> SoundIds { get; }
        }

        private static readonly IReadOnlyDictionary<ShipAiVoiceCue, CueConfig> CueConfigs =
            new Dictionary<ShipAiVoiceCue, CueConfig>
            {
                [ShipAiVoiceCue.CollisionWarning] = new(
                    ShipAiVoicePriority.Warning,
                    cooldownSeconds: 1.5,
                    interruptCooldownSeconds: 0.25,
                    volumeOverride: 1.0f,
                    "ship_collision_warning"),

                [ShipAiVoiceCue.CleanLoop] = new(
                    ShipAiVoicePriority.Bonus,
                    cooldownSeconds: 8.0,
                    interruptCooldownSeconds: 8.0,
                    volumeOverride: 0.75f,
                    "ship_ai_clean_loop",
                    "ship_ai_great_flying"),

                [ShipAiVoiceCue.CollisionLoop] = new(
                    ShipAiVoicePriority.Bonus,
                    cooldownSeconds: 8.0,
                    interruptCooldownSeconds: 8.0,
                    volumeOverride: 0.7f,
                    "ship_ai_close_one",
                    "ship_ai_maneuver_unstable"),

                [ShipAiVoiceCue.LowAltitudeRisk] = new(
                    ShipAiVoicePriority.Bonus,
                    cooldownSeconds: 8.0,
                    interruptCooldownSeconds: 8.0,
                    volumeOverride: 0.7f,
                    "ship_ai_low_altitude_bonus"),

                [ShipAiVoiceCue.PlanetBonusComplete] = new(
                    ShipAiVoicePriority.Bonus,
                    cooldownSeconds: 12.0,
                    interruptCooldownSeconds: 12.0,
                    volumeOverride: 0.7f,
                    "ship_ai_planet_bonus_complete")
            };

        public static ShipAiVoiceService Shared { get; } = new();

        private readonly Func<DateTime> _now;
        private readonly Random _random;
        private DateTime _lastSpokenAt = DateTime.MinValue;
        private ShipAiVoicePriority _lastPriority = ShipAiVoicePriority.Bonus;
        private string? _lastSoundId;

        public ShipAiVoiceService(Func<DateTime>? now = null, Random? random = null)
        {
            _now = now ?? (() => DateTime.Now);
            _random = random ?? new Random();
        }

        public bool TrySpeak(
            ShipAiVoiceCue cue,
            IAudioPlayer? audioPlayer,
            ISoundRegistry? soundRegistry,
            AudioPlayOptions? options = null)
        {
            if (audioPlayer == null || soundRegistry == null)
                return false;

            if (!CueConfigs.TryGetValue(cue, out var config))
                return false;

            var now = _now();
            if (!CanSpeak(config, now))
                return false;

            if (!TryPickSound(config.SoundIds, soundRegistry, out var definition))
                return false;

            audioPlayer.PlayOneShot(definition, BuildOptions(config, options));

            _lastSpokenAt = now;
            _lastPriority = config.Priority;
            _lastSoundId = definition.Id;
            return true;
        }

        private bool CanSpeak(CueConfig config, DateTime now)
        {
            if (_lastSpokenAt == DateTime.MinValue)
                return true;

            double elapsed = (now - _lastSpokenAt).TotalSeconds;
            if (elapsed >= config.CooldownSeconds)
                return true;

            return config.Priority > _lastPriority &&
                   elapsed >= config.InterruptCooldownSeconds;
        }

        private bool TryPickSound(
            IReadOnlyList<string> soundIds,
            ISoundRegistry soundRegistry,
            out SoundDefinition definition)
        {
            var available = new List<SoundDefinition>();
            foreach (string soundId in soundIds)
            {
                if (soundRegistry.TryGet(soundId, out var soundDefinition))
                    available.Add(soundDefinition);
            }

            if (available.Count == 0)
            {
                definition = null!;
                return false;
            }

            if (available.Count > 1 && _lastSoundId != null)
                available.RemoveAll(sound => string.Equals(sound.Id, _lastSoundId, StringComparison.OrdinalIgnoreCase));

            definition = available[_random.Next(available.Count)];
            return true;
        }

        private static AudioPlayOptions BuildOptions(CueConfig config, AudioPlayOptions? options)
        {
            return new AudioPlayOptions
            {
                VolumeOverride = options?.VolumeOverride ?? config.VolumeOverride,
                SpeedOverride = options?.SpeedOverride,
                Pan = options?.Pan,
                WorldPosition = options?.WorldPosition,
                Tag = options?.Tag
            };
        }
    }
}
