using System.Collections.Generic;

namespace GorgeStudio.Models;

public class ConstructorInfo
{
    public int Id { get; init; }
    public IReadOnlyList<ParameterInfo> Parameters { get; init; } = new List<ParameterInfo>();
    public IReadOnlyList<AnnotationInfo> Annotations { get; init; } = new List<AnnotationInfo>();
}
