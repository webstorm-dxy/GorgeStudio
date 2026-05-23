using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GorgeStudio.Models;
using GorgeStudio.Services;

namespace GorgeStudio.ViewModels;

public interface ISettingsCategory
{
    string Name { get; }
}

public partial class TimelineSettingsCategory : ViewModelBase, ISettingsCategory
{
    public string Name => "时间线设置";

    [ObservableProperty]
    private int _bpm;

    [ObservableProperty]
    private int _offset;

    [ObservableProperty]
    private int _beatsPerBar;

    [ObservableProperty]
    private int _subdivisionsPerBeat;

    public TimelineSettingsCategory(ProjectSettings settings)
    {
        _bpm = settings.Bpm;
        _offset = settings.Offset;
        _beatsPerBar = settings.BeatsPerBar;
        _subdivisionsPerBeat = settings.SubdivisionsPerBeat;
    }
}

public partial class ProjectSettingsWindowViewModel : ViewModelBase
{
    private readonly IProjectSettingsService _settingsService;

    [ObservableProperty]
    private ISettingsCategory? _selectedCategory;

    public ObservableCollection<ISettingsCategory> Categories { get; } = new();

    public bool DialogResult { get; private set; }

    public Action? CloseAction { get; set; }

    public ProjectSettingsWindowViewModel()
    {
        _settingsService = null!;
    }

    public ProjectSettingsWindowViewModel(IProjectSettingsService settingsService)
    {
        _settingsService = settingsService;
        var workingCopy = (ProjectSettings)settingsService.CurrentSettings.Clone();
        var timelineCategory = new TimelineSettingsCategory(workingCopy);
        Categories.Add(timelineCategory);
        SelectedCategory = timelineCategory;
    }

    [RelayCommand]
    private void Ok()
    {
        if (SelectedCategory is TimelineSettingsCategory ts)
        {
            _settingsService.SaveSettings(new ProjectSettings
            {
                Bpm = ts.Bpm,
                Offset = ts.Offset,
                BeatsPerBar = ts.BeatsPerBar,
                SubdivisionsPerBeat = ts.SubdivisionsPerBeat,
            });
        }
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
