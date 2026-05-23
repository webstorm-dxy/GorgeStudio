using System.Collections.Generic;
using Gorge.GorgeCompiler;

namespace GorgeStudio.Services.FileService;

/// <summary>
/// 表示一个待编译的 Gorge 源码包。
/// 封装了源文件集合、资源文件路径以及源文件到"是否为谱面代码"的映射关系。
/// 该对象由文件加载方法创建，随后传递给编译器进行编译。
/// </summary>
internal class GorgePackage
{
    /// <summary>
    /// 包的根路径。对于单个文件，为文件的完整路径；对于目录，为目录路径；
    /// 对于 Zip 包，为 Zip 文件路径或 "&lt;memory&gt;"。
    /// </summary>
    public string SourcePath { get; init; } = string.Empty;

    /// <summary>
    /// 包中包含的所有 Gorge 源文件（.g 文件）列表。
    /// 每个 <see cref="SourceCodeFile"/> 包含文件路径、源代码文本和是否为谱面代码的标记。
    /// </summary>
    public IReadOnlyList<SourceCodeFile> SourceFiles { get; init; } = new List<SourceCodeFile>();

    /// <summary>
    /// 包中包含的非 .g 资源文件列表。
    /// 这些文件不会参与编译，但会被记录以便后续使用（例如纹理、音频等资源）。
    /// </summary>
    public IReadOnlyList<string> AssetPaths { get; init; } = new List<string>();

    /// <summary>
    /// 包中包含的二进制资源文件数据（路径 → 字节数据）。
    /// 从 ZIP 加载时填充；从目录加载时仅记录路径，数据为 null。
    /// </summary>
    public IReadOnlyList<Models.Chart.AssetFile> AssetFiles { get; init; } = new List<Models.Chart.AssetFile>();

    /// <summary>
    /// 源文件路径到"是否为谱面代码"的映射字典。
    /// 键为源文件的完整路径，值为是否标记为谱面代码（与库代码相对）。
    /// 此映射用于在编译后正确标注每个编译类型的来源属性。
    /// </summary>
    public Dictionary<string, bool> SourcePathIsChart { get; init; } = new();

    /// <summary>
    /// 从包中解析出的项目设置。当包中包含 setting.json 时填充，否则为 null。
    /// </summary>
    public Models.ProjectSettings? Settings { get; init; }
}
