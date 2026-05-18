using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GorgeStudio.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private bool _canLaunch = true;

    /// <summary>
    /// 由 View 在 code-behind 中赋值，ViewModel 通过它触发嵌入。
    /// </summary>
    public Func<Task<bool>>? EmbedAction { get; set; }

    [RelayCommand]
    private async Task LaunchAsync()
    {
        if (EmbedAction == null) return;

        CanLaunch = false;
        StatusText = "正在启动...";

        try
        {
            bool ok = await EmbedAction();
            StatusText = ok ? "嵌入完成" : "嵌入失败";
        }
        finally
        {
            CanLaunch = true;
        }
    }
}
