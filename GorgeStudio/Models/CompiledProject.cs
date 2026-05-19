using System.Collections.Generic;
using System.Linq;

namespace GorgeStudio.Models;

public class CompiledProject
{
    public IReadOnlyList<CompiledClassInfo> Classes { get; init; } = new List<CompiledClassInfo>();
    public IReadOnlyList<CompiledEnumInfo> Enums { get; init; } = new List<CompiledEnumInfo>();
    public IReadOnlyList<CompiledInterfaceInfo> Interfaces { get; init; } = new List<CompiledInterfaceInfo>();
    public IReadOnlyDictionary<string, List<CompiledClassInfo>> ClassesByNamespace { get; init; }
        = new Dictionary<string, List<CompiledClassInfo>>();
    public IReadOnlyList<CompiledClassInfo> ChartClasses { get; init; } = new List<CompiledClassInfo>();
    public IReadOnlyList<CompiledClassInfo> LibraryClasses { get; init; } = new List<CompiledClassInfo>();

    public static CompiledProject Create(IReadOnlyList<CompiledClassInfo> classes,
        IReadOnlyList<CompiledEnumInfo> enums,
        IReadOnlyList<CompiledInterfaceInfo> interfaces)
    {
        return new CompiledProject
        {
            Classes = classes,
            Enums = enums,
            Interfaces = interfaces,
            ClassesByNamespace = classes
                .GroupBy(c => c.Namespace)
                .ToDictionary(g => g.Key, g => g.ToList()),
            ChartClasses = classes.Where(c => c.IsChartCode).ToList(),
            LibraryClasses = classes.Where(c => !c.IsChartCode).ToList()
        };
    }
}
