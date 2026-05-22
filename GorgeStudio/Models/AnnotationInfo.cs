using System.Collections.Generic;

namespace GorgeStudio.Models;

/// <summary>
/// 表示 Gorge 语言中一个注解（Annotation）的元数据信息。
/// 注解类似于 C# 的 Attribute，可附加到类、方法、字段等元素上，携带键值对参数。
/// </summary>
public class AnnotationInfo
{
    /// <summary>
    /// 注解的名称，例如 "Native"、"Inject"、"Display" 等。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 注解的泛型类型参数（如果有），以完全限定名称表示。
    /// 例如 <c>List&lt;int&gt;</c> 中的泛型信息。无泛型参数时为 <c>null</c>。
    /// </summary>
    public string? GenericType { get; init; }

    /// <summary>
    /// 注解携带的键值对参数集合。键为参数名，值为参数值（可为任意类型或 <c>null</c>）。
    /// 例如 <c>@Display(name = "MyClass", order = 1)</c> 中，name 和 order 即为参数。
    /// </summary>
    public IReadOnlyDictionary<string, object?> Parameters { get; init; } = new Dictionary<string, object?>();
}
