using System.Collections.Generic;

namespace GorgeStudio.Models;

/// <summary>
/// 表示一个已编译的 Gorge 接口的元数据信息。
/// 接口定义了一组方法签名，类通过实现接口来承诺提供这些方法。
/// </summary>
public class CompiledInterfaceInfo
{
    /// <summary>
    /// 接口的完全限定名称，格式为 "Namespace.InterfaceName"。
    /// </summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// 接口所在的命名空间。
    /// </summary>
    public string Namespace { get; init; } = string.Empty;

    /// <summary>
    /// 指示此接口是否为本地（Native）实现。
    /// </summary>
    public bool IsNative { get; init; }

    /// <summary>
    /// 接口定义的所有方法签名列表。每个方法包含名称、返回类型和参数列表。
    /// 实现此接口的类必须提供这些方法的具体实现。
    /// </summary>
    public IReadOnlyList<MethodInfo> Methods { get; init; } = new List<MethodInfo>();
}
