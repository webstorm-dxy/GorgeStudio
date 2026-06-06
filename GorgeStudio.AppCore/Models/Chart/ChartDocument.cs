using System.Collections.Generic;
using Gorge.GorgeLanguage.Objective;
using Gorge.Native.Gorge;

namespace GorgeStudio.Models.Chart;

/// <summary>
/// 谱面运行时文档模型。持有原始源码、编译后的项目元数据，
/// 以及每个谱面类对应的可编辑 Injector 实例。
/// </summary>
public class ChartDocument
{
    /// <summary>
    /// 编译产生的项目元数据（类、枚举、接口的签名信息）。
    /// </summary>
    public CompiledProject CompiledProject { get; init; } = null!;

    /// <summary>
    /// 谱面类的类声明集合（完全限定名 → ClassDeclaration）。
    /// 用于创建和操作 Injector 实例。
    /// </summary>
    public IReadOnlyDictionary<string, ClassDeclaration> ClassDeclarations { get; init; }
        = new Dictionary<string, ClassDeclaration>();

    /// <summary>
    /// 谱面类对应的可编辑 Injector 实例（完全限定名 → Injector）。
    /// 所有字段初始为默认值，可通过 UI 编辑。
    /// </summary>
    public Dictionary<string, Injector> Injectors { get; init; } = new();

    /// <summary>
    /// 标记为谱面代码的类列表（来自 CompiledProject.ChartClasses）。
    /// </summary>
    public IReadOnlyList<CompiledClassInfo> ChartClasses => CompiledProject.ChartClasses;
}
