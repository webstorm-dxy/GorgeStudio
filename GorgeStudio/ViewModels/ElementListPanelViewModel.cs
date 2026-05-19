using CommunityToolkit.Mvvm.ComponentModel;

namespace GorgeStudio.ViewModels;

public partial class ElementListPanelViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "Element List";

    [ObservableProperty]
    private string _description = "Elements in the current scene.";
}