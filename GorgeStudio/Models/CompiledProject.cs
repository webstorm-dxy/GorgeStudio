using System.Collections.Generic;
using System.Linq;

namespace GorgeStudio.Models;

/// <summary>
/// 表示一个完整的编译后的 Gorge 项目。
/// 包含所有已编译的类、枚举、接口，并按命名空间分组，同时区分谱面代码和库代码。
/// 该类是编译结果的核心数据载体，供 UI 各面板消费。
/// </summary>
public class CompiledProject
{
    /// <summary>
    /// 项目中所有已编译类的列表（不含 Native 类）。
    /// </summary>
    public IReadOnlyList<CompiledClassInfo> Classes { get; init; } = new List<CompiledClassInfo>();

    /// <summary>
    /// 项目中所有已编译枚举的列表（不含 Native 枚举）。
    /// </summary>
    public IReadOnlyList<CompiledEnumInfo> Enums { get; init; } = new List<CompiledEnumInfo>();

    /// <summary>
    /// 项目中所有已编译接口的列表（不含 Native 接口）。
    /// </summary>
    public IReadOnlyList<CompiledInterfaceInfo> Interfaces { get; init; } = new List<CompiledInterfaceInfo>();

    /// <summary>
    /// 按命名空间分组的类字典。键为命名空间全名，值为该命名空间下的所有类。
    /// 用于在元素列表面板中按命名空间层级展示类结构。
    /// </summary>
    public IReadOnlyDictionary<string, List<CompiledClassInfo>> ClassesByNamespace { get; init; }
        = new Dictionary<string, List<CompiledClassInfo>>();

    /// <summary>
    /// 项目中标记为"谱面代码"的类列表。谱面代码是用户编写的游戏逻辑，
    /// 区别于库代码（<see cref="LibraryClasses"/>）。
    /// </summary>
    public IReadOnlyList<CompiledClassInfo> ChartClasses { get; init; } = new List<CompiledClassInfo>();

    /// <summary>
    /// 项目中标记为"库代码"的类列表。库代码通常是可复用的基础组件，
    /// 区别于谱面代码（<see cref="ChartClasses"/>）。
    /// </summary>
    public IReadOnlyList<CompiledClassInfo> LibraryClasses { get; init; } = new List<CompiledClassInfo>();

    /// <summary>
    /// 根据编译产生的原始数据创建 <see cref="CompiledProject"/> 实例。
    /// 此方法会按命名空间对类进行分组，并区分谱面代码和库代码。
    /// </summary>
    /// <param name="classes">所有已编译的类列表。</param>
    /// <param name="enums">所有已编译的枚举列表。</param>
    /// <param name="interfaces">所有已编译的接口列表。</param>
    /// <returns>一个完整填充的 <see cref="CompiledProject"/> 实例，包含按命名空间分组和按类型分类的视图。</returns>
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
