using System.Collections.Generic;

namespace GorgeStudio.Models;

public class FieldInfo
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public int Index { get; init; }
    public IReadOnlyList<AnnotationInfo> Annotations { get; init; } = new List<AnnotationInfo>();
}
