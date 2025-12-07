using Domain;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

using System.Collections.Concurrent;

/// <summary>
/// NAudio-based implementation of IAudioPlayer.
/// Uses a MixingSampleProvider to mix all active sounds.
/// </summary>
public sealed class NAudioAudioPlayer : IAudioPlayer, IDisposable
{
    private bool enableLogging = false;
    private readonly IWavePlayer _outputDevice;
    private readonly MixingSampleProvider _mixer;
    private readonly ConcurrentDictionary<Guid, NAudioAudioInstance> _instances =
        new();

    private readonly string _audioBasePath;
    private readonly Random _rng = new();

    private NAudioAudioInstance? _musicInstance;
    private float _musicVolume = 0.6f;

    public NAudioAudioPlayer(string audioBasePath)
    {
        _audioBasePath = audioBasePath;

        // Mixer format: 44100 Hz, stereo, float.
        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
        _mixer = new MixingSampleProvider(waveFormat)
        {
            ReadFully = true
        };

        _outputDevice = new WaveOutEvent();
        _outputDevice.Init(_mixer);
        _outputDevice.Play();
    }

    public void Dispose()
    {
        _outputDevice?.Stop();
        _outputDevice?.Dispose();
    }

    public IAudioInstance Play(SoundDefinition definition, AudioPlayMode mode, AudioPlayOptions? options = null)
    {
        var fullPath = System.IO.Path.Combine(_audioBasePath, definition.File);
        if (!System.IO.File.Exists(fullPath))
            throw new System.IO.FileNotFoundException($"Audio file not found: {fullPath}");

        // LES FIL – AudioFileReader er både WaveStream og ISampleProvider
        var fileReader = new AudioFileReader(fullPath);

        // Normaliser til mixer-format (44100 Hz, 2 kanaler)
        ISampleProvider source = fileReader;
        ISampleProvider sample = NormalizeToMixerFormat(source);

        // Volume
        float baseVolume = definition.Settings.Volume;
        if (options?.VolumeOverride is { } volOverride)
            baseVolume = volOverride;

        var volumeProvider = new VolumeSampleProvider(sample)
        {
            Volume = baseVolume
        };

        // Speed (logisk, brukes senere hvis du vil styre pitch)
        float speed;
        if (options?.SpeedOverride is { } speedOverride)
            speed = speedOverride;
        else
            speed = definition.Speed.GetRandomizedSpeed(_rng);

        // Segment-handling
        SegmentedLoopSampleProvider? loopProvider = null;
        ISampleProvider pipeline = volumeProvider;

        if (mode == AudioPlayMode.SegmentedLoop)
        {
            loopProvider = new SegmentedLoopSampleProvider(
                volumeProvider,      // det som faktisk går til mixeren
                fileReader,          // selve fila – for CurrentTime / seeking
                definition.Segments,
                _mixer.WaveFormat);

            pipeline = loopProvider;
            if (enableLogging) Logger.Log(@"Audio: Playing segmented loop.");
        }

        var instance = new NAudioAudioInstance(
            this,
            definition,
            volumeProvider,
            pipeline,
            loopProvider,
            mode,
            baseVolume,
            speed);

        _instances[instance.Id] = instance;
        _mixer.AddMixerInput(pipeline);
        Logger.Log(@"Audio: Play end of method.");
        return instance;
    }

    public void PlayOneShot(SoundDefinition definition, AudioPlayOptions? options = null)
    {
        _ = Play(definition, AudioPlayMode.OneShot, options);
        // For one-shot, we rely on AudioFileReader naturally reaching EOF.
    }

    public void Stop(IAudioInstance instance, bool playEndSegment)
    {
        if (instance is NAudioAudioInstance naudioInstance)
        {
            naudioInstance.Stop(playEndSegment);
        }
    }

    public void StopAll()
    {
        foreach (var kvp in _instances)
        {
            kvp.Value.Stop(playEndSegment: false);
        }

        _instances.Clear();
    }

