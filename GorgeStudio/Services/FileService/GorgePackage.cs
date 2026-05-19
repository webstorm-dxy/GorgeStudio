using System.Collections.Generic;
using Gorge.GorgeCompiler;

namespace GorgeStudio.Services.FileService;

internal class GorgePackage
{
    public string SourcePath { get; init; } = string.Empty;

    public IReadOnlyList<SourceCodeFile> SourceFiles { get; init; } = new List<SourceCodeFile>();

    public IReadOnlyList<string> AssetPaths { get; init; } = new List<string>();

    /// <summary>
    /// Source file path → IsChartSourceCode lookup for correlating compiled types back to their origin.
    /// </summary>
    public Dictionary<string, bool> SourcePathIsChart { get; init; } = new();
}
