namespace GorgeStudio.Models;

/// <summary>
/// 表示 Gorge 类中一个注入字段（Injector Field）的元数据信息。
/// 注入字段是由依赖注入容器在对象构造时自动赋值的特殊字段，
/// 区别于普通字段，注入字段不需要用户手动初始化。
/// </summary>
public class InjectorFieldInfo
{
    /// <summary>
    /// 注入字段在编译上下文中的唯一标识符。
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// 注入字段的名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 注入字段类型的名称字符串。基本类型为 "int"、"float" 等；
    /// 引用类型为完全限定名称。
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// 注入字段在类中的声明顺序索引。
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// 默认值的索引引用。指向固定值池（FixedFieldValuePool）中的条目。
    /// 当 DI 容器无法解析该字段时，将使用此默认值。
    /// 如果没有指定默认值，则为 <c>null</c>。
    /// </summary>
    public int? DefaultValueIndex { get; init; }
}
