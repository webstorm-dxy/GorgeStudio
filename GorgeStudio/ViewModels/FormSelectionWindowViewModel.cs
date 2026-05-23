using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GorgeStudio.Models;

namespace GorgeStudio.ViewModels;

public partial class FormSelectionItem : ObservableObject
{
    public FormInfo Form { get; init; } = null!;

    [ObservableProperty]
    private bool _isSelected;
}

public partial class FormSelectionWindowViewModel : ViewModelBase
{
    public ObservableCollection<FormSelectionItem> Items { get; } = new();

    public bool DialogResult { get; private set; }

    public Action? CloseAction { get; set; }

    public FormSelectionWindowViewModel()
    {
    }

    public FormSelectionWindowViewModel(List<FormInfo> forms)
    {
        foreach (var form in forms)
            Items.Add(new FormSelectionItem { Form = form });
    }

    public List<FormInfo> GetSelectedForms() =>
        Items.Where(i => i.IsSelected).Select(i => i.Form).ToList();

    [RelayCommand]
    private void Ok()
    {
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
