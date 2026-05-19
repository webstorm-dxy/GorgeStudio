using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GorgeStudio.Services;

namespace GorgeStudio.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IEmbedService? _embedService;

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

    /// <summary>
    /// Avalonia designer requires a public parameterless constructor for Design.DataContext.
    /// Runtime construction is still handled by DI through the IEmbedService constructor.
    /// </summary>
    public MainWindowViewModel()
    {
    }

    /// <summary>
    /// 通过构造函数注入嵌入服务，ViewModel 不依赖任何 View 类型。
    /// </summary>
    public MainWindowViewModel(IEmbedService embedService)
    {
        _embedService = embedService;
        _embedService.StatusChanged += msg => StatusText = msg;
        RightPanel = new PropertiesPanelViewModel();
        ElementListPanel = new ElementListPanelViewModel();
        TimelinePanel = new TimelinePanelViewModel();
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
}
