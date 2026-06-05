using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using GorgeStudio.Models;
using GorgeStudio.Models.Chart;
using GorgeStudio.Services;
using GorgeStudio.Services.ChartService;
using GorgeStudio.Services.CodeGeneration;
using GorgeStudio.Services.FileService;
using GorgeStudio.Services.GodotRemote;
using GorgeStudio.Services.Packaging;
using GorgeStudio.Views;

namespace GorgeStudio.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IEmbedService? _embedService;
    private readonly IFileService? _fileService;
    private readonly IChartService? _chartService;
    private readonly IGorgeCodeGenerator? _codeGenerator;
    private readonly IPackageWriter? _packageWriter;
    private readonly IProjectSettingsService? _projectSettingsService;
    private readonly IGodotRemoteClient? _godotRemoteClient;
    private readonly IServiceProvider? _serviceProvider;

    /// <summary>
    /// 当前已加载或已保存的 .gpkg/.zip 文件路径。
    /// 为 null 表示尚未从文件加载或保存过，此时保存将触发"另存为"对话框。
    /// </summary>
    private string? _currentFilePath;

    /// <summary>
    /// 当前已加载的 Form 列表。用于保存时将 Form 源文件打包进 .gpkg。
    /// </summary>
    private List<FormInfo>? _loadedForms;

    private sealed record SaveChartResult(bool Success, bool Cancelled, string? FilePath, string? ErrorMessage);

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private bool _canLaunch = true;

    [ObservableProperty]
    private ViewModelBase? _rightPanel;

    [ObservableProperty]
    private ViewModelBase? _elementListPanel;

    [ObservableProperty]
    private ViewModelBase? _timelinePanel;

    [ObservableProperty]
    private CompiledProject? _currentProject;

    [ObservableProperty]
    private SimulationScore? _currentScore;

    /// <summary>
    /// 无参构造函数，仅供 Avalonia 设计器使用。
    /// </summary>
    public MainWindowViewModel()
    {
    }

    /// <summary>
    /// 运行时构造函数。通过 DI 接收所有服务和面板 ViewModel。
    /// </summary>
    public MainWindowViewModel(
        IEmbedService embedService,
        IFileService fileService,
        IChartService chartService,
        IGorgeCodeGenerator codeGenerator,
        IPackageWriter packageWriter,
        IProjectSettingsService projectSettingsService,
        IGodotRemoteClient godotRemoteClient,
        IServiceProvider serviceProvider,
        ElementListPanelViewModel elementListPanel,
        PropertiesPanelViewModel propertiesPanel,
        TimelinePanelViewModel timelinePanel)
    {
        _embedService = embedService;
        _fileService = fileService;
        _chartService = chartService;
        _codeGenerator = codeGenerator;
        _packageWriter = packageWriter;
        _projectSettingsService = projectSettingsService;
        _godotRemoteClient = godotRemoteClient;
        _serviceProvider = serviceProvider;

        RightPanel = propertiesPanel;
        ElementListPanel = elementListPanel;
        TimelinePanel = timelinePanel;

        // 合并服务的状态消息
        _embedService.StatusChanged += msg => StatusText = msg;
        _fileService.StatusChanged += msg => StatusText = msg;

        // 元素列表选中项变更时，同步到属性面板
        elementListPanel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ElementListPanelViewModel.SelectedItem))
                propertiesPanel.SelectedObject = elementListPanel.SelectedItem?.Tag;
        };

        // 时间线乐段变更时，刷新元素列表和属性面板
        timelinePanel.ScoreChanged += () =>
        {
            if (CurrentScore != null)
            {
                elementListPanel.ReloadSimulationScore(CurrentScore);
                propertiesPanel.SetChartDocument(CurrentScore);
            }
        };

        // 时间线选中变更时，同步到属性面板
        timelinePanel.SelectionChanged += selected =>
        {
            var refreshOnly = ReferenceEquals(propertiesPanel.SelectedObject, selected);
            propertiesPanel.SelectedObject = selected;
            if (refreshOnly)
                propertiesPanel.RefreshSelectedObject();
        };
    }

    /// <summary>
    /// 启动时扫描 Assets/Forms 目录，发现可用 Form 并弹出选择窗口。
    /// 内置库（Native、DremuBase 等）会被自动排除。
    /// 用户可选择多个 Form，确定后一次性编译加载。
    /// </summary>
    public async Task AutoLoadAsync()
    {
        var formsPath = ResolveAssetFormsPath();
        if (formsPath == null)
        {
            StatusText = "Assets/Forms 目录未找到";
            return;
        }

        if (_fileService == null) return;

        var forms = await _fileService.DiscoverFormsAsync(formsPath);

        if (forms.Count == 0)
        {
            StatusText = "未发现可用模态";
            return;
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
            || desktop.MainWindow is not { } window)
            return;

        var vm = new FormSelectionWindowViewModel(forms);
        var ok = await FormSelectionWindow.ShowAsync(window, vm);
        if (!ok)
        {
            StatusText = "已取消加载";
            return;
        }

        var selected = vm.GetSelectedForms();
        if (selected.Count == 0)
        {
            StatusText = "未选择任何模态";
            return;
        }

        var paths = selected.Select(f => f.Path).ToList();
        await LoadAndHandleResult(() => _fileService.LoadAndCompileMultipleDirectoriesAsync(paths), selected);
    }

    /// <summary>
    /// 打开模态管理窗口，允许用户重新选择要加载的 Form。
    /// </summary>
    [RelayCommand]
    private async Task ManageFormsAsync()
    {
        var formsPath = ResolveAssetFormsPath();
        if (formsPath == null)
        {
            StatusText = "Assets/Forms 目录未找到";
            return;
        }

        if (_fileService == null) return;

        var forms = await _fileService.DiscoverFormsAsync(formsPath);

        if (forms.Count == 0)
        {
            StatusText = "未发现可用模态";
            return;
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
            || desktop.MainWindow is not { } window)
            return;

        var vm = new FormSelectionWindowViewModel(forms);
        var ok = await FormSelectionWindow.ShowAsync(window, vm);
        if (!ok)
            return;

        var selected = vm.GetSelectedForms();
        if (selected.Count == 0)
        {
            StatusText = "未选择任何模态";
            return;
        }

        var paths = selected.Select(f => f.Path).ToList();
        await LoadAndHandleResult(() => _fileService.LoadAndCompileMultipleDirectoriesAsync(paths), selected);
    }

    /// <summary>
    /// 从 setting.json 的 Forms 列表恢复 FormInfo 列表。
    /// 在 Assets/Forms/ 目录下查找对应的 Form 目录。
    /// </summary>
    private List<FormInfo>? RestoreFormsFromSettings(List<string> formDirNames)
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

    /// <summary>
    /// 解析 Assets/Forms 目录的运行时路径。
    /// 先查找输出目录（bin/Debug/net9.0/Assets/Forms），
    /// 找不到则回退到源码树（GorgeStudio/Assets/Forms）。
    /// </summary>
    private static string? ResolveAssetFormsPath()
    {
        var assemblyDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
        if (assemblyDir != null)
        {
            var outputPath = Path.Combine(assemblyDir, "Assets", "Forms");
            if (Directory.Exists(outputPath))
                return outputPath;
        }

        // 回退到源码树：从输出目录向上查找 GorgeStudio 项目目录
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

    [RelayCommand]
    private async Task LaunchAsync()
    {
        if (OperatingSystem.IsMacOS())
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime
                && lifetime.MainWindow is not null)
            {
                var dialog = new Window
                {
                    Title = "提示",
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                };
                var panel = new StackPanel { Margin = new Thickness(24, 16), Spacing = 12 };
                panel.Children.Add(new TextBlock { Text = "Mac仅支持远程仿真" });
                var button = new Button { Content = "确定", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                button.Click += (_, _) => dialog.Close();
                panel.Children.Add(button);
                dialog.Content = panel;
                await dialog.ShowDialog(lifetime.MainWindow);
            }
            StatusText = "Mac仅支持远程仿真";
            return;
        }

        if (CurrentScore == null)
        {
            StatusText = "没有可加载的谱面数据";
            return;
        }

        CanLaunch = false;

        try
        {
            // 1. 保存当前谱面
            StatusText = "正在保存谱面...";
            var saveResult = await SaveCurrentChartForGodotAsync();
            if (saveResult.Cancelled)
            {
                StatusText = "已取消加载";
                return;
            }
            if (!saveResult.Success)
            {
                StatusText = $"保存失败：{saveResult.ErrorMessage ?? "未知错误"}";
                return;
            }

            // 2. 启动 Godot 进程
            StatusText = "正在启动 Godot...";
            var launchResult = await _embedService!.LaunchAsync();
            if (!launchResult.Success)
            {
                StatusText = $"启动失败：{launchResult.ErrorMessage ?? "未知错误"}";
                return;
            }

            // 3. 等待 Godot UDP 就绪
            StatusText = "等待 Godot 就绪...";
            var readyResult = await _godotRemoteClient!.WaitUntilReadyAsync();
            if (!readyResult.Success)
            {
                StatusText = $"Godot 未就绪：{readyResult.Error ?? "超时"}";
                return;
            }

            // 4. 发送 set_packages 命令
            StatusText = "正在加载谱面到 Godot...";
            var setResult = await _godotRemoteClient.SetGpkgAsync(saveResult.FilePath!);
            if (!setResult.Success)
            {
                StatusText = $"加载失败：{setResult.Error ?? "未知错误"}";
                return;
            }

            StatusText = $"Godot 已加载当前谱面，时长 {setResult.DurationSeconds:F1}s";
        }
        finally
        {
            CanLaunch = true;
        }
    }

    [RelayCommand]
    private async Task LoadZipAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
            || desktop.MainWindow is not { } window)
            return;

        var files = await window.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "打开谱面包",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("Gorge 谱面包")
                {
                    Patterns = new[] { "*.gpkg", "*.zip" }
                }
            }
        });

        if (files.Count == 0) return;

        var path = files[0].Path.LocalPath;
        _currentFilePath = path;
        await LoadAndHandleResult(() => _fileService!.LoadAndCompileZipAsync(path));
    }

    private async Task LoadAndHandleResult(Func<Task<CompileResult>> loadAction, List<FormInfo>? loadedForms = null)
    {
        if (_fileService == null) return;

        CanLaunch = false;
        StatusText = "正在加载...";

        try
        {
            var result = await loadAction();

            if (result.Success && result.Project != null)
            {
                CurrentProject = result.Project;

                if (result.Settings != null && _projectSettingsService != null)
                {
                    _projectSettingsService.SaveSettings(result.Settings);

                    // 从 setting.json 恢复已加载的 Form 信息
                    if (loadedForms == null && result.Settings.Forms.Count > 0)
                    {
                        loadedForms = RestoreFormsFromSettings(result.Settings.Forms);
                    }
                }

                _loadedForms = loadedForms;

                if (loadedForms != null && _projectSettingsService != null)
                    _projectSettingsService.CurrentSettings.Forms = loadedForms.Select(f => f.Name).ToList();

                var elementListPanel = ElementListPanel as ElementListPanelViewModel;
                elementListPanel?.LoadProject(result.Project);

                // 构建谱面运行时模型
                if (_chartService != null && result.ClassDeclarations != null)
                {
                    var score = await _chartService.BuildChartDocumentAsync(result);
                    CurrentScore = score;

                    if (RightPanel is PropertiesPanelViewModel propertiesPanel)
                        propertiesPanel.SetChartDocument(score);

                    if (TimelinePanel is TimelinePanelViewModel timelinePanel)
                        timelinePanel.SetChartDocument(score);

                    elementListPanel?.LoadSimulationScore(score);
                }

                StatusText = $"编译成功，{result.Project.Classes.Count} 个类，耗时 {result.CompileTime.TotalMilliseconds:F0}ms";
            }
            else
            {
                StatusText = $"编译失败：{result.ErrorMessage ?? "未知错误"}";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"加载失败：{ex.Message}";
        }
        finally
        {
            CanLaunch = true;
        }
    }

    [RelayCommand]
    private async Task OpenProjectSettingsAsync()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is { } window
            && _serviceProvider != null)
        {
            var vm = _serviceProvider.GetRequiredService<ProjectSettingsWindowViewModel>();
            await ProjectSettingsWindow.ShowAsync(window, vm);
        }
    }

    /// <summary>
    /// 保存当前谱面文档。
    /// 如果已从文件加载或曾保存过，直接覆盖保存；否则弹出"另存为"对话框。
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (CurrentScore == null || _codeGenerator == null || _packageWriter == null)
        {
            StatusText = "没有可保存的谱面数据";
            return;
        }

        if (_currentFilePath != null)
        {
            await WriteSaveDataAsync(_currentFilePath);
        }
        else
        {
            await SaveAsAsync();
        }
    }

    /// <summary>
    /// 另存为：始终弹出文件选择对话框，将当前谱面保存到新路径。
    /// </summary>
    [RelayCommand]
    private async Task SaveAsAsync()
    {
        if (CurrentScore == null || _codeGenerator == null || _packageWriter == null)
        {
            StatusText = "没有可保存的谱面数据";
            return;
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
            || desktop.MainWindow is not { } window)
            return;

        var file = await window.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "另存为",
            DefaultExtension = ".gpkg",
            FileTypeChoices = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("Gorge 谱面包")
                {
                    Patterns = new[] { "*.gpkg" }
                }
            }
        });

        if (file == null) return;
        await WriteSaveDataAsync(file.Path.LocalPath);
    }

    /// <summary>
    /// 为 Godot 加载流程保存当前谱面。
    /// 如果已有文件路径则直接覆盖保存，否则弹出"另存为"对话框。
    /// </summary>
    private async Task<SaveChartResult> SaveCurrentChartForGodotAsync()
    {
        if (_currentFilePath != null)
        {
            return await WriteSaveDataForResultAsync(_currentFilePath);
        }

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
            || desktop.MainWindow is not { } window)
            return new SaveChartResult(false, false, null, "无法获取主窗口");

        var file = await window.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            Title = "保存谱面后启动 Godot",
            DefaultExtension = ".gpkg",
            FileTypeChoices = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("Gorge 谱面包")
                {
                    Patterns = new[] { "*.gpkg" }
                }
            }
        });

        if (file == null)
            return new SaveChartResult(false, true, null, null);

        return await WriteSaveDataForResultAsync(file.Path.LocalPath);
    }

    /// <summary>
    /// 将当前谱面数据写入指定路径，以 <see cref="SaveChartResult"/> 形式返回结果。
    /// </summary>
    private async Task<SaveChartResult> WriteSaveDataForResultAsync(string savePath)
    {
        try
        {
            var sourceFiles = _codeGenerator!.Generate(CurrentScore!);

            List<Gorge.GorgeCompiler.SourceCodeFile>? formSourceFiles = null;
            List<AssetFile>? formResourceFiles = null;
            if (_loadedForms is { Count: > 0 })
            {
                formSourceFiles = new List<Gorge.GorgeCompiler.SourceCodeFile>();
                formResourceFiles = new List<AssetFile>();
                foreach (var form in _loadedForms)
                {
                    var allFormFiles = Directory.EnumerateFiles(form.Path, "*.*", SearchOption.AllDirectories);
                    foreach (var filePath in allFormFiles)
                    {
                        var relativePath = "Forms/" + form.DirectoryName + "/"
                            + Path.GetRelativePath(form.Path, filePath).Replace('\\', '/');
                        if (filePath.EndsWith(".g", StringComparison.OrdinalIgnoreCase))
                        {
                            var code = await File.ReadAllTextAsync(filePath);
                            formSourceFiles.Add(new Gorge.GorgeCompiler.SourceCodeFile(relativePath, code, true));
                        }
                        else
                        {
                            var data = await File.ReadAllBytesAsync(filePath);
                            formResourceFiles.Add(new AssetFile(relativePath, data, true));
                        }
                    }
                }
            }

            var settings = _projectSettingsService?.CurrentSettings;
            if (settings != null && _loadedForms != null)
            {
                settings.Forms = _loadedForms.Select(f => f.DirectoryName).ToList();
            }

            var zipData = _packageWriter!.WriteZip(sourceFiles, CurrentScore!.ChartAssetFiles, settings, formSourceFiles, formResourceFiles);

            await using var stream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.WriteAsync(zipData);

            _currentFilePath = savePath;
            return new SaveChartResult(true, false, savePath, null);
        }
        catch (Exception ex)
        {
            return new SaveChartResult(false, false, null, ex.Message);
        }
    }

    /// <summary>
    /// 将当前谱面数据写入指定路径。
    /// 同时将已加载 Form 的源文件打包进 .gpkg，确保下次打开时自包含。
    /// </summary>
    private async Task WriteSaveDataAsync(string savePath)
    {
        StatusText = "正在保存...";
        var result = await WriteSaveDataForResultAsync(savePath);
        if (result.Success)
            StatusText = $"保存成功：{Path.GetFileName(savePath)}";
        else
            StatusText = $"保存失败：{result.ErrorMessage ?? "未知错误"}";
    }
}
