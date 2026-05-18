using CommunityToolkit.Mvvm.ComponentModel;

namespace GorgeStudio.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _statusText = "就绪";

    /// <summary>
    /// Fraction (0.0–1.0) of window height allocated to the embedded area.
    /// </summary>
    [ObservableProperty]
    private double _embedRatio = 0.70;
}
