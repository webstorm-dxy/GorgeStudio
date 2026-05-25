using System;

namespace GorgeStudio.Models.Timeline;

public static class TimelineTimeConverter
{
    public const double MinimumPeriodLengthSeconds = 0.25;

    public static bool TryGetBeatSeconds(int bpm, out double beatSeconds)
    {
        if (bpm <= 0)
        {
            beatSeconds = 0;
            return false;
        }

        beatSeconds = 60.0 / bpm;
        return true;
    }

    public static bool TryGetSnapIntervalSeconds(
        TimelineSnapMode mode,
        int bpm,
        int beatsPerBar,
        int subdivisionsPerBeat,
        out double intervalSeconds)
    {
        if (!TryGetBeatSeconds(bpm, out var beatSeconds)
            || beatsPerBar <= 0
            || subdivisionsPerBeat <= 0)
        {
            intervalSeconds = 0;
            return false;
        }

        intervalSeconds = mode switch
        {
            TimelineSnapMode.Bar => beatSeconds * beatsPerBar,
            TimelineSnapMode.Beat => beatSeconds,
            TimelineSnapMode.Subdivision => beatSeconds / subdivisionsPerBeat,
            _ => beatSeconds / subdivisionsPerBeat
        };

        return intervalSeconds > 0 && !double.IsNaN(intervalSeconds) && !double.IsInfinity(intervalSeconds);
    }

    public static double OffsetMillisecondsToSeconds(int offsetMilliseconds)
    {
        return offsetMilliseconds / 1000.0;
    }

    public static double GridIndexToSeconds(
        long index,
        TimelineSnapMode mode,
        int bpm,
        int offsetMilliseconds,
        int beatsPerBar,
        int subdivisionsPerBeat)
    {
        if (!TryGetSnapIntervalSeconds(mode, bpm, beatsPerBar, subdivisionsPerBeat, out var intervalSeconds))
            return Math.Max(0, OffsetMillisecondsToSeconds(offsetMilliseconds));

        return Math.Max(0, OffsetMillisecondsToSeconds(offsetMilliseconds) + index * intervalSeconds);
    }

    public static double DurationGridIndexToSeconds(
        long index,
        TimelineSnapMode mode,
        int bpm,
        int beatsPerBar,
        int subdivisionsPerBeat)
    {
        if (!TryGetSnapIntervalSeconds(mode, bpm, beatsPerBar, subdivisionsPerBeat, out var intervalSeconds))
            return MinimumPeriodLengthSeconds;

        return Math.Max(MinimumPeriodLengthSeconds, index * intervalSeconds);
    }
}
