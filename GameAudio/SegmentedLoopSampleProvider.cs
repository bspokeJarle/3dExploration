using Domain;
using NAudio.Wave;

/// <summary>
/// Plays a sound that has three conceptual sections: intro, loop, and end tail.
/// The provider starts at <see cref="SoundSegments.Start"/>, loops between
/// <see cref="SoundSegments.LoopStart"/> and <see cref="SoundSegments.LoopEnd"/>,
/// and can optionally jump into the end segment when playback is stopped.
/// </summary>
internal sealed class SegmentedLoopSampleProvider : ISampleProvider
{
    private bool enableLogging = false;
    private readonly ISampleProvider _source;
    private readonly AudioFileReader _file;
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

        // Start playback at the configured intro position.
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

        // If playback is still inside the intro/loop section when stop is requested,
        // jump directly to the loop end so the end tail stays short and predictable.
        if (now < _loopEndTs)
        {
            if (enableLogging) Logger.Log(
                $"Audio: SegmentedLoop - stop requested at t={now.TotalSeconds:F2}s, " +
                $"jumping to loopEnd={_segments.LoopEnd:F2}s for short tail.");

            _file.CurrentTime = _loopEndTs;
        }
        else
        {
            if (enableLogging) Logger.Log(
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
            if (!_stopRequested)
            {
                _file.CurrentTime = _loopStartTs;
                read = _source.Read(buffer, offset, count);
            }

            if (read > 0)
            {
                return read;
            }

            if (enableLogging) Logger.Log("Audio: SegmentedLoop - source returned 0, marking as finished.");
            _finished = true;
            return 0;
        }

        // Capture the playback position once so logging and transition checks are consistent.
        var t = _file.CurrentTime.TotalSeconds;

        if (!_stopRequested)
        {
            if (_file.CurrentTime >= _loopEndTs)
            {
                if (enableLogging) Logger.Log($"Audio: SegmentedLoop - looping back at t={t:F2}s -> loopStart={_segments.LoopStart:F2}s");
                _file.CurrentTime = _loopStartTs;
            }
        }
        else
        {
            if (_file.CurrentTime >= _endTs)
            {
                if (enableLogging) Logger.Log($"Audio: SegmentedLoop - reached end segment at t={t:F2}s, finishing.");
                _finished = true;

                for (int i = offset; i < offset + read; i++)
                    buffer[i] = 0f;
            }
        }

        return read;
    }
}

