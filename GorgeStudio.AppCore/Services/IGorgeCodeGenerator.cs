using System.Collections.Generic;
using Gorge.GorgeCompiler;
using GorgeStudio.Models.Chart;

namespace GorgeStudio.Services.CodeGeneration;

/// <summary>
/// 谱面源码生成器接口。
/// 将 <see cref="SimulationScore"/> 中的谱表数据生成为一组 Gorge 源文件。
/// </summary>
public interface IGorgeCodeGenerator
{
    /// <summary>
    /// 从仿真总谱生成 Gorge 源文件列表。
    /// 遍历每个谱面谱表，调用其 ToGorgeCode() 方法生成完整的 .g 文件内容。
    /// </summary>
    /// <param name="score">包含 Staff/Period/Element 层级的仿真总谱。</param>
    /// <returns>生成的源文件列表，每个 SourceCodeFile 的 Path 为 "ClassName.g"。</returns>
    IReadOnlyList<SourceCodeFile> Generate(SimulationScore score);
}
