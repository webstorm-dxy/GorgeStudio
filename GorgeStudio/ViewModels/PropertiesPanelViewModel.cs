using CommunityToolkit.Mvvm.ComponentModel;

namespace GorgeStudio.ViewModels;

public partial class PropertiesPanelViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "Properties";

    [ObservableProperty]
    private string _description = "Select an object to view its properties.";
}
