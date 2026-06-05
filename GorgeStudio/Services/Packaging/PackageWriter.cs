using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Gorge.GorgeCompiler;
using GorgeStudio.Models;
using GorgeStudio.Models.Chart;

namespace GorgeStudio.Services.Packaging;

/// <summary>
/// 谱面包（ZIP）写入器接口。
/// </summary>
public interface IPackageWriter
{
    /// <summary>
    /// 将源文件列表、资源文件和项目设置打包为 ZIP 字节数组。
    /// </summary>
    /// <param name="sourceFiles">要打包的 Gorge 源文件。</param>
    /// <param name="assetFiles">要打包的资源文件（PNG、WAV 等）。可为空。</param>
    /// <param name="settings">项目设置。非 null 时序列化为 setting.json 写入 ZIP 根目录。</param>
    /// <param name="formSourceFiles">Form 源文件列表。非 null 时写入 Forms/ 子目录下。</param>
    /// <param name="formResourceFiles">Form 资源文件列表（PNG、WAV 等）。非 null 时写入 Forms/ 子目录下，保持相对路径。</param>
    /// <returns>ZIP 文件的完整二进制数据。</returns>
    byte[] WriteZip(IReadOnlyList<SourceCodeFile> sourceFiles, IReadOnlyList<AssetFile>? assetFiles = null, ProjectSettings? settings = null, IReadOnlyList<SourceCodeFile>? formSourceFiles = null, IReadOnlyList<AssetFile>? formResourceFiles = null);
}

/// <summary>
/// <see cref="IPackageWriter"/> 的默认实现。
/// 使用 System.IO.Compression.ZipArchive 创建 ZIP 文件。
/// </summary>
public sealed class PackageWriter : IPackageWriter
{
    public byte[] WriteZip(IReadOnlyList<SourceCodeFile> sourceFiles, IReadOnlyList<AssetFile>? assetFiles = null, ProjectSettings? settings = null, IReadOnlyList<SourceCodeFile>? formSourceFiles = null, IReadOnlyList<AssetFile>? formResourceFiles = null)
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            // 1) Write chart source files
            foreach (var sourceFile in sourceFiles)
            {
                if (!sourceFile.IsChartSourceCode) continue;
                var entry = archive.CreateEntry(sourceFile.Path, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                var bytes = Encoding.UTF8.GetBytes(sourceFile.Code);
                entryStream.Write(bytes, 0, bytes.Length);
            }

            // 2) Write form source files under Forms/{dirName}/
            if (formSourceFiles != null)
            {
                foreach (var formFile in formSourceFiles)
                {
                    var entry = archive.CreateEntry(formFile.Path, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    var bytes = Encoding.UTF8.GetBytes(formFile.Code);
                    entryStream.Write(bytes, 0, bytes.Length);
                }
            }

            // 3) Write form resource files (PNG, WAV, etc.) under Forms/{dirName}/
            if (formResourceFiles != null)
            {
                foreach (var resourceFile in formResourceFiles)
                {
                    var entry = archive.CreateEntry(resourceFile.Path, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    entryStream.Write(resourceFile.Data, 0, resourceFile.Data.Length);
                }
            }

            // 4) Write asset files
            if (assetFiles != null)
            {
                foreach (var assetFile in assetFiles)
                {
                    if (!assetFile.IsChartAsset) continue;
                    var entry = archive.CreateEntry(assetFile.Path, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    entryStream.Write(assetFile.Data, 0, assetFile.Data.Length);
                }
            }

            // 5) Write setting.json
            if (settings != null)
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                var entry = archive.CreateEntry("setting.json", CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                var bytes = Encoding.UTF8.GetBytes(json);
                entryStream.Write(bytes, 0, bytes.Length);
            }
        }

        return memoryStream.ToArray();
    }
}
