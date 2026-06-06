using System;
using System.Collections.Generic;
using GorgeStudio.Models;
using GorgeStudio.Models.Chart;

namespace GorgeStudio.AppCore.Models.Results;

public record LoadChartResult(
    bool Success,
    CompiledProject? Project,
    SimulationScore? Score,
    string? FilePath,
    string? ErrorMessage,
    List<FormInfo>? LoadedForms,
    TimeSpan CompileTime);

public record SaveChartResult(
    bool Success,
    bool Cancelled,
    string? FilePath,
    string? ErrorMessage);

public record LaunchGodotResult(
    bool Success,
    string? ErrorMessage,
    double DurationSeconds = 0,
    double BeginSeconds = 0,
    double EndSeconds = 0);

public record InspectResult(
    string Title,
    string Description,
    IReadOnlyList<PropertyEntry> Properties);

/// <summary>
/// 属性面板中单个属性的名称-值对。
/// </summary>
public class PropertyEntry
{
    public string Name { get; }
    public string Value { get; }

    public PropertyEntry(string name, string value)
    {
        Name = name;
        Value = value;
    }
}
