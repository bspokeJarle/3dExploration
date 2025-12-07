using Domain;
using NAudio.Wave;

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

