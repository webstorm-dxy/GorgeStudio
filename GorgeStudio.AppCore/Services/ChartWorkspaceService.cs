using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Gorge.GorgeCompiler;
using GorgeStudio.AppCore.Models.Results;
using GorgeStudio.AppCore.Sessions;
using GorgeStudio.Models;
using GorgeStudio.Models.Chart;
using GorgeStudio.Services;
using GorgeStudio.Services.ChartService;
using GorgeStudio.Services.CodeGeneration;
using GorgeStudio.Services.FileService;
using GorgeStudio.Services.Packaging;

namespace GorgeStudio.AppCore.Services;

public interface IChartWorkspaceService
{
    event Action<string>? StatusChanged;

    Task<LoadChartResult> LoadFromZipAsync(string path, CancellationToken ct = default);

    Task<LoadChartResult> LoadFromFormsAsync(
        IReadOnlyList<string> formPaths,
        List<FormInfo>? loadedForms = null,
        CancellationToken ct = default);

    Task<SaveChartResult> SaveAsync(
        ChartSession session,
        string? savePath = null,
        CancellationToken ct = default);
}

public sealed class ChartWorkspaceService : IChartWorkspaceService
{
    private readonly IFileService _fileService;
    private readonly IChartService _chartService;
    private readonly IGorgeCodeGenerator _codeGenerator;
    private readonly IPackageWriter _packageWriter;
    private readonly IProjectSettingsService? _projectSettingsService;

    public event Action<string>? StatusChanged;

    public ChartWorkspaceService(
        IFileService fileService,
        IChartService chartService,
        IGorgeCodeGenerator codeGenerator,
        IPackageWriter packageWriter,
        IProjectSettingsService? projectSettingsService = null)
    {
        _fileService = fileService;
        _chartService = chartService;
        _codeGenerator = codeGenerator;
        _packageWriter = packageWriter;
        _projectSettingsService = projectSettingsService;
    }

    public async Task<LoadChartResult> LoadFromZipAsync(string path, CancellationToken ct = default)
    {
        StatusChanged?.Invoke("正在加载...");
        try
        {
            var result = await _fileService.LoadAndCompileZipAsync(path, ct: ct);
            return await BuildLoadResultAsync(result, path, null);
        }
        catch (Exception ex)
        {
            return new LoadChartResult(false, null, null, path, ex.Message, null, TimeSpan.Zero);
        }
    }

    public async Task<LoadChartResult> LoadFromFormsAsync(
        IReadOnlyList<string> formPaths,
        List<FormInfo>? loadedForms = null,
        CancellationToken ct = default)
    {
        StatusChanged?.Invoke("正在加载...");
        try
        {
            var result = await _fileService.LoadAndCompileMultipleDirectoriesAsync(formPaths, ct: ct);
            return await BuildLoadResultAsync(result, null, loadedForms);
        }
        catch (Exception ex)
        {
            return new LoadChartResult(false, null, null, null, ex.Message, loadedForms, TimeSpan.Zero);
        }
    }

    private async Task<LoadChartResult> BuildLoadResultAsync(
        CompileResult result, string? filePath, List<FormInfo>? loadedForms)
    {
        if (!result.Success || result.Project == null)
        {
            return new LoadChartResult(false, null, null, filePath,
                result.ErrorMessage ?? "编译失败", loadedForms, result.CompileTime);
        }

        if (result.Settings != null && _projectSettingsService != null)
        {
            _projectSettingsService.SaveSettings(result.Settings);
            if (loadedForms == null && result.Settings.Forms.Count > 0)
            {
                loadedForms = RestoreFormsFromSettings(result.Settings.Forms);
            }
        }

        if (loadedForms != null && _projectSettingsService != null)
            _projectSettingsService.CurrentSettings.Forms = loadedForms.Select(f => f.Name).ToList();

        SimulationScore? score = null;
        if (_chartService != null && result.ClassDeclarations != null)
        {
            score = await _chartService.BuildChartDocumentAsync(result, ct: CancellationToken.None);
        }

        var count = result.Project.Classes.Count;
        var ms = result.CompileTime.TotalMilliseconds;
        StatusChanged?.Invoke($"编译成功，{count} 个类，耗时 {ms:F0}ms");

        return new LoadChartResult(true, result.Project, score, filePath, null, loadedForms, result.CompileTime);
    }

    public async Task<SaveChartResult> SaveAsync(
        ChartSession session,
        string? savePath = null,
        CancellationToken ct = default)
    {
        if (session.CurrentScore == null)
            return new SaveChartResult(false, false, null, "没有可保存的谱面数据");

        var targetPath = savePath ?? session.CurrentFilePath;
        if (targetPath == null)
            return new SaveChartResult(false, false, null, "未指定保存路径");

        try
        {
            var sourceFiles = _codeGenerator.Generate(session.CurrentScore);

            List<SourceCodeFile>? formSourceFiles = null;
            List<AssetFile>? formResourceFiles = null;
            if (session.LoadedForms is { Count: > 0 })
            {
                formSourceFiles = new List<SourceCodeFile>();
                formResourceFiles = new List<AssetFile>();
                foreach (var form in session.LoadedForms)
                {
                    var allFormFiles = Directory.EnumerateFiles(form.Path, "*.*", SearchOption.AllDirectories);
                    foreach (var filePath in allFormFiles)
                    {
                        var relativePath = "Forms/" + form.DirectoryName + "/"
                            + Path.GetRelativePath(form.Path, filePath).Replace('\\', '/');
                        if (filePath.EndsWith(".g", StringComparison.OrdinalIgnoreCase))
                        {
                            var code = await File.ReadAllTextAsync(filePath, ct);
                            formSourceFiles.Add(new SourceCodeFile(relativePath, code, false));
                        }
                        else
                        {
                            var data = await File.ReadAllBytesAsync(filePath, ct);
                            formResourceFiles.Add(new AssetFile(relativePath, data, false));
                        }
                    }
                }
            }

            var settings = _projectSettingsService?.CurrentSettings;
            if (settings != null && session.LoadedForms.Count > 0)
            {
                settings.Forms = session.LoadedForms.Select(f => f.DirectoryName).ToList();
            }

            var zipData = _packageWriter.WriteZip(sourceFiles, session.CurrentScore.ChartAssetFiles,
                settings, formSourceFiles, formResourceFiles);

            await using var stream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.WriteAsync(zipData, ct);

            session.CurrentFilePath = targetPath;
            StatusChanged?.Invoke($"保存成功：{Path.GetFileName(targetPath)}");
            return new SaveChartResult(true, false, targetPath, null);
        }
        catch (Exception ex)
        {
            return new SaveChartResult(false, false, null, ex.Message);
        }
    }

    private static List<FormInfo>? RestoreFormsFromSettings(List<string> formDirNames)
    {
        var formsPath = ResolveAssetFormsPath();
        if (formsPath == null)
            return null;

        var forms = new List<FormInfo>();
        foreach (var dirName in formDirNames)
        {
            var dirPath = Path.Combine(formsPath, dirName);
            if (Directory.Exists(dirPath))
                forms.Add(new FormInfo { DirectoryName = dirName, Path = dirPath });
        }

        return forms.Count > 0 ? forms : null;
    }

    internal static string? ResolveAssetFormsPath()
    {
        var assemblyDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
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
}
