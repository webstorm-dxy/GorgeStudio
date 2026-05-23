using System.Collections.Generic;

namespace GorgeStudio.Models;

/// <summary>
/// 表示 Gorge 类中一个字段的元数据信息。
/// 字段存储类的实例数据，具有类型、名称和索引位置。
/// </summary>
public class FieldInfo
{
    /// <summary>
    /// 字段在编译上下文中的唯一标识符。
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// 字段的名称，例如 "health"、"position"。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 字段类型的名称字符串。基本类型为 "int"、"float"、"bool"、"string" 等；
    /// 引用类型为完全限定名称，例如 "MyGame.Vector3"。
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// 字段在类中的声明顺序索引。从 0 开始计数，用于确定字段在对象内存布局中的位置。
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// 附加到此字段的注解列表。
    /// 例如 <c>@Display(name = "生命值")</c> 可用于 UI 属性面板展示。
    /// </summary>
    public IReadOnlyList<AnnotationInfo> Annotations { get; init; } = new List<AnnotationInfo>();
}
