using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using GorgeStudio.AppCore.Services;
using GorgeStudio.AppCore.Sessions;
using GorgeStudio.AppCore.Models.Results;
using GorgeStudio.Models;
using GorgeStudio.Models.Chart;
using GorgeStudio.Services;
using GorgeStudio.Views;

namespace GorgeStudio.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IEmbedService? _embedService;
    private readonly IServiceProvider? _serviceProvider;
    private readonly IChartWorkspaceService? _workspaceService;
    private readonly IFormsCatalogService? _formsCatalogService;
    private readonly IGodotLaunchWorkflow? _godotLaunchWorkflow;
    private readonly IProjectSettingsService? _projectSettingsService;

    private readonly ChartSession _session = new();

    [ObservableProperty]
    private string _statusText = "就绪";

    partial void OnStatusTextChanged(string value)
    {
        Console.WriteLine($"[GorgeStudio] {value}");
    }

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
        IChartWorkspaceService workspaceService,
        IFormsCatalogService formsCatalogService,
        IGodotLaunchWorkflow godotLaunchWorkflow,
        IProjectSettingsService projectSettingsService,
        IServiceProvider serviceProvider,
        ElementListPanelViewModel elementListPanel,
        PropertiesPanelViewModel propertiesPanel,
        TimelinePanelViewModel timelinePanel)
    {
        _embedService = embedService;
        _workspaceService = workspaceService;
        _formsCatalogService = formsCatalogService;
        _godotLaunchWorkflow = godotLaunchWorkflow;
        _projectSettingsService = projectSettingsService;
        _serviceProvider = serviceProvider;

        RightPanel = propertiesPanel;
        ElementListPanel = elementListPanel;
        TimelinePanel = timelinePanel;

        // 合并服务的状态消息
        _embedService.StatusChanged += msg => StatusText = msg;
        _workspaceService.StatusChanged += msg => StatusText = msg;

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

    public async Task AutoLoadAsync()
    {
        if (_formsCatalogService == null) return;

        var forms = await _formsCatalogService.DiscoverFormsAsync();

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
        await ApplyLoadResult(() => _workspaceService!.LoadFromFormsAsync(paths, selected));
    }

    [RelayCommand]
    private async Task ManageFormsAsync()
    {
        if (_formsCatalogService == null) return;

        var forms = await _formsCatalogService.DiscoverFormsAsync();

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
        await ApplyLoadResult(() => _workspaceService!.LoadFromFormsAsync(paths, selected));
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
            // Start Godot embedding (platform-specific)
            StatusText = "正在启动 Godot...";
            var launchResult = await _embedService!.LaunchAsync();
            if (!launchResult.Success)
            {
                StatusText = $"启动失败：{launchResult.ErrorMessage ?? "未知错误"}";
                return;
            }

            // Save, wait for UDP, and load chart via AppCore workflow
            var result = await _godotLaunchWorkflow!.SaveLaunchAndLoadAsync(_session);
            StatusText = result.Success
                ? $"Godot 已加载当前谱面，时长 {result.DurationSeconds:F1}s"
                : result.ErrorMessage ?? "启动失败";
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
        _session.CurrentFilePath = path;
        await ApplyLoadResult(() => _workspaceService!.LoadFromZipAsync(path));
    }

    private async Task ApplyLoadResult(Func<Task<LoadChartResult>> loadAction)
    {
        CanLaunch = false;

        try
        {
            var result = await loadAction();

            if (result.Success && result.Project != null)
            {
                CurrentProject = result.Project;
                _session.CurrentProject = result.Project;
                _session.Settings = _projectSettingsService?.CurrentSettings ?? _session.Settings;

                if (result.LoadedForms != null)
                {
                    _session.LoadedForms.Clear();
                    _session.LoadedForms.AddRange(result.LoadedForms);
                }

                if (result.Score != null)
                {
                    CurrentScore = result.Score;
                    _session.CurrentScore = result.Score;

                    var elementListPanel = ElementListPanel as ElementListPanelViewModel;
                    elementListPanel?.LoadProject(result.Project);
                    elementListPanel?.LoadSimulationScore(result.Score);

                    if (RightPanel is PropertiesPanelViewModel propertiesPanel)
                        propertiesPanel.SetChartDocument(result.Score);

                    if (TimelinePanel is TimelinePanelViewModel timelinePanel)
                        timelinePanel.SetChartDocument(result.Score);
                }

                var count = result.Project.Classes.Count;
                var ms = result.CompileTime.TotalMilliseconds;
                StatusText = $"编译成功，{count} 个类，耗时 {ms:F0}ms";
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

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (CurrentScore == null)
        {
            StatusText = "没有可保存的谱面数据";
            return;
        }

        if (_session.CurrentFilePath != null)
        {
            await _workspaceService!.SaveAsync(_session);
        }
        else
        {
            await SaveAsAsync();
        }
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        if (CurrentScore == null)
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
        await _workspaceService!.SaveAsync(_session, file.Path.LocalPath);
    }
}
