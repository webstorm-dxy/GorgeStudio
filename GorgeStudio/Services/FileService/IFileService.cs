using System;
using System.Threading;
using System.Threading.Tasks;
using GorgeStudio.Models;

namespace GorgeStudio.Services;

public interface IFileService
{
    Task<CompileResult> LoadAndCompileFileAsync(
        string filePath,
        bool isChart = true,
        IProgress<float>? progress = null,
        CancellationToken ct = default);

    Task<CompileResult> LoadAndCompileDirectoryAsync(
        string directoryPath,
        bool recursive = false,
        bool isChart = true,
        IProgress<float>? progress = null,
        CancellationToken ct = default);

    Task<CompileResult> LoadAndCompileZipAsync(
        string zipFilePath,
        bool isChart = true,
        IProgress<float>? progress = null,
        CancellationToken ct = default);

    Task<CompileResult> LoadAndCompileZipAsync(
        byte[] zipData,
        bool isChart = true,
        IProgress<float>? progress = null,
        CancellationToken ct = default);

    event Action<string>? StatusChanged;
}
