using Domain;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Concurrent;
using System.Numerics;

namespace GameAudioInstances
{
    /// <summary>
    /// NAudio-based implementation of IAudioInstance.
    /// Represents a single playing sound (rocket, laser, explosion, etc.).
    /// </summary>
    internal sealed class NAudioAudioInstance : IAudioInstance
    {
        private readonly NAudioAudioPlayer _owner;
        private readonly ISampleProvider _pipeline;
        private readonly VolumeSampleProvider _volumeProvider;
        private readonly SegmentedLoopSampleProvider? _loopProvider;
        private readonly SoundDefinition _definition;

        // Currently not in use
        private float _currentVolume;
        private float _currentSpeed;
        private Vector3 _worldPosition;
        private bool _isPlaying;
        private readonly AudioPlayMode _mode;

        public Guid Id { get; } = Guid.NewGuid();

        public string SoundId => _definition.Id;

        public bool IsPlaying => _isPlaying;
        public bool IsLooping => _loopProvider?.IsLooping ?? false;

        internal ISampleProvider Pipeline => _pipeline;

        internal NAudioAudioInstance(
            NAudioAudioPlayer owner,
            SoundDefinition definition,
            VolumeSampleProvider volumeProvider,
            ISampleProvider pipeline,
            SegmentedLoopSampleProvider? loopProvider,
            AudioPlayMode mode,
            float initialVolume,
            float initialSpeed)
        {
            _owner = owner;
            _definition = definition;
            _volumeProvider = volumeProvider;
            _pipeline = pipeline;
            _loopProvider = loopProvider;
            _currentVolume = initialVolume;
            _currentSpeed = initialSpeed;
            _worldPosition = Vector3.Zero;
            _isPlaying = true;
            _mode = mode;
        }

        public void SetVolume(float volume)
        {
            if (volume < 0f) volume = 0f;
            if (volume > 1f) volume = 1f;
            _currentVolume = volume;
            _volumeProvider.Volume = volume;
        }

        public void SetSpeed(float speed)
        {
            // TODO: Hook into a real varispeed provider if you want speed to affect playback rate.
            _currentSpeed = speed;
        }

        public void SetWorldPosition(Vector3 position)
        {
            _worldPosition = position;
            // TODO: Use this for pan/volume attenuation if you want 3D-ish behavior.
        }

        public void Stop(bool playEndSegment)
        {
            if (!_isPlaying)
                return;

            if (_mode == AudioPlayMode.SegmentedLoop && _loopProvider != null && playEndSegment)
            {
                _loopProvider.RequestStopWithEndSegment();
            }
            else
            {
                _isPlaying = false;
                _owner.InternalStopInstance(this);
            }
        }

        internal void MarkFinished()
        {
            _isPlaying = false;
        }
    }

    /// <summary>
    /// NAudio-based implementation of IAudioPlayer.
    /// Uses a MixingSampleProvider to mix all active sounds.
    /// </summary>
    public sealed class NAudioAudioPlayer : IAudioPlayer, IDisposable
    {
        private readonly IWavePlayer _outputDevice;
        private readonly MixingSampleProvider _mixer;
        private readonly ConcurrentDictionary<Guid, NAudioAudioInstance> _instances =
            new();

        private readonly string _audioBasePath;
        private readonly Random _rng = new();

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

            // Read file – AudioFileReader gives us float samples and handles format conversion.
            var fileReader = new AudioFileReader(fullPath);
            var source = fileReader.ToSampleProvider();

            // Resample to mixer format if needed.
            ISampleProvider sample = source;
            if (!sample.WaveFormat.Equals(_mixer.WaveFormat))
            {
                sample = new WdlResamplingSampleProvider(sample, _mixer.WaveFormat.SampleRate);
            }

            // Volume
            float baseVolume = definition.Settings.Volume;
            if (options?.VolumeOverride is { } volOverride)
                baseVolume = volOverride;

            var volumeProvider = new VolumeSampleProvider(sample)
            {
                Volume = baseVolume
            };

            // Speed: currently just logical, hook to varispeed in future.
            float speed;
            if (options?.SpeedOverride is { } speedOverride)
                speed = speedOverride;
            else
                speed = definition.Speed.GetRandomizedSpeed(_rng);

            // Segment handling
            SegmentedLoopSampleProvider? loopProvider = null;
            ISampleProvider pipeline = volumeProvider;

            if (mode == AudioPlayMode.SegmentedLoop)
            {
                loopProvider = new SegmentedLoopSampleProvider(
                    volumeProvider,
                    definition.Segments,
                    _mixer.WaveFormat);

                pipeline = loopProvider;
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

    /// <summary>
    /// Simple SegmentedLoopSampleProvider stub.
    /// NOTE: This is a simplified version that doesn't yet do real segment looping –
    /// it just reads from the source. You can extend with true loop logic later.
    /// </summary>
    internal sealed class SegmentedLoopSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly WaveFormat _format;
        private readonly SoundSegments _segments;

        private enum PlayState
        {
            Start,
            Loop,
            End,
            Finished
        }

        private PlayState _state = PlayState.Start;
        private bool _stopRequested;

        public SegmentedLoopSampleProvider(ISampleProvider source, SoundSegments segments, WaveFormat targetFormat)
        {
            _source = source;
            _format = targetFormat;
            _segments = segments;
        }

        public WaveFormat WaveFormat => _format;

        public bool IsLooping => _state == PlayState.Loop && !_stopRequested;

        public void RequestStopWithEndSegment()
        {
            _stopRequested = true;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (_state == PlayState.Finished)
                return 0;

            // Simplified: just pass-through from source.
            // Later you can implement sample-accurate start/loop/end behavior
            // using a WaveStream and segment times from _segments.
            int read = _source.Read(buffer, offset, count);
            if (read == 0)
            {
                _state = PlayState.Finished;
                return 0;
            }

            return read;
        }
    }
}
