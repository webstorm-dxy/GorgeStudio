using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GorgeStudio.AppCore.Services;
using GorgeStudio.AppCore.Models.Results;
using GorgeStudio.Models.Chart;

namespace GorgeStudio.ViewModels;

public partial class PropertiesPanelViewModel : ViewModelBase
{
    private readonly IPropertyInspectionService? _inspectionService;

    [ObservableProperty]
    private string _title = "Properties";

    [ObservableProperty]
    private string _description = "Select an object to view its properties.";

    [ObservableProperty]
    private object? _selectedObject;

    public ObservableCollection<PropertyEntry> Properties { get; } = new();

    public PropertiesPanelViewModel()
    {
    }

    public PropertiesPanelViewModel(IPropertyInspectionService inspectionService)
    {
        _inspectionService = inspectionService;
    }

    public void SetChartDocument(SimulationScore? score)
    {
        if (SelectedObject != null)
            InspectObject();
    }

    public void RefreshSelectedObject()
    {
        InspectObject();
    }

    partial void OnSelectedObjectChanged(object? value)
    {
        InspectObject();
    }

    private void InspectObject()
    {
        Properties.Clear();

        if (_inspectionService == null)
        {
            Description = "Select an object to view its properties.";
            return;
        }

        var result = _inspectionService.Inspect(SelectedObject);
        Description = result.Description;
        foreach (var prop in result.Properties)
            Properties.Add(prop);
    }
}
