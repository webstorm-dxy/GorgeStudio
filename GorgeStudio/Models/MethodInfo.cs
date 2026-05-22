using System.Collections.Generic;

namespace GorgeStudio.Models;

/// <summary>
/// 表示 Gorge 语言中一个方法（实例方法或静态方法）的元数据信息。
/// 方法包含名称、返回类型、参数列表及注解，是类和接口的核心组成元素。
/// </summary>
public class MethodInfo
{
    /// <summary>
    /// 方法在编译上下文中的唯一标识符。
    /// 用于在编译数据中关联方法声明与其实现的中间代码。
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// 方法的名称，例如 "Update"、"GetHealth"。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 方法返回类型的名称字符串。基本类型为 "int"、"float"、"bool"、"string" 等；
    /// 引用类型为完全限定名称。无返回值（void）时为 <c>null</c>。
    /// </summary>
    public string? ReturnType { get; init; }

    /// <summary>
    /// 方法的参数列表，按声明顺序排列。
    /// 每个参数包含名称、类型、索引位置和唯一标识符。
    /// 无参数时为空列表。
    /// </summary>
    public IReadOnlyList<ParameterInfo> Parameters { get; init; } = new List<ParameterInfo>();

    /// <summary>
    /// 附加到此方法的注解列表。
    /// 例如 <c>@Native</c> 表示此方法由外部运行时实现。
    /// </summary>
    public IReadOnlyList<AnnotationInfo> Annotations { get; init; } = new List<AnnotationInfo>();
}
