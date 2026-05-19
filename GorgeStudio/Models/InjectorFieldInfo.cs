namespace GorgeStudio.Models;

public class InjectorFieldInfo
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public int Index { get; init; }
    public int? DefaultValueIndex { get; init; }
}
