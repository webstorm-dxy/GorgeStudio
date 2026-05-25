using System;

namespace GorgeStudio.Models.Timeline;

public enum TimelineSnapMode
{
    Bar,
    Beat,
    Subdivision
}

public static class TimelineSnapper
{
    /// <summary>
    /// Snaps an absolute timeline position. Input and output are seconds.
    /// </summary>
    public static double Snap(
        double seconds,
        bool enabled,
        TimelineSnapMode mode,
        int bpm,
        int offsetMilliseconds,
        int beatsPerBar,
        int subdivisionsPerBeat)
    {
        seconds = Math.Max(0, seconds);
        if (!enabled) return seconds;
        if (!TimelineTimeConverter.TryGetSnapIntervalSeconds(
                mode, bpm, beatsPerBar, subdivisionsPerBeat, out var interval))
            return seconds;

        var offsetSeconds = TimelineTimeConverter.OffsetMillisecondsToSeconds(offsetMilliseconds);
        var relative = seconds - offsetSeconds;
        var snappedRelative = Math.Round(relative / interval, MidpointRounding.AwayFromZero) * interval;
        var snapped = offsetSeconds + snappedRelative;
        return Math.Max(0, snapped);
    }

    /// <summary>
    /// Snaps a duration. Input and output are seconds; timeline offset is intentionally not applied.
    /// </summary>
    public static double SnapDuration(
        double duration,
        bool enabled,
        TimelineSnapMode mode,
        int bpm,
        int beatsPerBar,
        int subdivisionsPerBeat)
    {
        duration = Math.Max(TimelineTimeConverter.MinimumPeriodLengthSeconds, duration);
        if (!enabled) return duration;
        if (!TimelineTimeConverter.TryGetSnapIntervalSeconds(
                mode, bpm, beatsPerBar, subdivisionsPerBeat, out var interval))
            return duration;

        var snapped = Math.Round(duration / interval, MidpointRounding.AwayFromZero) * interval;
        return Math.Max(TimelineTimeConverter.MinimumPeriodLengthSeconds, snapped);
    }
}
