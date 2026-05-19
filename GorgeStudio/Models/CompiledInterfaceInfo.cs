using System.Collections.Generic;

namespace GorgeStudio.Models;

public class CompiledInterfaceInfo
{
    public string FullName { get; init; } = string.Empty;
    public string Namespace { get; init; } = string.Empty;
    public bool IsNative { get; init; }
    public IReadOnlyList<MethodInfo> Methods { get; init; } = new List<MethodInfo>();
}
