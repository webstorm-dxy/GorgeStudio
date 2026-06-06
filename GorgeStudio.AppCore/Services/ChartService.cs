using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GorgeStudio.Models;
using GorgeStudio.Models.Chart;
using GorgeStudio.Services.CodeGeneration;

namespace GorgeStudio.Services.ChartService;

/// <summary>
/// <see cref="IChartService"/> 的默认实现。
/// 从编译结果中提取谱面类的 ClassDeclaration，识别 @ElementStaff / @AudioStaff 注解，
/// 构建完整的 Staff → Period → Element 层级。
/// </summary>
public sealed class ChartService : IChartService
{
    public Task<SimulationScore> BuildChartDocumentAsync(CompileResult result, CancellationToken ct = default)
    {
        if (result.Project == null || result.ClassDeclarations == null || result.AllClassDeclarations == null)
            throw new ArgumentException("CompileResult must have Project, ClassDeclarations and AllClassDeclarations.");

        // 设置枚举值查找表，供 InjectorHardcodeGenerator 使用
        SetEnumValues(result.Project.Enums);

        var score = new SimulationScore(0f, 100f, 1f);

        // 构建 Staff → Period 结构
        // 先设置 AllClassDeclarations，BuildFromClassDeclarations 中需要通过返回类型查找框架类的 ClassDeclaration
        score.ClassDeclarations = result.AllClassDeclarations;
        score.BuildFromClassDeclarations(result.ClassDeclarations);

        // 载入资产文件
        foreach (var assetFile in result.AssetFiles)
            score.ChartAssetFiles.Add(assetFile.Clone());

        return Task.FromResult(score);
    }

    private static void SetEnumValues(IReadOnlyList<CompiledEnumInfo> enums)
    {
        var dict = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var e in enums)
            dict[e.FullName] = e.Values;
        InjectorHardcodeGenerator.EnumValues = dict;
    }
}
