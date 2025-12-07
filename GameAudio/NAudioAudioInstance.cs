using Domain;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Numerics;

/// <summary>
/// NAudio-based implementation of IAudioInstance.
/// Represents a single playing sound (rocket, laser, explosion, etc.).
/// </summary>
internal sealed class NAudioAudioInstance : IAudioInstance
{
    private bool enableLogging = false;
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
            if (enableLogging) Logger.Log(@"Audio:IsPlaying set to false.");
            _isPlaying = false;
            _owner.InternalStopInstance(this);
        }
    }

    internal void MarkFinished()
    {
        if (enableLogging) Logger.Log(@"Audio: Marking instance as finished.");
        _isPlaying = false;
    }
}
