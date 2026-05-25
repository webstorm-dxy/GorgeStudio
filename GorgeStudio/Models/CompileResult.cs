using System;
using System.Collections.Generic;
using GorgeStudio.Models.Chart;

namespace GorgeStudio.Models;

/// <summary>
/// 封装一次编译操作的结果，包含编译是否成功、产生的项目数据、错误信息及耗时等。
/// 无论编译成功或失败，都会返回此对象；调用方应检查 <see cref="Success"/> 以判断结果。
/// </summary>
public class CompileResult
{
    /// <summary>
    /// 编译成功时产生的项目数据，包含所有已编译的类、枚举、接口信息。
    /// 编译失败时为 <c>null</c>。
    /// </summary>
    public CompiledProject? Project { get; init; }

    /// <summary>
    /// 指示编译是否成功完成。为 <c>true</c> 时 <see cref="Project"/> 不为 <c>null</c>；
    /// 为 <c>false</c> 时应检查 <see cref="ErrorMessage"/> 获取失败原因。
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 编译失败时的错误描述信息。编译成功时此字段为 <c>null</c>。
    /// 可能包含编译器异常消息、文件未找到等错误文本。
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 参与此次编译的所有源文件路径列表。可用于在 UI 中展示编译了哪些文件。
    /// </summary>
    public IReadOnlyList<string> SourceFilePaths { get; init; } = new List<string>();

    /// <summary>
    /// 编译操作的总耗时，从开始加载源文件到编译完成的墙上时钟时间。
    /// 可用于性能分析或在状态栏中显示。
    /// </summary>
    public TimeSpan CompileTime { get; init; }

    /// <summary>
    /// 编译产生的运行时类声明映射（类完全限定名 → ClassDeclaration）。
    /// 仅在编译成功时非 null，供 ChartService 创建可编辑 Injector 使用。
    /// 仅包含非 Native 类。
    /// </summary>
    public IReadOnlyDictionary<string, Gorge.GorgeLanguage.Objective.ClassDeclaration>? ClassDeclarations { get; init; }

    /// <summary>
    /// 所有运行时类声明映射（类完全限定名 → ClassDeclaration），包含 Native 和非 Native 类型。
    /// 仅在编译成功时非 null，供 PeriodEditingService 创建 GorgeFramework.PeriodConfig 等内建类型的 Injector。
    /// </summary>
    public IReadOnlyDictionary<string, Gorge.GorgeLanguage.Objective.ClassDeclaration>? AllClassDeclarations { get; init; }

    /// <summary>
    /// 包中的二进制资源文件列表（PNG、WAV 等）。
    /// 仅在从 ZIP 加载或目录加载时非空；单文件加载时为空。
    /// </summary>
    public IReadOnlyList<AssetFile> AssetFiles { get; init; } = new List<AssetFile>();

    /// <summary>
    /// 从包中提取的项目设置（如 setting.json）。
    /// 仅从 ZIP/GPGK 加载时非 null；单文件或目录加载时为 null。
    /// </summary>
    public ProjectSettings? Settings { get; init; }
}
