using System;
using System.Collections.Generic;
using System.Linq;
using GorgeStudio.Models.Chart;
using GorgeStudio.Models.Timeline;
using GorgeStudio.Services;

namespace GorgeStudio.AppCore.Services;

public interface ITimelineEditingWorkflow
{
    IStaff CreateStaff(string classNamePrefix, bool isChart, string displayName, string? formName = null);
    string GetNextClassName(SimulationScore score, string prefix);
    void AddStaff(SimulationScore score, IStaff staff);
    void RemoveStaff(SimulationScore score, int index);
    void CopyStaff(SimulationScore score, int index);
    void RenameStaffDisplayName(SimulationScore score, int index, string newDisplayName);
    IPeriod CreatePeriod(IStaff staff, SimulationScore score, float timeOffsetSeconds);
    IPeriod InsertPeriod(IStaff staff, IPeriod period);
    IPeriod DuplicatePeriod(IStaff staff, IPeriod source);
    void RemovePeriod(IStaff staff, IPeriod period);
    void UpdatePeriodTimeOffset(IPeriod period, float timeOffsetSeconds);
    void UpdatePeriodMinLength(IPeriod period, float minLengthSeconds);
    double SnapTime(double seconds, bool snapEnabled, TimelineSnapMode snapMode, int bpm, int offset, int beatsPerBar, int subdivisionsPerBeat);
    double SnapDuration(double duration, bool snapEnabled, TimelineSnapMode snapMode, int bpm, int beatsPerBar, int subdivisionsPerBeat);
}

public sealed class TimelineEditingWorkflow : ITimelineEditingWorkflow
{
    private readonly IPeriodEditingService _periodEditingService;

    public TimelineEditingWorkflow(IPeriodEditingService periodEditingService)
    {
        _periodEditingService = periodEditingService;
    }

    public IStaff CreateStaff(string classNamePrefix, bool isChart, string displayName, string? formName = null)
    {
        if (formName != null)
            return new ElementStaff(classNamePrefix, isChart, displayName, formName);
        return new AudioStaff(classNamePrefix, isChart, displayName);
    }

    public string GetNextClassName(SimulationScore score, string prefix)
    {
        var existingNumbers = new System.Collections.Generic.List<int>();
        foreach (var s in score.Stave)
        {
            if (s.ClassName.StartsWith(prefix))
            {
                var numPart = s.ClassName[prefix.Length..];
                if (int.TryParse(numPart, out var n))
                    existingNumbers.Add(n);
            }
        }
        var nextNumber = existingNumbers.Count > 0 ? existingNumbers.Max() + 1 : 1;
        return $"{prefix}{nextNumber}";
    }

    public void AddStaff(SimulationScore score, IStaff staff)
    {
        score.Stave.Add(staff);
    }

    public void RemoveStaff(SimulationScore score, int index)
    {
        if (index >= 0 && index < score.Stave.Count)
            score.Stave.RemoveAt(index);
    }

    public void CopyStaff(SimulationScore score, int index)
    {
        if (index < 0 || index >= score.Stave.Count)
            return;

        var source = score.Stave[index];
        var clonedStaff = source.Clone();
        var baseName = source.ClassName;
        var newName = baseName;
        var counter = 1;
        while (score.CheckStaffNameConflict(newName))
        {
            newName = $"{baseName}{counter}";
            counter++;
        }
        clonedStaff.ClassName = newName;

        var copyDisplayName = $"{(string.IsNullOrWhiteSpace(source.DisplayName) ? source.ClassName : source.DisplayName)} 副本";
        if (copyDisplayName.Length > 64)
            copyDisplayName = copyDisplayName[..64];
        clonedStaff.DisplayName = copyDisplayName;

        score.Stave.Insert(index + 1, clonedStaff);
    }

    public void RenameStaffDisplayName(SimulationScore score, int index, string newDisplayName)
    {
        if (index >= 0 && index < score.Stave.Count)
            score.Stave[index].DisplayName = newDisplayName;
    }

    public IPeriod CreatePeriod(IStaff staff, SimulationScore score, float timeOffsetSeconds)
    {
        return _periodEditingService.CreatePeriod(staff, score, timeOffsetSeconds);
    }

    public IPeriod InsertPeriod(IStaff staff, IPeriod period)
    {
        return _periodEditingService.InsertPeriod(staff, period);
    }

    public IPeriod DuplicatePeriod(IStaff staff, IPeriod source)
    {
        return _periodEditingService.DuplicatePeriod(staff, source);
    }

    public void RemovePeriod(IStaff staff, IPeriod period)
    {
        _periodEditingService.RemovePeriod(staff, period);
    }

    public void UpdatePeriodTimeOffset(IPeriod period, float timeOffsetSeconds)
    {
        _periodEditingService.UpdatePeriodTimeOffset(period, timeOffsetSeconds);
    }

    public void UpdatePeriodMinLength(IPeriod period, float minLengthSeconds)
    {
        _periodEditingService.UpdatePeriodMinLength(period, minLengthSeconds);
    }

    public double SnapTime(double seconds, bool snapEnabled, TimelineSnapMode snapMode,
        int bpm, int offset, int beatsPerBar, int subdivisionsPerBeat)
    {
        return TimelineSnapper.Snap(seconds, snapEnabled, snapMode, bpm, offset, beatsPerBar, subdivisionsPerBeat);
    }

    public double SnapDuration(double duration, bool snapEnabled, TimelineSnapMode snapMode,
        int bpm, int beatsPerBar, int subdivisionsPerBeat)
    {
        return TimelineSnapper.SnapDuration(duration, snapEnabled, snapMode, bpm, beatsPerBar, subdivisionsPerBeat);
    }
}
