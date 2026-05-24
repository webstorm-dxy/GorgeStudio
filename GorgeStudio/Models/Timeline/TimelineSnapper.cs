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
        if (bpm <= 0 || beatsPerBar <= 0 || subdivisionsPerBeat <= 0) return seconds;

        var offsetSeconds = offsetMilliseconds / 1000.0;
        var beatSeconds = 60.0 / bpm;

        var interval = mode switch
        {
            TimelineSnapMode.Bar => beatSeconds * beatsPerBar,
            TimelineSnapMode.Beat => beatSeconds,
            TimelineSnapMode.Subdivision => beatSeconds / subdivisionsPerBeat,
            _ => beatSeconds / subdivisionsPerBeat
        };

        var relative = seconds - offsetSeconds;
        var snappedRelative = Math.Round(relative / interval, MidpointRounding.AwayFromZero) * interval;
        var snapped = offsetSeconds + snappedRelative;
        return Math.Max(0, snapped);
    }

    public static double SnapDuration(
        double duration,
        bool enabled,
        TimelineSnapMode mode,
        int bpm,
        int beatsPerBar,
        int subdivisionsPerBeat)
    {
        duration = Math.Max(0.25, duration);
        if (!enabled) return duration;
        if (bpm <= 0 || beatsPerBar <= 0 || subdivisionsPerBeat <= 0) return duration;

        var beatSeconds = 60.0 / bpm;

        var interval = mode switch
        {
            TimelineSnapMode.Bar => beatSeconds * beatsPerBar,
            TimelineSnapMode.Beat => beatSeconds,
            TimelineSnapMode.Subdivision => beatSeconds / subdivisionsPerBeat,
            _ => beatSeconds / subdivisionsPerBeat
        };

        var snapped = Math.Round(duration / interval, MidpointRounding.AwayFromZero) * interval;
        return Math.Max(0.25, snapped);
    }
}
