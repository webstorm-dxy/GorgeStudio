using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gorge.GorgeCompiler;
using Gorge.GorgeCompiler.CompileContext;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeLanguage.Objective;
using GorgeStudio.Models;
using GorgeStudio.Models.Chart;

namespace GorgeStudio.Services.FileService;

/// <summary>
/// <see cref="IFileService"/> 的默认实现。
/// 负责从多种来源（单文件、目录、Zip 文件/数据）加载 Gorge 源文件并进行编译，
/// 最后将编译器内部的数据结构映射为 UI 友好的 <see cref="CompiledProject"/> 模型。
/// </summary>
/// <remarks>
/// 编译流程：
/// <list type="number">
/// <item>从源加载 Gorge 源码文件，构建 <see cref="GorgePackage"/></item>
/// <item>调用 <see cref="Compiler.CompileAsync"/> 进行四阶段编译</item>
/// <item>遍历编译结果的全局作用域，将编译数据映射为 <see cref="CompiledClassInfo"/> 等模型</item>
/// <item>通过 <see cref="CompiledProject.Create"/> 生成最终的项目视图</item>
/// </list>
/// 进度报告：文件加载占 0%-10%，编译占 10%-100%。
/// 所有异步操作支持 <see cref="CancellationToken"/> 取消。
/// </remarks>
public sealed class FileService : IFileService, IDisposable
{
    /// <inheritdoc/>
    public event Action<string>? StatusChanged;

