using System.Collections.Generic;

namespace GorgeStudio.Models;

/// <summary>
/// 表示一个已编译的 Gorge 类的完整元数据信息。
/// 包含类的名称、命名空间、继承关系、成员（字段、方法、构造函数、注入字段）及注解等。
/// 该类是 UI 面板（如属性面板、元素列表面板）展示类信息的主要数据载体。
/// </summary>
public class CompiledClassInfo
{
    /// <summary>
    /// 类的完全限定名称，格式为 "Namespace.ClassName"。
    /// 例如 "MyGame.Player"。
    /// </summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// 不包含命名空间的类名。例如 "Player"。
    /// </summary>
    public string ClassName { get; init; } = string.Empty;

    /// <summary>
    /// 类所在的命名空间。顶层命名空间为空字符串。
    /// </summary>
    public string Namespace { get; init; } = string.Empty;

    /// <summary>
    /// 指示此类是否为本地（Native）实现。Native 类由运行时提供，不由 Gorge 源码编译。
    /// </summary>
    public bool IsNative { get; init; }

    /// <summary>
    /// 指示此类是否为谱面代码（Chart Code）。
    /// 谱面代码属于用户编写的游戏逻辑，区别于库代码（Library Code）。
    /// </summary>
    public bool IsChartCode { get; init; }

    /// <summary>
    /// 父类的完全限定名称。如果此类无继承（隐式继承 Object），则为 <c>null</c>。
    /// </summary>
    public string? SuperClassName { get; init; }

    /// <summary>
    /// 此类实现的所有接口的完全限定名称列表。
    /// </summary>
    public IReadOnlyList<string> InterfaceNames { get; init; } = new List<string>();

    /// <summary>
    /// 附加到此类的注解名称列表。仅包含注解名，详细参数见 <see cref="Annotations"/>。
    /// </summary>
    public IReadOnlyList<string> AnnotationNames { get; init; } = new List<string>();

    /// <summary>
    /// 类的所有实例字段列表。每个字段包含名称、类型、索引位置和注解信息。
    /// </summary>
    public IReadOnlyList<FieldInfo> Fields { get; init; } = new List<FieldInfo>();

    /// <summary>
    /// 类的所有实例方法列表。每个方法包含名称、返回类型、参数列表和注解信息。
    /// </summary>
    public IReadOnlyList<MethodInfo> Methods { get; init; } = new List<MethodInfo>();

    /// <summary>
    /// 类的所有静态方法列表。结构同 <see cref="Methods"/>，但为静态上下文。
    /// </summary>
    public IReadOnlyList<MethodInfo> StaticMethods { get; init; } = new List<MethodInfo>();

    /// <summary>
    /// 类的所有构造函数列表。包含参数列表和注解信息。
    /// Gorge 支持多构造函数重载。
    /// </summary>
    public IReadOnlyList<ConstructorInfo> Constructors { get; init; } = new List<ConstructorInfo>();

    /// <summary>
    /// 类的所有注入字段（Injector Field）列表。注入字段用于依赖注入框架，
    /// 在对象构造时由容器自动填充。
    /// </summary>
    public IReadOnlyList<InjectorFieldInfo> InjectorFields { get; init; } = new List<InjectorFieldInfo>();

    /// <summary>
    /// 附加到此类上的完整注解信息列表。每个注解包含名称、泛型类型和键值对参数。
    /// 相比之下 <see cref="AnnotationNames"/> 仅包含名称列表，用于快速查找。
    /// </summary>
    public IReadOnlyList<AnnotationInfo> Annotations { get; init; } = new List<AnnotationInfo>();

    /// <summary>
    /// 类的继承深度。从 System.Object 开始计数，Object 自身深度为 0。
    /// 例如，如果类继承自某一级父类，则深度为 1。
    /// </summary>
    public int InheritanceDepth { get; init; }
}
