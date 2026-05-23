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
    private readonly IServiceProvider? _serviceProvider;

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
                new Avalonia.Platform.Storage.FilePickerFileType("ZIP 谱面包")
                {
                    Patterns = new[] { "*.zip" }
                }
            }
        });

        if (files.Count == 0) return;

        var path = files[0].Path.LocalPath;
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
    /// 将当前谱面文档保存为 .zip 文件。
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
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
            Title = "保存谱面包",
            DefaultExtension = ".zip",
            FileTypeChoices = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("ZIP 谱面包")
                {
                    Patterns = new[] { "*.zip" }
                }
            }
        });

        if (file == null) return;

        try
        {
            StatusText = "正在保存...";
            var sourceFiles = _codeGenerator.Generate(CurrentScore);
            var zipData = _packageWriter.WriteZip(sourceFiles, CurrentScore.ChartAssetFiles);

            await using var stream = await file.OpenWriteAsync();
            await stream.WriteAsync(zipData);

            StatusText = $"保存成功：{file.Name}";
        }
        catch (Exception ex)
        {
            StatusText = $"保存失败：{ex.Message}";
        }
    }
}
