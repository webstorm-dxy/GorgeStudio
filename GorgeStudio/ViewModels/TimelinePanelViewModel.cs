using CommunityToolkit.Mvvm.ComponentModel;

namespace GorgeStudio.ViewModels;

public partial class TimelinePanelViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "Timeline";
}
