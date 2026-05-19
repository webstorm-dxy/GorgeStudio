using System.Collections.Generic;

namespace GorgeStudio.Models;

public class AnnotationInfo
{
    public string Name { get; init; } = string.Empty;
    public string? GenericType { get; init; }
    public IReadOnlyDictionary<string, object?> Parameters { get; init; } = new Dictionary<string, object?>();
}
