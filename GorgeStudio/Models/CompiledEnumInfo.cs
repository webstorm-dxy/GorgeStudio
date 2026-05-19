using System.Collections.Generic;

namespace GorgeStudio.Models;

public class CompiledEnumInfo
{
    public string FullName { get; init; } = string.Empty;
    public string Namespace { get; init; } = string.Empty;
    public bool IsNative { get; init; }
    public IReadOnlyList<string> Values { get; init; } = new List<string>();
    public IReadOnlyList<string> DisplayNames { get; init; } = new List<string>();
}
