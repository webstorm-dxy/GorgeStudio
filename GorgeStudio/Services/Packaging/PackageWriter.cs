using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Gorge.GorgeCompiler;
using GorgeStudio.Models.Chart;

namespace GorgeStudio.Services.Packaging;

/// <summary>
/// 谱面包（ZIP）写入器接口。
/// </summary>
public interface IPackageWriter
{
    /// <summary>
    /// 将源文件列表和资源文件打包为 ZIP 字节数组。
    /// </summary>
    /// <param name="sourceFiles">要打包的 Gorge 源文件。</param>
    /// <param name="assetFiles">要打包的资源文件（PNG、WAV 等）。可为空。</param>
    /// <returns>ZIP 文件的完整二进制数据。</returns>
    byte[] WriteZip(IReadOnlyList<SourceCodeFile> sourceFiles, IReadOnlyList<AssetFile>? assetFiles = null);
}

/// <summary>
/// <see cref="IPackageWriter"/> 的默认实现。
/// 使用 System.IO.Compression.ZipArchive 创建 ZIP 文件。
/// </summary>
public sealed class PackageWriter : IPackageWriter
{
    public byte[] WriteZip(IReadOnlyList<SourceCodeFile> sourceFiles, IReadOnlyList<AssetFile>? assetFiles = null)
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var sourceFile in sourceFiles)
            {
                if (!sourceFile.IsChartSourceCode) continue;

                var entry = archive.CreateEntry(sourceFile.Path, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                var bytes = Encoding.UTF8.GetBytes(sourceFile.Code);
                entryStream.Write(bytes, 0, bytes.Length);
            }

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
        }

        return memoryStream.ToArray();
    }
}