    /// <summary>
    /// 加载并编译单个 Gorge 源文件（.g）。
    /// </summary>
    /// <param name="filePath">源文件的完整路径。</param>
    /// <param name="isChart">是否标记为谱面代码。默认 <c>true</c>。</param>
    /// <param name="progress">编译进度报告器。可为 <c>null</c>。</param>
    /// <param name="ct">取消令牌。取消时返回 <see cref="CompileResult"/> 且 <c>Success = false</c>。</param>
    /// <returns>包含编译结果或错误信息的 <see cref="CompileResult"/>。</returns>
    public async Task<CompileResult> LoadAndCompileFileAsync(
        string filePath, bool isChart = true,
        IProgress<float>? progress = null, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            ReportStatus($"Loading file: {filePath}");
            var package = await LoadPackageFromFileAsync(filePath, isChart, ct);

            ReportStatus($"Compiling {package.SourceFiles.Count} source file(s)...");
            var (project, classDecls) = await CompilePackageAsync(package, progress, ct);

            ReportStatus("Compilation complete.");
            return new CompileResult
            {
                Project = project,
                Success = true,
                SourceFilePaths = package.SourceFiles.Select(f => f.Path).ToList(),
                CompileTime = sw.Elapsed,
                ClassDeclarations = classDecls,
                AssetFiles = package.AssetFiles,
                Settings = package.Settings
            };
        }
        catch (OperationCanceledException)
        {
            return new CompileResult { Success = false, ErrorMessage = "Compilation cancelled.", CompileTime = sw.Elapsed };
        }
        catch (Exception ex)
        {
            return new CompileResult { Success = false, ErrorMessage = ex.Message, CompileTime = sw.Elapsed };
        }
    }

    /// <summary>
    /// 加载并编译目录中所有的 Gorge 源文件（*.g）。
    /// 如果目录中没有 .g 文件，返回失败的 <see cref="CompileResult"/>。
    /// </summary>
    /// <param name="directoryPath">目录的完整路径。</param>
    /// <param name="recursive">是否递归搜索子目录。默认 <c>false</c>。</param>
    /// <param name="isChart">是否将目录中所有源文件标记为谱面代码。默认 <c>true</c>。</param>
    /// <param name="progress">编译进度报告器。可为 <c>null</c>。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>包含编译结果或错误信息的 <see cref="CompileResult"/>。</returns>
    public async Task<CompileResult> LoadAndCompileDirectoryAsync(
        string directoryPath, bool recursive = false, bool isChart = true,
        IProgress<float>? progress = null, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            ReportStatus($"Loading directory: {directoryPath}");
            var package = await LoadPackageFromDirectoryAsync(directoryPath, recursive, isChart, ct);

            if (package.SourceFiles.Count == 0)
            {
                return new CompileResult
                {
                    Success = false,
                    ErrorMessage = "No .g source files found in the directory.",
                    CompileTime = sw.Elapsed
                };
            }

            ReportStatus($"Compiling {package.SourceFiles.Count} source file(s)...");
            var (project, classDecls) = await CompilePackageAsync(package, progress, ct);

            ReportStatus("Compilation complete.");
            return new CompileResult
            {
                Project = project,
                Success = true,
                SourceFilePaths = package.SourceFiles.Select(f => f.Path).ToList(),
                CompileTime = sw.Elapsed,
                ClassDeclarations = classDecls,
                AssetFiles = package.AssetFiles,
                Settings = package.Settings
            };
        }
        catch (OperationCanceledException)
        {
            return new CompileResult { Success = false, ErrorMessage = "Compilation cancelled.", CompileTime = sw.Elapsed };
        }
        catch (Exception ex)
        {
            return new CompileResult { Success = false, ErrorMessage = ex.Message, CompileTime = sw.Elapsed };
        }
    }

    /// <summary>
    /// 从 Zip 文件加载并编译所有 Gorge 源文件。
    /// </summary>
    /// <param name="zipFilePath">Zip 文件的完整路径。</param>
    /// <param name="isChart">是否将所有源文件标记为谱面代码。默认 <c>true</c>。</param>
    /// <param name="progress">编译进度报告器。可为 <c>null</c>。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>包含编译结果或错误信息的 <see cref="CompileResult"/>。</returns>
    public async Task<CompileResult> LoadAndCompileZipAsync(
        string zipFilePath, bool isChart = true,
        IProgress<float>? progress = null, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            ReportStatus($"Loading zip: {zipFilePath}");
            using var zipStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var package = LoadPackageFromZip(zipStream, zipFilePath, isChart);

            if (package.SourceFiles.Count == 0)
            {
                return new CompileResult
                {
                    Success = false,
                    ErrorMessage = "No .g source files found in the zip archive.",
                    CompileTime = sw.Elapsed
                };
            }

            ReportStatus($"Compiling {package.SourceFiles.Count} source file(s)...");
            var (project, classDecls) = await CompilePackageAsync(package, progress, ct);

            ReportStatus("Compilation complete.");
            return new CompileResult
            {
                Project = project,
                Success = true,
                SourceFilePaths = package.SourceFiles.Select(f => f.Path).ToList(),
                CompileTime = sw.Elapsed,
                ClassDeclarations = classDecls,
                AssetFiles = package.AssetFiles,
                Settings = package.Settings
            };
        }
        catch (OperationCanceledException)
        {
            return new CompileResult { Success = false, ErrorMessage = "Compilation cancelled.", CompileTime = sw.Elapsed };
        }
        catch (Exception ex)
        {
            return new CompileResult { Success = false, ErrorMessage = ex.Message, CompileTime = sw.Elapsed };
        }
    }

    /// <summary>
    /// 从内存中的 Zip 二进制数据加载并编译所有 Gorge 源文件。
    /// 适用于已通过网络或其他方式获取 Zip 数据的场景。
    /// </summary>
    /// <param name="zipData">Zip 文件的完整二进制内容。</param>
    /// <param name="isChart">是否将所有源文件标记为谱面代码。默认 <c>true</c>。</param>
    /// <param name="progress">编译进度报告器。可为 <c>null</c>。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>包含编译结果或错误信息的 <see cref="CompileResult"/>。</returns>
    public async Task<CompileResult> LoadAndCompileZipAsync(
        byte[] zipData, bool isChart = true,
        IProgress<float>? progress = null, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            ReportStatus("Loading zip from memory...");
            using var zipStream = new MemoryStream(zipData);
            var package = LoadPackageFromZip(zipStream, "<memory>", isChart);

            if (package.SourceFiles.Count == 0)
            {
                return new CompileResult
                {
                    Success = false,
                    ErrorMessage = "No .g source files found in the zip data.",
                    CompileTime = sw.Elapsed
                };
            }

            ReportStatus($"Compiling {package.SourceFiles.Count} source file(s)...");
            var (project, classDecls) = await CompilePackageAsync(package, progress, ct);

            ReportStatus("Compilation complete.");
            return new CompileResult
            {
                Project = project,
                Success = true,
                SourceFilePaths = package.SourceFiles.Select(f => f.Path).ToList(),
                CompileTime = sw.Elapsed,
                ClassDeclarations = classDecls,
                AssetFiles = package.AssetFiles,
                Settings = package.Settings
            };
        }
        catch (OperationCanceledException)
        {
            return new CompileResult { Success = false, ErrorMessage = "Compilation cancelled.", CompileTime = sw.Elapsed };
        }
        catch (Exception ex)
        {
            return new CompileResult { Success = false, ErrorMessage = ex.Message, CompileTime = sw.Elapsed };
        }
    }

    /// <summary>
    /// 释放文件服务占用的资源。清除事件订阅，防止内存泄漏。
    /// </summary>
    public void Dispose()
    {
        StatusChanged = null;
    }

    private static readonly HashSet<string> BuiltinDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Native", "DremuBase", "NativeDocs"
    };

    /// <inheritdoc/>
    public Task<List<FormInfo>> DiscoverFormsAsync(string formsRootPath)
    {
        var forms = new List<FormInfo>();
        var fullPath = Path.GetFullPath(formsRootPath);

        if (!Directory.Exists(fullPath))
            return Task.FromResult(forms);

        foreach (var subDir in Directory.EnumerateDirectories(fullPath))
        {
            var dirName = Path.GetFileName(subDir);
            if (BuiltinDirectoryNames.Contains(dirName))
                continue;

            var gFiles = Directory.EnumerateFiles(subDir, "*.g", SearchOption.TopDirectoryOnly).ToList();
            if (gFiles.Count == 0)
                continue;

            string? formName = null;
            string? version = null;
            foreach (var gFile in gFiles)
            {
                var content = File.ReadAllText(gFile);
                var match = Regex.Match(content, @"@Form\s*\(\s*name\s*=\s*""([^""]*)""\s*,\s*version\s*=\s*""([^""]*)""");
                if (match.Success)
                {
                    formName = match.Groups[1].Value;
                    version = match.Groups[2].Value;
                    break;
                }
            }

            forms.Add(new FormInfo
            {
                Name = formName ?? dirName,
                DirectoryName = dirName,
                Version = version ?? "?",
                Path = subDir
            });
        }

        return Task.FromResult(forms);
    }

    /// <inheritdoc/>
    public async Task<CompileResult> LoadAndCompileMultipleDirectoriesAsync(
        IReadOnlyList<string> directoryPaths, bool recursive = true, bool isChart = true,
        IProgress<float>? progress = null, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            ReportStatus($"Loading {directoryPaths.Count} directories...");
            var package = await LoadPackageFromMultipleDirectoriesAsync(directoryPaths, recursive, isChart, ct);

            if (package.SourceFiles.Count == 0)
            {
                return new CompileResult
                {
                    Success = false,
                    ErrorMessage = "No .g source files found in the selected directories.",
                    CompileTime = sw.Elapsed
                };
            }

            ReportStatus($"Compiling {package.SourceFiles.Count} source file(s)...");
            var (project, classDecls) = await CompilePackageAsync(package, progress, ct);

            ReportStatus("Compilation complete.");
            return new CompileResult
            {
                Project = project,
                Success = true,
                SourceFilePaths = package.SourceFiles.Select(f => f.Path).ToList(),
                CompileTime = sw.Elapsed,
                ClassDeclarations = classDecls,
                AssetFiles = package.AssetFiles,
                Settings = package.Settings
            };
        }
        catch (OperationCanceledException)
        {
            return new CompileResult { Success = false, ErrorMessage = "Compilation cancelled.", CompileTime = sw.Elapsed };
        }
        catch (Exception ex)
        {
            return new CompileResult { Success = false, ErrorMessage = ex.Message, CompileTime = sw.Elapsed };
        }
    }

    #region File Loading

    /// <summary>
    /// 从单个文件路径加载 Gorge 源码，构建 <see cref="GorgePackage"/>。
    /// </summary>
    /// <param name="filePath">源文件完整路径。</param>
    /// <param name="isChart">是否为谱面代码。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>包含单个源文件和路径映射的 <see cref="GorgePackage"/>。</returns>
    private static async Task<GorgePackage> LoadPackageFromFileAsync(string filePath, bool isChart,
        CancellationToken ct = default)
    {
        var fullPath = Path.GetFullPath(filePath);
        var code = await File.ReadAllTextAsync(fullPath, ct);
        var sourceFile = new SourceCodeFile(fullPath, code, isChart);

        return new GorgePackage
        {
            SourcePath = fullPath,
            SourceFiles = new[] { sourceFile },
            SourcePathIsChart = new Dictionary<string, bool> { { fullPath, isChart } }
        };
    }

    /// <summary>
    /// 从目录加载所有 .g 源文件和资源文件，构建 <see cref="GorgePackage"/>。
    /// </summary>
    /// <param name="directoryPath">目录完整路径。</param>
    /// <param name="recursive">是否递归搜索子目录。</param>
    /// <param name="isChart">是否将所有源文件标记为谱面代码。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>包含所有源文件和资源文件路径的 <see cref="GorgePackage"/>。</returns>
    private static async Task<GorgePackage> LoadPackageFromDirectoryAsync(
        string directoryPath, bool recursive, bool isChart, CancellationToken ct = default)
    {
        var fullPath = Path.GetFullPath(directoryPath);
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var filePaths = Directory.EnumerateFiles(fullPath, "*.g", searchOption).ToList();

        var sourceFiles = new List<SourceCodeFile>();
        var assetPaths = new List<string>();
        var sourcePathIsChart = new Dictionary<string, bool>();

        foreach (var filePath in filePaths)
        {
            var code = await File.ReadAllTextAsync(filePath, ct);
            // 路径中位于 Native 目录下的文件强制标记为库代码（非谱面代码）
            var nativeMarker = $"{Path.DirectorySeparatorChar}Native{Path.DirectorySeparatorChar}";
            var fileIsChart = isChart && !filePath.Contains(nativeMarker);
            sourceFiles.Add(new SourceCodeFile(filePath, code, fileIsChart));
            sourcePathIsChart[filePath] = fileIsChart;
        }

        // 收集非 .g 文件作为资源
        var allFiles = Directory.EnumerateFiles(fullPath, "*.*", searchOption);
        foreach (var file in allFiles)
        {
            if (!file.EndsWith(".g", StringComparison.OrdinalIgnoreCase))
            {
                assetPaths.Add(file);
            }
        }

        return new GorgePackage
        {
            SourcePath = fullPath,
            SourceFiles = sourceFiles,
            AssetPaths = assetPaths,
            SourcePathIsChart = sourcePathIsChart
        };
    }

    /// <summary>
    /// 从多个目录加载所有 .g 源文件和资源文件，合并为一个 <see cref="GorgePackage"/>。
    /// </summary>
    private static async Task<GorgePackage> LoadPackageFromMultipleDirectoriesAsync(
        IReadOnlyList<string> directoryPaths, bool recursive, bool isChart, CancellationToken ct = default)
    {
        var allSourceFiles = new List<SourceCodeFile>();
        var allAssetPaths = new List<string>();
        var allAssetFiles = new List<AssetFile>();
        var sourcePathIsChart = new Dictionary<string, bool>();

        foreach (var directoryPath in directoryPaths)
        {
            var fullPath = Path.GetFullPath(directoryPath);
            if (!Directory.Exists(fullPath))
                continue;

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var filePaths = Directory.EnumerateFiles(fullPath, "*.g", searchOption).ToList();

            foreach (var filePath in filePaths)
            {
                var code = await File.ReadAllTextAsync(filePath, ct);
                var nativeMarker = $"{Path.DirectorySeparatorChar}Native{Path.DirectorySeparatorChar}";
                var fileIsChart = isChart && !filePath.Contains(nativeMarker);
                allSourceFiles.Add(new SourceCodeFile(filePath, code, fileIsChart));
                sourcePathIsChart[filePath] = fileIsChart;
            }

            var allFiles = Directory.EnumerateFiles(fullPath, "*.*", searchOption);
            foreach (var file in allFiles)
            {
                if (!file.EndsWith(".g", StringComparison.OrdinalIgnoreCase))
                    allAssetPaths.Add(file);
            }
        }

        return new GorgePackage
        {
            SourcePath = string.Join("; ", directoryPaths),
            SourceFiles = allSourceFiles,
            AssetPaths = allAssetPaths,
            AssetFiles = allAssetFiles,
            SourcePathIsChart = sourcePathIsChart
        };
    }

    /// <summary>
    /// 从 Zip 流中加载所有 .g 源文件，构建 <see cref="GorgePackage"/>。
    /// </summary>
    /// <param name="zipStream">Zip 文件的只读流。方法内部会包装为 <see cref="ZipArchive"/>。</param>
    /// <param name="sourceName">源标识名称。对于文件为路径，对于内存数据为 "&lt;memory&gt;"。</param>
    /// <param name="isChart">是否将所有源文件标记为谱面代码。</param>
    /// <returns>包含从 Zip 提取的所有源文件的 <see cref="GorgePackage"/>。</returns>
    /// <remarks>
    /// 以 .g 扩展名结尾的条目被视为源码文件，其内容被读取为文本；
    /// 其他条目被视为资源文件，仅记录路径。
    /// Zip 内部的目录条目（无名称）会被跳过。
    /// </remarks>
    private static GorgePackage LoadPackageFromZip(Stream zipStream, string sourceName, bool isChart)
    {
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false);
        var sourceFiles = new List<SourceCodeFile>();
        var assetPaths = new List<string>();
        var assetFiles = new List<AssetFile>();
        var sourcePathIsChart = new Dictionary<string, bool>();
        ProjectSettings? projectSettings = null;

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            // 仅在 ZIP 根目录下的 setting.json 被视为项目设置
            if (entry.FullName.Equals("setting.json", StringComparison.OrdinalIgnoreCase))
            {
                using var reader = new StreamReader(entry.Open());
                var json = reader.ReadToEnd();
                if (!string.IsNullOrWhiteSpace(json))
                {
                    projectSettings = JsonSerializer.Deserialize<ProjectSettings>(json);
                }
                continue;
            }

            var entryPath = $"{sourceName}/{entry.FullName}";

            if (entry.Name.EndsWith(".g", StringComparison.OrdinalIgnoreCase))
            {
                using var reader = new StreamReader(entry.Open());
                var code = reader.ReadToEnd();
                sourceFiles.Add(new SourceCodeFile(entryPath, code, isChart));
                sourcePathIsChart[entryPath] = isChart;
            }
            else
            {
                assetPaths.Add(entryPath);
                using var entryStream = entry.Open();
                using var ms = new MemoryStream();
                entryStream.CopyTo(ms);
                assetFiles.Add(new AssetFile(entry.FullName, ms.ToArray(), isChart));
            }
        }

        return new GorgePackage
        {
            SourcePath = sourceName,
            SourceFiles = sourceFiles,
            AssetPaths = assetPaths,
            AssetFiles = assetFiles,
            SourcePathIsChart = sourcePathIsChart,
            Settings = projectSettings
        };
    }

    #endregion

    #region Compilation

    /// <summary>
    /// 缓存的 Forms/ 内建源文件，所有编译都会自动包含它们以提供完整的类型定义。
    /// </summary>
    private static List<SourceCodeFile>? _cachedBuiltinFiles;
    private static readonly object _builtinFilesLock = new();
    private static readonly SemaphoreSlim _compilationLock = new(1, 1);

    /// <summary>
    /// 解析 Assets/Forms 目录路径，与 <see cref="MainWindowViewModel"/> 中的逻辑一致。
    /// </summary>
    private static string? ResolveFormsPath()
    {
        var assemblyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location);
        if (assemblyDir != null)
        {
            var outputPath = Path.Combine(assemblyDir, "Assets", "Forms");
            if (Directory.Exists(outputPath))
                return outputPath;
        }

        var currentDir = assemblyDir ?? Directory.GetCurrentDirectory();
        var searchDir = currentDir;
        for (var i = 0; i < 6; i++)
        {
            var candidate = Path.Combine(searchDir, "GorgeStudio", "Assets", "Forms");
            if (Directory.Exists(candidate))
                return candidate;
            searchDir = Path.GetDirectoryName(searchDir);
            if (searchDir == null)
                break;
        }

        return null;
    }

    /// <summary>
    /// 加载 Assets/Forms 目录中 Native/ 和 Dremu/ 的内建源文件，为编译提供核心类型定义。
    /// Native/ 提供 GorgeFramework 等基础类型；Dremu/ 和 DremuBase/ 提供 Dremu 谱面所需类型。
    /// 其他游戏目录（BeeBoo, Obsertor 等）按需由调用方提供。
    /// 结果会被缓存，启动后仅加载一次。所有文件标记为库代码（isChart: false）。
    /// </summary>
    private static List<SourceCodeFile> LoadBuiltinSourceFiles()
    {
        if (_cachedBuiltinFiles != null)
            return _cachedBuiltinFiles;

        lock (_builtinFilesLock)
        {
            if (_cachedBuiltinFiles != null)
                return _cachedBuiltinFiles;

            var formsPath = ResolveFormsPath();
            if (formsPath == null)
            {
                _cachedBuiltinFiles = new List<SourceCodeFile>();
                return _cachedBuiltinFiles;
            }

            var files = new List<SourceCodeFile>();

            // 始终加载 Native/ 和 Dremu/ 目录中的文件
            var builtinDirs = new[] { "Native", "Dremu", "DremuBase" };
            foreach (var dirName in builtinDirs)
            {
                var dirPath = Path.Combine(formsPath, dirName);
                if (!Directory.Exists(dirPath))
                    continue;

                var allGFiles = Directory.EnumerateFiles(dirPath, "*.g", SearchOption.AllDirectories);
                foreach (var filePath in allGFiles)
                {
                    var code = File.ReadAllText(filePath);
                    files.Add(new SourceCodeFile(filePath, code, false));
                }
            }

            _cachedBuiltinFiles = files;
            return _cachedBuiltinFiles;
        }
    }

    /// <summary>
    /// 异步编译一个 Gorge 源码包。自动包含 Assets/Forms 内建库文件以提供完整类型定义。
    /// 文件加载阶段占进度 0-10%，编译阶段占 10-100%。
    /// </summary>
    /// <param name="package">待编译的源码包。</param>
    /// <param name="progress">进度报告器。为 <c>null</c> 时不报告进度。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>包含编译后的项目模型和类声明映射的元组。</returns>
    private static async Task<(CompiledProject Project, Dictionary<string, ClassDeclaration> ClassDecls)> CompilePackageAsync(
        GorgePackage package, IProgress<float>? progress, CancellationToken ct)
    {
        // 文件加载占 0-10%，编译占 10-100%
        progress?.Report(0.1f);

        // 自动包含 Assets/Forms 中的所有内建类型定义，确保所有命名空间可用
        var builtinFiles = LoadBuiltinSourceFiles();
        var allSourceFiles = new List<SourceCodeFile>(package.SourceFiles);
        foreach (var builtinFile in builtinFiles)
        {
            // 仅通过类名去重：如果包中已有同名类（相同文件名），跳过内建文件
            var builtinFileName = Path.GetFileName(builtinFile.Path);
            if (!package.SourceFiles.Any(f => Path.GetFileName(f.Path) == builtinFileName))
            {
                allSourceFiles.Add(builtinFile);
                package.SourcePathIsChart[builtinFile.Path] = false;
            }
        }

        var compileProgress = progress != null
            ? new Progress<float>(p => progress.Report(0.1f + p * 0.9f))
            : null;

        // Gorge 编译器使用静态状态（CompileTempStatic），同一时间只能有一个编译在执行
        await _compilationLock.WaitAsync(ct);
        ClassImplementationContext context;
        try
        {
            context = await Compiler.CompileAsync(allSourceFiles, compileProgress, ct);
        }
        finally
        {
            _compilationLock.Release();
        }

        progress?.Report(1.0f);

        var classDeclarations = new Dictionary<string, ClassDeclaration>();
        var project = ProcessCompilationContext(context, package, classDeclarations);

        return (project, classDeclarations);
    }

    /// <summary>
    /// 将编译上下文（<see cref="ClassImplementationContext"/>）中的编译数据
    /// 映射为 UI 友好的 <see cref="CompiledProject"/> 模型。
    /// 遍历全局作用域的命名空间树，递归提取类、枚举和接口信息。
    /// </summary>
    /// <param name="context">编译器的输出上下文，包含全局作用域。</param>
    /// <param name="package">原始源码包，用于获取源码到谱面代码的映射关系。</param>
    /// <param name="classDeclarations">输出参数，收集所有非 Native 类的 ClassDeclaration 映射。</param>
    /// <returns>填充完成的 <see cref="CompiledProject"/> 实例。</returns>
    private static CompiledProject ProcessCompilationContext(
        ClassImplementationContext context, GorgePackage package,
        Dictionary<string, ClassDeclaration> classDeclarations)
    {
        var classes = new List<CompiledClassInfo>();
        var enums = new List<CompiledEnumInfo>();
        var interfaces = new List<CompiledInterfaceInfo>();

        WalkNamespace(context.GlobalScope, classes, enums, interfaces, package.SourcePathIsChart, classDeclarations);

        return CompiledProject.Create(classes, enums, interfaces);
    }

    /// <summary>
    /// 递归遍历命名空间作用域树，将所有非 Native 的类、枚举、接口提取到各自的列表中。
    /// </summary>
    /// <param name="ns">当前命名空间作用域。</param>
    /// <param name="classes">输出参数，累积所有非 Native 类。</param>
    /// <param name="enums">输出参数，累积所有非 Native 枚举。</param>
    /// <param name="interfaces">输出参数，累积所有非 Native 接口。</param>
    /// <param name="sourcePathIsChart">源文件路径到"是否为谱面代码"的映射字典。</param>
    /// <param name="classDeclarations">输出参数，累积非 Native 类的完全限定名 → ClassDeclaration 映射。</param>
    /// <remarks>
    /// Native 类型由运行时提供，不需要展示在 UI 的编译结果中，因此在此处被过滤。
    /// </remarks>
    private static void WalkNamespace(
        NamespaceScope ns,
        List<CompiledClassInfo> classes,
        List<CompiledEnumInfo> enums,
        List<CompiledInterfaceInfo> interfaces,
        Dictionary<string, bool> sourcePathIsChart,
        Dictionary<string, ClassDeclaration> classDeclarations)
    {
        foreach (var (_, classScope) in ns.Classes)
        {
            if (!classScope.IsNative)
            {
                classes.Add(MapClass(classScope, sourcePathIsChart));
                var decl = classScope.Declaration;
                var fullName = decl.Type.FullName ?? decl.Name;
                classDeclarations[fullName] = decl;
            }
        }

        foreach (var (_, enumScope) in ns.Enums)
        {
            if (!enumScope.EnumSymbol.IsNative && enumScope.Enum != null)
            {
                enums.Add(MapEnum(enumScope, sourcePathIsChart));
            }
        }

        foreach (var (_, interfaceScope) in ns.Interfaces)
        {
            if (!interfaceScope.InterfaceSymbol.IsNative && interfaceScope.Interface != null)
            {
                interfaces.Add(MapInterface(interfaceScope, sourcePathIsChart));
            }
        }

        foreach (var (_, subNs) in ns.SubNamespaces)
        {
            WalkNamespace(subNs, classes, enums, interfaces, sourcePathIsChart, classDeclarations);
        }
    }

    #endregion

    #region Mapping

    /// <summary>
    /// 将编译器的 <see cref="ClassScope"/> 映射为 UI 模型的 <see cref="CompiledClassInfo"/>。
    /// 提取类的所有元数据：名称、继承关系、字段、方法、构造函数、注入字段和注解。
    /// </summary>
    /// <param name="classScope">编译器的类作用域，包含类的完整声明信息。</param>
    /// <param name="sourcePathIsChart">源文件到谱面代码标记的映射。</param>
    /// <returns>填充完整的 <see cref="CompiledClassInfo"/> 实例。</returns>
    private static CompiledClassInfo MapClass(ClassScope classScope, Dictionary<string, bool> sourcePathIsChart)
    {
        var declaration = classScope.Declaration;
        var fullName = declaration.Type.FullName ?? declaration.Name;
        var (ns, className) = SplitFullName(fullName);

        var isChart = sourcePathIsChart.TryGetValue(classScope.SourceFilePath, out var chart) && chart;

        var depth = 0;
        var super = declaration.SuperClass;
        while (super != null)
        {
            depth++;
            super = super.SuperClass;
        }

        return new CompiledClassInfo
        {
            FullName = fullName,
            ClassName = className,
            Namespace = ns,
            IsNative = classScope.IsNative,
            IsChartCode = isChart,
            SuperClassName = declaration.SuperClass?.Type.FullName,
            InterfaceNames = declaration.SuperInterfaces.Select(i => i.Type.FullName ?? i.Name).ToList(),
            AnnotationNames = declaration.Annotations.Select(a => a.Name).ToList(),
            Fields = declaration.Fields.Select(MapField).ToList(),
            Methods = declaration.Methods.Select(MapMethod).ToList(),
            StaticMethods = declaration.StaticMethods.Select(MapMethod).ToList(),
            Constructors = declaration.Constructors.Select(MapConstructor).ToList(),
            InjectorFields = declaration.InjectorFields.Select(MapInjectorField).ToList(),
            Annotations = classScope.Annotations.Select(a => MapAnnotationFromScope(a)).ToList(),
            InheritanceDepth = depth
        };
    }

    /// <summary>
    /// 将编译器的 <see cref="EnumScope"/> 映射为 UI 模型的 <see cref="CompiledEnumInfo"/>。
    /// </summary>
    /// <param name="enumScope">编译器的枚举作用域。</param>
    /// <param name="sourcePathIsChart">源文件到谱面代码标记的映射。</param>
    /// <returns>填充完整的 <see cref="CompiledEnumInfo"/> 实例。</returns>
    private static CompiledEnumInfo MapEnum(EnumScope enumScope, Dictionary<string, bool> sourcePathIsChart)
    {
        var compiledEnum = enumScope.Enum;
        var fullName = compiledEnum.Type.FullName;
        var (ns, _) = SplitFullName(fullName ?? compiledEnum.Name);

        return new CompiledEnumInfo
        {
            FullName = fullName ?? compiledEnum.Name,
            Namespace = ns,
            IsNative = enumScope.EnumSymbol.IsNative,
            Values = compiledEnum.Values.ToList(),
            DisplayNames = compiledEnum.DisplayNames.ToList()
        };
    }

    /// <summary>
    /// 将编译器的 <see cref="InterfaceScope"/> 映射为 UI 模型的 <see cref="CompiledInterfaceInfo"/>。
    /// </summary>
    /// <param name="interfaceScope">编译器的接口作用域。</param>
    /// <param name="sourcePathIsChart">源文件到谱面代码标记的映射。</param>
    /// <returns>填充完整的 <see cref="CompiledInterfaceInfo"/> 实例。</returns>
    private static CompiledInterfaceInfo MapInterface(InterfaceScope interfaceScope,
        Dictionary<string, bool> sourcePathIsChart)
    {
        var compiledInterface = interfaceScope.Interface;
        var fullName = compiledInterface.Type.FullName;
        var (ns, _) = SplitFullName(fullName ?? compiledInterface.Name);

        return new CompiledInterfaceInfo
        {
            FullName = fullName ?? compiledInterface.Name,
            Namespace = ns,
            IsNative = interfaceScope.InterfaceSymbol.IsNative,
            Methods = compiledInterface.Methods.Select(MapMethod).ToList()
        };
    }

    /// <summary>
    /// 将编译器的 <see cref="FieldInformation"/> 映射为 UI 模型的 <see cref="FieldInfo"/>。
    /// </summary>
    /// <param name="field">编译器的字段信息。</param>
    /// <returns>UI 友好的字段信息模型。</returns>
    private static Models.FieldInfo MapField(Gorge.GorgeLanguage.Objective.FieldInformation field)
    {
        return new Models.FieldInfo
        {
            Id = field.Id,
            Name = field.Name,
            Type = TypeToString(field.Type),
            Index = field.Index,
            Annotations = field.Annotations?.Select(MapAnnotation).ToList()
                         ?? new List<AnnotationInfo>()
        };
    }

    /// <summary>
    /// 将编译器的 <see cref="MethodInformation"/> 映射为 UI 模型的 <see cref="MethodInfo"/>。
    /// </summary>
    /// <param name="method">编译器的方法信息。</param>
    /// <returns>UI 友好的方法信息模型。</returns>
    private static Models.MethodInfo MapMethod(Gorge.GorgeLanguage.Objective.MethodInformation method)
    {
        return new Models.MethodInfo
        {
            Id = method.Id,
            Name = method.Name,
            ReturnType = method.ReturnType != null ? TypeToString(method.ReturnType) : null,
            Parameters = method.Parameters?.Select(MapParameter).ToList()
                         ?? new List<ParameterInfo>(),
            Annotations = method.Annotations?.Select(MapAnnotation).ToList()
                          ?? new List<AnnotationInfo>()
        };
    }

    /// <summary>
    /// 将编译器的 <see cref="ConstructorInformation"/> 映射为 UI 模型的 <see cref="ConstructorInfo"/>。
    /// </summary>
    /// <param name="ctor">编译器的构造函数信息。</param>
    /// <returns>UI 友好的构造函数信息模型。</returns>
    private static Models.ConstructorInfo MapConstructor(
        Gorge.GorgeLanguage.Objective.ConstructorInformation ctor)
    {
        return new Models.ConstructorInfo
        {
            Id = ctor.Id,
            Parameters = ctor.Parameters?.Select(MapParameter).ToList()
                         ?? new List<ParameterInfo>(),
            Annotations = ctor.Annotations?.Select(MapAnnotation).ToList()
                          ?? new List<AnnotationInfo>()
        };
    }

    /// <summary>
    /// 将编译器的 <see cref="InjectorFieldInformation"/> 映射为 UI 模型的 <see cref="InjectorFieldInfo"/>。
    /// </summary>
    /// <param name="field">编译器的注入字段信息。</param>
    /// <returns>UI 友好的注入字段信息模型。</returns>
    private static Models.InjectorFieldInfo MapInjectorField(
        Gorge.GorgeLanguage.Objective.InjectorFieldInformation field)
    {
        return new Models.InjectorFieldInfo
        {
            Id = field.Id,
            Name = field.Name,
            Type = TypeToString(field.Type),
            Index = field.Index,
            DefaultValueIndex = field.DefaultValueIndex
        };
    }

    /// <summary>
    /// 将编译器的 <see cref="ParameterInformation"/> 映射为 UI 模型的 <see cref="ParameterInfo"/>。
    /// </summary>
    /// <param name="param">编译器的参数信息。</param>
    /// <returns>UI 友好的参数信息模型。</returns>
    private static ParameterInfo MapParameter(Gorge.GorgeLanguage.Objective.ParameterInformation param)
    {
        return new ParameterInfo
        {
            Id = param.Id,
            Name = param.Name,
            Type = TypeToString(param.Type),
            Index = param.Index
        };
    }

    /// <summary>
    /// 将编译器的 <see cref="AnnotationScope"/> 映射为 UI 模型的 <see cref="AnnotationInfo"/>。
    /// 从注解作用域中读取注解参数符号和元数据条目。
    /// </summary>
    /// <param name="annotationScope">编译器的注解作用域。</param>
    /// <returns>包含注解名、泛型类型和参数的 UI 模型。</returns>
    private static AnnotationInfo MapAnnotationFromScope(AnnotationScope annotationScope)
    {
        var parameters = new Dictionary<string, object?>();

        // 读取注解参数符号（例如 @Anno(key = "value")）
        foreach (var (paramName, symbol) in annotationScope.Symbols)
        {
            if (symbol is AnnotationParameterSymbol paramSymbol)
            {
                parameters[paramName] = paramSymbol.Value;
            }
        }

        // 读取元数据条目
        foreach (var (metaName, metaSymbol) in annotationScope.MetadataScope.Symbols)
        {
            if (metaSymbol is MetadataEntrySymbol entrySymbol)
            {
                parameters[metaName] = entrySymbol.Metadata.Value;
            }
        }

        return new AnnotationInfo
        {
            Name = annotationScope.AnnotationIdentifier,
            GenericType = annotationScope.GenericType?.ToGorgeType()?.FullName,
            Parameters = parameters
        };
    }

    /// <summary>
    /// 将编译器的 <see cref="Annotation"/> 对象映射为 UI 模型的 <see cref="AnnotationInfo"/>。
    /// 从注解对象中提取元数据和私有参数字典。
    /// </summary>
    /// <param name="annotation">编译器的注解对象。</param>
    /// <returns>包含注解名、泛型类型和参数的 UI 模型。</returns>
    /// <remarks>
    /// 注解参数存储为私有字段 <c>_parameters</c>，此方法通过反射访问。
    /// 如果反射失败（如在裁剪环境下），参数集合可能为空。
    /// </remarks>
    private static AnnotationInfo MapAnnotation(Gorge.GorgeLanguage.Objective.Annotation annotation)
    {
        var parameters = new Dictionary<string, object?>();

        // 读取元数据条目
        foreach (var (key, metadata) in annotation.Metadatas)
        {
            parameters[key] = metadata.Value is string s ? s : metadata.Value;
        }

        // 注解参数存储为私有字段，通过反射访问
        var paramField = typeof(Gorge.GorgeLanguage.Objective.Annotation)
            .GetField("_parameters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (paramField?.GetValue(annotation) is Dictionary<string, object> privateParams)
        {
            foreach (var (key, value) in privateParams)
            {
                parameters[key] = value is string s ? s : value;
            }
        }

        return new AnnotationInfo
        {
            Name = annotation.Name,
            GenericType = annotation.GenericType?.FullName,
            Parameters = parameters
        };
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 将 <see cref="GorgeType"/> 转换为可读的类型名称字符串。
    /// 基本类型（int, float, bool, string）返回小写名称；
    /// 引用类型返回完全限定名称。
    /// </summary>
    /// <param name="type">Gorge 类型对象。</param>
    /// <returns>类型的字符串表示。例如 "int"、"float"、"MyGame.Player"。</returns>
    private static string TypeToString(GorgeType type)
    {
        if (type.ClassName == null)
        {
            return type.BasicType switch
            {
                BasicType.Int => "int",
                BasicType.Float => "float",
                BasicType.Bool => "bool",
                BasicType.String => "string",
                _ => type.BasicType.ToString().ToLowerInvariant()
            };
        }

        return type.FullName ?? type.ClassName;
    }

    /// <summary>
    /// 将完全限定名称拆分为命名空间和类名两部分。
    /// 例如 "MyGame.Entities.Player" → ("MyGame.Entities", "Player")。
    /// 无命名空间时返回空字符串作为命名空间。
    /// </summary>
    /// <param name="fullName">完全限定名称，以点号分隔命名空间和类名。</param>
    /// <returns>包含命名空间和类名的元组。</returns>
    private static (string ns, string className) SplitFullName(string fullName)
    {
        var lastDot = fullName.LastIndexOf('.');
        if (lastDot < 0)
            return ("", fullName);
        return (fullName[..lastDot], fullName[(lastDot + 1)..]);
    }

    /// <summary>
    /// 发送状态变更消息。通过 <see cref="StatusChanged"/> 事件通知所有订阅者。
    /// </summary>
    /// <param name="message">状态消息文本，如 "Loading file: ..."。</param>
    private void ReportStatus(string message)
    {
        StatusChanged?.Invoke(message);
    }

    #endregion
}
