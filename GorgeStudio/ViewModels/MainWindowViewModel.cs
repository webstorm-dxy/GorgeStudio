using System;
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
    private readonly IServiceProvider? _serviceProvider;

    /// <summary>
    /// 当前已加载或已保存的 .gpkg/.zip 文件路径。
    /// 为 null 表示尚未从文件加载或保存过，此时保存将触发"另存为"对话框。
    /// </summary>
    private string? _currentFilePath;

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
    }

    /// <summary>
    /// 启动时自动加载 Assets/Forms 目录下的所有 .g 源文件。
    /// 先查找输出目录，找不到则回退到源码树（dev 模式）。
    /// </summary>
    public async Task AutoLoadAsync()
    {
        var formsPath = ResolveAssetFormsPath();
        if (formsPath == null)
        {
            StatusText = "Assets/Forms 目录未找到";
            return;
        }

        await LoadAndHandleResult(() => _fileService!.LoadAndCompileDirectoryAsync(formsPath, recursive: true));
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

        CanLaunch = false;
        StatusText = "正在启动...";

        try
        {
            bool ok = _embedService != null && await _embedService.LaunchAsync();
            StatusText = ok ? "嵌入完成" : "嵌入失败";
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

    private async Task LoadAndHandleResult(Func<Task<CompileResult>> loadAction)
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
                }

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
    /// 将当前谱面数据写入指定路径。
    /// </summary>
    private async Task WriteSaveDataAsync(string savePath)
    {
        try
        {
            StatusText = "正在保存...";
            var sourceFiles = _codeGenerator!.Generate(CurrentScore!);
            var zipData = _packageWriter!.WriteZip(sourceFiles, CurrentScore!.ChartAssetFiles, _projectSettingsService?.CurrentSettings);

            await using var stream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.WriteAsync(zipData);

            _currentFilePath = savePath;
            StatusText = $"保存成功：{Path.GetFileName(savePath)}";
        }
        catch (Exception ex)
        {
            StatusText = $"保存失败：{ex.Message}";
        }
    }
}
