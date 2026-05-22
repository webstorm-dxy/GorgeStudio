using System.Collections.Generic;

namespace GorgeStudio.Models;

/// <summary>
/// 表示一个已编译的 Gorge 枚举的元数据信息。
/// 枚举包含一组命名的常量值，可附带用于 UI 展示的显示名称。
/// </summary>
public class CompiledEnumInfo
{
    /// <summary>
    /// 枚举的完全限定名称，格式为 "Namespace.EnumName"。
    /// </summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// 枚举所在的命名空间。
    /// </summary>
    public string Namespace { get; init; } = string.Empty;

    /// <summary>
    /// 指示此枚举是否为本地（Native）实现，由运行时提供。
    /// </summary>
    public bool IsNative { get; init; }

    /// <summary>
    /// 枚举值的内部名称列表，按定义顺序排列。
    /// 每个值对应一个唯一的整数常量。
    /// </summary>
    public IReadOnlyList<string> Values { get; init; } = new List<string>();

    /// <summary>
    /// 枚举值的显示名称列表，与 <see cref="Values"/> 一一对应。
    /// 显示名称用于 UI 展示，可能包含空格和特殊字符；内部名称用于代码引用。
    /// </summary>
    public IReadOnlyList<string> DisplayNames { get; init; } = new List<string>();
}
