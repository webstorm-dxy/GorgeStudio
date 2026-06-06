using System;
using System.Collections.Generic;

namespace GorgeStudio.Models;

public class ProjectSettings : ICloneable
{
    public int Bpm { get; set; } = 120;
    public int Offset { get; set; } = 0;
    public int BeatsPerBar { get; set; } = 4;
    public int SubdivisionsPerBeat { get; set; } = 4;
    public List<string> Forms { get; set; } = new();

    public object Clone()
    {
        return new ProjectSettings
        {
            Bpm = Bpm,
            Offset = Offset,
            BeatsPerBar = BeatsPerBar,
            SubdivisionsPerBeat = SubdivisionsPerBeat,
            Forms = new List<string>(Forms),
        };
    }
}
