using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GorgeStudio.ViewModels;

public partial class RenameStaffDisplayNameWindowViewModel : ViewModelBase
{
    public string ClassName { get; }

    [ObservableProperty]
    private string _displayName = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _canConfirm;

    public bool DialogResult { get; private set; }
    public Action? CloseAction { get; set; }

    private const int MaxDisplayNameLength = 64;

    public RenameStaffDisplayNameWindowViewModel()
    {
    }

    public RenameStaffDisplayNameWindowViewModel(string className, string currentDisplayName)
    {
        ClassName = className;
        _displayName = currentDisplayName ?? "";
        Validate();
    }

    partial void OnDisplayNameChanged(string value)
    {
        Validate();
    }

    private void Validate()
    {
        var trimmed = DisplayName.Trim();

        if (trimmed.Length == 0)
        {
            ErrorMessage = "显示名不能为空";
            CanConfirm = false;
        }
        else if (trimmed.Length > MaxDisplayNameLength)
        {
            ErrorMessage = $"显示名长度不能超过 {MaxDisplayNameLength} 个字符（当前 {trimmed.Length}）";
            CanConfirm = false;
        }
        else
        {
            ErrorMessage = "";
            CanConfirm = true;
        }
    }

    [RelayCommand]
    private void Ok()
    {
        if (!CanConfirm) return;
        DisplayName = DisplayName.Trim();
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
