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
                Logger.Log(@"Audio:Requested stop with EndSegment.");
                _loopProvider.RequestStopWithEndSegment();
            }
            else
            {
                Logger.Log(@"Audio:IsPlaying set to false.");
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
                Logger.Log(@"Audio: Playing segmented loop.");
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

    /// <summary>
    /// Simple SegmentedLoopSampleProvider stub.
    /// NOTE: This is a simplified version that doesn't yet do real segment looping –
    /// it just reads from the source. You can extend with true loop logic later.
    /// </summary>
    internal sealed class SegmentedLoopSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;     // Normalized audio (matches mixer format)
        private readonly AudioFileReader _file;       // Underlying file for time/seek
        private readonly WaveFormat _format;
        private readonly SoundSegments _segments;

        private bool _stopRequested;
        private bool _finished;

        private readonly TimeSpan _loopStartTs;
        private readonly TimeSpan _loopEndTs;
        private readonly TimeSpan _endTs;

        public SegmentedLoopSampleProvider(
            ISampleProvider source,
            AudioFileReader file,
            SoundSegments segments,
            WaveFormat targetFormat)
        {
            _source = source;
            _file = file;
            _segments = segments;
            _format = targetFormat;

            _loopStartTs = TimeSpan.FromSeconds(_segments.LoopStart);
            _loopEndTs = TimeSpan.FromSeconds(_segments.LoopEnd);
            _endTs = TimeSpan.FromSeconds(_segments.End);

            // Start på "start"-segmentet
            _file.CurrentTime = TimeSpan.FromSeconds(_segments.Start);
        }

        public WaveFormat WaveFormat => _format;

        public bool IsLooping => !_stopRequested && !_finished;

        /// <summary>
        /// Called when we want to stop looping and play the end tail.
        /// </summary>
        public void RequestStopWithEndSegment()
        {
            _stopRequested = true;

            var now = _file.CurrentTime;

            // Hvis vi fortsatt er i start/loop-området når vi stopper,
            // hopper vi rett til loopEnd slik at tailen blir kort og konsistent.
            if (now < _loopEndTs)
            {
                Logger.Log(
                    $"Audio: SegmentedLoop - stop requested at t={now.TotalSeconds:F2}s, " +
                    $"jumping to loopEnd={_segments.LoopEnd:F2}s for short tail.");

                _file.CurrentTime = _loopEndTs;
            }
            else
            {
                Logger.Log(
                    $"Audio: SegmentedLoop - stop requested at t={now.TotalSeconds:F2}s, already in tail.");
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (_finished)
                return 0;

            int read = _source.Read(buffer, offset, count);
            if (read == 0)
            {
                Logger.Log("Audio: SegmentedLoop - source returned 0, marking as finished.");
                _finished = true;
                return 0;
            }

            // Debug: hvor i fila er vi nå?
            var t = _file.CurrentTime.TotalSeconds;

            if (!_stopRequested)
            {
                if (_file.CurrentTime >= _loopEndTs)
                {
                    Logger.Log($"Audio: SegmentedLoop - looping back at t={t:F2}s -> loopStart={_segments.LoopStart:F2}s");
                    _file.CurrentTime = _loopStartTs;
                }
            }
            else
            {
                if (_file.CurrentTime >= _endTs)
                {
                    Logger.Log($"Audio: SegmentedLoop - reached end segment at t={t:F2}s, finishing.");
                    _finished = true;

                    for (int i = offset; i < offset + read; i++)
                        buffer[i] = 0f;
                }
            }

            return read;
        }
    }
}
