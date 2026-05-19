using System.Collections.Generic;

namespace GorgeStudio.Models;

public class CompiledClassInfo
{
    public string FullName { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public string Namespace { get; init; } = string.Empty;
    public bool IsNative { get; init; }
    public bool IsChartCode { get; init; }
    public string? SuperClassName { get; init; }
    public IReadOnlyList<string> InterfaceNames { get; init; } = new List<string>();
    public IReadOnlyList<string> AnnotationNames { get; init; } = new List<string>();
    public IReadOnlyList<FieldInfo> Fields { get; init; } = new List<FieldInfo>();
    public IReadOnlyList<MethodInfo> Methods { get; init; } = new List<MethodInfo>();
    public IReadOnlyList<MethodInfo> StaticMethods { get; init; } = new List<MethodInfo>();
    public IReadOnlyList<ConstructorInfo> Constructors { get; init; } = new List<ConstructorInfo>();
    public IReadOnlyList<InjectorFieldInfo> InjectorFields { get; init; } = new List<InjectorFieldInfo>();
    public IReadOnlyList<AnnotationInfo> Annotations { get; init; } = new List<AnnotationInfo>();
    public int InheritanceDepth { get; init; }
}
