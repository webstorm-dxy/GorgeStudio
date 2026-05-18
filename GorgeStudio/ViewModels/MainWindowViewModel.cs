using System.Threading.Tasks;
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
        TimelinePanel = new TimelinePanelViewModel();
    }

    [RelayCommand]
    private async Task LaunchAsync()
    {
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
