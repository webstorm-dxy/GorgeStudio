using System.Collections.Generic;
using System.Linq;
using Gorge.GorgeCompiler;
using GorgeStudio.Models.Chart;

namespace GorgeStudio.Services.CodeGeneration;

/// <summary>
/// <see cref="IGorgeCodeGenerator"/> 的默认实现。
/// 从 SimulationScore 的 Staff/Period/Element 层级生成 Gorge 源码文件。
/// </summary>
public sealed class GorgeCodeGenerator : IGorgeCodeGenerator
{
    public IReadOnlyList<SourceCodeFile> Generate(SimulationScore score)
    {
        var result = new List<SourceCodeFile>();

        foreach (var staff in score.Stave.Where(s => s.IsChartClass))
        {
            var code = staff.ToGorgeCode();
            result.Add(new SourceCodeFile(staff.ClassName + ".g", code, true));
        }

        foreach (var loader in score.AssetLoaders.Where(a => a.IsChartClass))
        {
            var code = loader.ToGorgeCode();
            result.Add(new SourceCodeFile(loader.ClassName + ".g", code, true));
        }

        return result;
    }
}
