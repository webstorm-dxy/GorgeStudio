using System;
using System.Collections.Generic;

namespace GorgeStudio.Models.Timeline;

public class TickDivision
{
    public double TickIntervalSeconds { get; init; }
    public int MajorTickInterval { get; init; }
    public int HighlightTickInterval { get; init; }

    public static TickDivision Calculate(int bpm, int beatsPerBar, int subdivisionsPerBeat, double pixelsPerSecond)
    {
        const double minPixelsPerTick = 24.0;

        if (bpm <= 0 || beatsPerBar < 1 || subdivisionsPerBeat < 1 || pixelsPerSecond <= 0)
        {
            return new TickDivision
            {
                TickIntervalSeconds = 1.0,
                MajorTickInterval = 4,
                HighlightTickInterval = 0
            };
        }

        var beatTime = 60.0 / bpm;
        var subBeatTime = beatTime / subdivisionsPerBeat;
        var barTime = beatTime * beatsPerBar;

        var candidates = new List<(double interval, int majorEvery, int highlightEvery)>
        {
            (subBeatTime, beatsPerBar * subdivisionsPerBeat, subdivisionsPerBeat),
            (beatTime, beatsPerBar, 1),
            (barTime, 2, 1)
        };

        // Geometric progression: multi-bar ticks
        var current = barTime * 2;
        for (var i = 0; i < 5; i++)
        {
            candidates.Add((current, 2, 1));
            current *= 2;
        }

        foreach (var (interval, majorEvery, highlightEvery) in candidates)
        {
            if (interval * pixelsPerSecond >= minPixelsPerTick)
            {
                return new TickDivision
                {
                    TickIntervalSeconds = interval,
                    MajorTickInterval = majorEvery,
                    HighlightTickInterval = highlightEvery
                };
            }
        }

        var last = candidates[^1];
        return new TickDivision
        {
            TickIntervalSeconds = last.interval,
            MajorTickInterval = last.majorEvery,
            HighlightTickInterval = last.highlightEvery
        };
    }
}
