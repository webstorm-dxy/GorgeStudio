using System;
using System.Collections.Generic;

namespace GorgeStudio.Models;

public class CompileResult
{
    public CompiledProject? Project { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<string> SourceFilePaths { get; init; } = new List<string>();
    public TimeSpan CompileTime { get; init; }
}
