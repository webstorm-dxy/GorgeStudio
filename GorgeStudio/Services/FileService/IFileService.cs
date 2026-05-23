using System;
using System.Threading;
using System.Threading.Tasks;
using GorgeStudio.Models;

namespace GorgeStudio.Services;

/// <summary>
/// 文件服务的抽象接口，负责加载 Gorge 源文件（.g）并进行编译。
/// 支持多种加载源：单文件、目录、Zip 文件以及内存中的 Zip 数据。
/// 所有方法均返回 <see cref="CompileResult"/>，包含编译结果或错误信息。
/// </summary>
public interface IFileService
{
    /// <summary>
    /// 加载并编译单个 Gorge 源文件（.g）。
    /// </summary>
    /// <param name="filePath">源文件的完整路径。</param>
    /// <param name="isChart">是否标记为谱面代码（<c>true</c>）或库代码（<c>false</c>）。默认 <c>true</c>。</param>
    /// <param name="progress">编译进度报告器，接收 0.0 到 1.0 之间的进度值。可为 <c>null</c>。</param>
    /// <param name="ct">取消令牌，用于取消编译操作。</param>
    /// <returns>包含编译结果或错误信息的 <see cref="CompileResult"/>。</returns>
    Task<CompileResult> LoadAndCompileFileAsync(
        string filePath,
        bool isChart = true,
        IProgress<float>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// 加载并编译目录中所有的 Gorge 源文件（*.g）。
    /// </summary>
    /// <param name="directoryPath">目录的完整路径。</param>
    /// <param name="recursive">是否递归搜索子目录。默认 <c>false</c>，仅搜索顶层。</param>
    /// <param name="isChart">是否将目录中所有源文件标记为谱面代码。默认 <c>true</c>。</param>
    /// <param name="progress">编译进度报告器。可为 <c>null</c>。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>包含编译结果或错误信息的 <see cref="CompileResult"/>。</returns>
    Task<CompileResult> LoadAndCompileDirectoryAsync(
        string directoryPath,
        bool recursive = false,
        bool isChart = true,
        IProgress<float>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// 加载并编译 Zip 文件中包含的所有 Gorge 源文件。
    /// </summary>
    /// <param name="zipFilePath">Zip 文件的完整路径。</param>
    /// <param name="isChart">是否将所有源文件标记为谱面代码。默认 <c>true</c>。</param>
    /// <param name="progress">编译进度报告器。可为 <c>null</c>。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>包含编译结果或错误信息的 <see cref="CompileResult"/>。</returns>
    Task<CompileResult> LoadAndCompileZipAsync(
        string zipFilePath,
        bool isChart = true,
        IProgress<float>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// 从内存中的 Zip 数据加载并编译所有 Gorge 源文件。
    /// 适用于已通过其他方式（如网络下载）获得二进制数据的场景。
    /// </summary>
    /// <param name="zipData">Zip 文件的二进制数据。</param>
    /// <param name="isChart">是否将所有源文件标记为谱面代码。默认 <c>true</c>。</param>
    /// <param name="progress">编译进度报告器。可为 <c>null</c>。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>包含编译结果或错误信息的 <see cref="CompileResult"/>。</returns>
    Task<CompileResult> LoadAndCompileZipAsync(
        byte[] zipData,
        bool isChart = true,
        IProgress<float>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// 文件加载和编译过程中的状态变化通知事件。
    /// </summary>
    event Action<string>? StatusChanged;
}
