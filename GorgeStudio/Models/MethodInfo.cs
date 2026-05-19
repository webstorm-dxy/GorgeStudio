using System.Collections.Generic;

namespace GorgeStudio.Models;

public class MethodInfo
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ReturnType { get; init; }
    public IReadOnlyList<ParameterInfo> Parameters { get; init; } = new List<ParameterInfo>();
    public IReadOnlyList<AnnotationInfo> Annotations { get; init; } = new List<AnnotationInfo>();
}
