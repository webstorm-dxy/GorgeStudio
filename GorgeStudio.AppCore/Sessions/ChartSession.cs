using System.Collections.Generic;
using GorgeStudio.Models;
using GorgeStudio.Models.Chart;

namespace GorgeStudio.AppCore.Sessions;

public sealed class ChartSession
{
    public string? CurrentFilePath { get; set; }
    public CompiledProject? CurrentProject { get; set; }
    public SimulationScore? CurrentScore { get; set; }
    public ProjectSettings Settings { get; set; } = new();
    public List<FormInfo> LoadedForms { get; } = new();
}
