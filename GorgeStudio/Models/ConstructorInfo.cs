using System.Collections.Generic;

namespace GorgeStudio.Models;

/// <summary>
/// 表示 Gorge 类中一个构造函数的元数据信息。
/// 构造函数用于初始化类的新实例，可接受参数并携带注解。
/// </summary>
public class ConstructorInfo
{
    /// <summary>
    /// 构造函数在编译上下文中的唯一标识符。
    /// 用于在编译数据中关联构造函数与其实现的中间代码。
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// 构造函数的参数列表，按声明顺序排列。
    /// 每个参数包含名称、类型、索引位置和唯一标识符。
    /// </summary>
    public IReadOnlyList<ParameterInfo> Parameters { get; init; } = new List<ParameterInfo>();

    /// <summary>
    /// 附加到此构造函数的注解列表。
    /// 例如可使用 <c>@Inject</c> 标记构造函数用于依赖注入。
    /// </summary>
    public IReadOnlyList<AnnotationInfo> Annotations { get; init; } = new List<AnnotationInfo>();
}
