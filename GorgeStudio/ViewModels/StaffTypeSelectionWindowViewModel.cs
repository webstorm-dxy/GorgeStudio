using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GorgeStudio.ViewModels;

public class StaffTypeOption
{
    public string Annotation { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public bool RequiresForm { get; init; }
}

public partial class StaffTypeSelectionWindowViewModel : ViewModelBase
{
    public ObservableCollection<StaffTypeOption> StaffTypes { get; } = new();
    public ObservableCollection<string> Forms { get; } = new();

    [ObservableProperty]
    private StaffTypeOption? _selectedStaffType;

    [ObservableProperty]
    private string? _selectedForm;

    public bool DialogResult { get; private set; }
    public Action? CloseAction { get; set; }

    public bool IsSelectionValid =>
        SelectedStaffType != null
        && (!SelectedStaffType.RequiresForm || SelectedForm != null);

    partial void OnSelectedStaffTypeChanged(StaffTypeOption? value)
    {
        OnPropertyChanged(nameof(IsSelectionValid));
    }

    partial void OnSelectedFormChanged(string? value)
    {
        OnPropertyChanged(nameof(IsSelectionValid));
    }

    public StaffTypeSelectionWindowViewModel()
    {
    }

    [RelayCommand]
    private void Ok()
    {
        if (!IsSelectionValid) return;
        DialogResult = true;
        CloseAction?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        CloseAction?.Invoke();
    }
}
