namespace GorgeStudio.Models;

/// <summary>
/// 表示 Gorge 方法或构造函数中一个参数的元数据信息。
/// 参数定义了调用方法时需要传入的数据，包含名称、类型和位置索引。
/// </summary>
public class ParameterInfo
{
    /// <summary>
    /// 参数在编译上下文中的唯一标识符。
    /// 用于在编译数据中将参数声明与其在虚机栈中的位置关联起来。
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// 参数的名称，即方法签名中声明的形参名。例如 "x"、"target"。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 参数类型的名称字符串。基本类型为 "int"、"float"、"bool"、"string" 等；
    /// 引用类型为完全限定名称，例如 "MyGame.Player"。
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// 参数在方法参数列表中的位置索引（从 0 开始）。
    /// 用于确定参数在调用时的传参顺序以及虚机栈帧中的位置。
    /// </summary>
    public int Index { get; init; }
}