    /// <summary>
    /// Plays background music using segmented loop.
    /// The SoundDefinition's segments (start, loopStart, loopEnd, end)
    /// define which part of the file is used.
    /// Any previously playing music instance is stopped first.
    /// </summary>
    public void PlayMusic(SoundDefinition definition, float? volumeOverride = null)
    {
        // Stopp eventuell gammel musikk først
        if (_musicInstance != null)
        {
            Logger.Log("Audio: Stopping previous music instance before starting new.");
            _musicInstance.Stop(playEndSegment: false); // ikke tail på musikkbytte
            _musicInstance = null;
        }

        // Bestem volum
        float vol = volumeOverride ?? definition.Settings.Volume;
        if (vol < 0f) vol = 0f;
        if (vol > 1f) vol = 1f;
        _musicVolume = vol;

        Logger.Log(
            $"Audio: PlayMusic id={definition.Id}, " +
            $"segments: start={definition.Segments.Start:F2}, " +
            $"loopStart={definition.Segments.LoopStart:F2}, " +
            $"loopEnd={definition.Segments.LoopEnd:F2}, end={definition.Segments.End:F2}");

        // Viktig: Segmentert loop – da brukes segments.start/loopStart/loopEnd
        var instance = Play(
            definition,
            AudioPlayMode.SegmentedLoop,
            new AudioPlayOptions
            {
                VolumeOverride = vol
                // SpeedOverride kan være null → bruker definition.Speed/base
            });

        // Vi vet at Play() i vår implem faktisk returnerer NAudioAudioInstance
        _musicInstance = instance as NAudioAudioInstance;
    }

    /// <summary>
    /// Stops the currently playing music, if any.
    /// </summary>
    public void StopMusic()
    {
        if (_musicInstance != null)
        {
            Logger.Log("Audio: Stopping music instance.");
            _musicInstance.Stop(playEndSegment: false);
            _musicInstance = null;
        }
    }

    /// <summary>
    /// Adjusts music volume at runtime.
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        _musicVolume = Math.Clamp(volume, 0f, 1f);

        if (_musicInstance != null)
        {
            Logger.Log($"Audio: Setting music volume to {_musicVolume:F2}.");
            _musicInstance.SetVolume(_musicVolume);
        }
    }

    private ISampleProvider NormalizeToMixerFormat(ISampleProvider source)
    {
        ISampleProvider sample = source;

        // 1) Kanal-konvertering (mono ↔ stereo)
        if (sample.WaveFormat.Channels != _mixer.WaveFormat.Channels)
        {
            if (sample.WaveFormat.Channels == 1 && _mixer.WaveFormat.Channels == 2)
            {
                // Mono -> Stereo
                sample = new MonoToStereoSampleProvider(sample);
            }
            else if (sample.WaveFormat.Channels == 2 && _mixer.WaveFormat.Channels == 1)
            {
                // Stereo -> Mono
                sample = new StereoToMonoSampleProvider(sample);
            }
            else
            {
                throw new NotSupportedException(
                    $"Unsupported channel config: source={sample.WaveFormat.Channels}, mixer={_mixer.WaveFormat.Channels}");
            }
        }

        // 2) Sample rate-konvertering
        if (sample.WaveFormat.SampleRate != _mixer.WaveFormat.SampleRate)
        {
            sample = new WdlResamplingSampleProvider(sample, _mixer.WaveFormat.SampleRate);
        }

        return sample;
    }

    public void Update(double deltaTimeSeconds)
    {
        // If you later need per-frame bookkeeping, you can put it here.
        // Current implementation does not require per-frame updates.
    }

    internal void InternalStopInstance(NAudioAudioInstance instance)
    {
        _instances.TryRemove(instance.Id, out _);
        // If you're on a NAudio version with RemoveMixerInput:
        // _mixer.RemoveMixerInput(instance.Pipeline);
        // Otherwise, pipeline will just output 0 after EOF – minimal overhead.
    }
}
