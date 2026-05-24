using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gorge.GorgeLanguage.Objective;
using Gorge.Native.Gorge;
using GorgeStudio.Models.Chart;
using GorgeStudio.Services;
using GorgeStudio.Views;
using Microsoft.Extensions.DependencyInjection;

namespace GorgeStudio.ViewModels;

public partial class TimelinePanelViewModel : ViewModelBase
{
    private readonly IProjectSettingsService? _settingsService;
    private readonly IServiceProvider? _serviceProvider;
    private SimulationScore? _simulationScore;

    [ObservableProperty]
    private string _title = "Timeline";

    [ObservableProperty]
    private ObservableCollection<TimelineElement> _elements = new();

    [ObservableProperty]
    private double _totalDuration = 10.0;

    [ObservableProperty]
    private double _playheadPosition;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private double _pixelsPerSecond = 100.0;

    [ObservableProperty]
    private int _bpm = 120;

    [ObservableProperty]
    private int _beatsPerBar = 4;

    [ObservableProperty]
    private int _subdivisionsPerBeat = 4;

    [ObservableProperty]
    private ObservableCollection<TrackInfo> _tracks = new();

    [ObservableProperty]
    private int _selectedTrackIndex = -1;

    public double TrackRowHeight => 40.0;

    public TimelinePanelViewModel()
    {
    }

    public TimelinePanelViewModel(IProjectSettingsService settingsService, IServiceProvider serviceProvider)
    {
        _settingsService = settingsService;
        _serviceProvider = serviceProvider;
        LoadSettingsFromService();
    }

    partial void OnZoomLevelChanged(double value)
    {
        PixelsPerSecond = Math.Max(10, Math.Min(2000, 100.0 * value));
    }

    private void LoadSettingsFromService()
    {
        if (_settingsService?.CurrentSettings is { } s)
        {
            Bpm = s.Bpm;
            BeatsPerBar = s.BeatsPerBar;
            SubdivisionsPerBeat = s.SubdivisionsPerBeat;
        }
    }

    public void SetChartDocument(SimulationScore score)
    {
        _simulationScore = score;
        Elements.Clear();
        Tracks.Clear();

        var newElements = new List<TimelineElement>();

        foreach (var staff in score.Stave)
        {
            if (staff is ElementStaff elementStaff)
            {
                foreach (var period in elementStaff.Periods)
                {
                    foreach (var element in period.Elements)
                    {
                        ExtractElement(element, staff.ClassName, newElements);
                    }
                }
            }
        }

        newElements.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));

        foreach (var e in newElements)
            Elements.Add(e);

        var trackNames = newElements
            .Select(e => e.SourceInjector)
            .Distinct()
            .OrderBy(n => n);
        foreach (var name in trackNames)
            Tracks.Add(new TrackInfo { Name = name });

        if (newElements.Count > 0)
        {
            var maxEnd = newElements.Max(e => e.StartTime + e.Duration);
            TotalDuration = Math.Ceiling(Math.Max(maxEnd, 1.0));
        }
    }

    [RelayCommand]
    private async Task AddTrackAsync()
    {
        if (_simulationScore == null) return;

        var vm = new StaffTypeSelectionWindowViewModel();
        vm.StaffTypes.Add(new StaffTypeOption
            { Annotation = "ElementStaff", DisplayName = "Element 谱表", RequiresForm = true });
        vm.StaffTypes.Add(new StaffTypeOption
            { Annotation = "AudioStaff", DisplayName = "Audio 谱表", RequiresForm = false });

        if (_settingsService?.CurrentSettings?.Forms is { } forms)
            foreach (var f in forms)
                vm.Forms.Add(f);

        var window = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
        if (window == null) return;

        var result = await StaffTypeSelectionWindow.ShowAsync(window, vm);
        if (!result || vm.SelectedStaffType == null) return;

        var typePrefix = vm.SelectedStaffType.Annotation;
        var existingNumbers = _simulationScore.Stave
            .Where(s => s.ClassName.StartsWith(typePrefix))
            .Select(s =>
            {
                var numPart = s.ClassName[typePrefix.Length..];
                return int.TryParse(numPart, out var n) ? n : 1;
            })
            .ToList();
        var nextNumber = existingNumbers.Count > 0 ? existingNumbers.Max() + 1 : 1;
        var className = $"{typePrefix}{nextNumber}";
        var displayName = $"{typePrefix}谱表";

        IStaff newStaff = vm.SelectedStaffType.RequiresForm
            ? new ElementStaff(className, true, displayName, vm.SelectedForm ?? "Default")
            : new AudioStaff(className, true, displayName);

        _simulationScore.Stave.Add(newStaff);
        Tracks.Add(new TrackInfo { Name = className });
    }

    [RelayCommand]
    private void DeleteTrack()
    {
        if (SelectedTrackIndex >= 0 && SelectedTrackIndex < Tracks.Count)
        {
            var trackName = Tracks[SelectedTrackIndex].Name;
            if (_simulationScore?.TryGetStaff(trackName, out var staff) == true)
                _simulationScore.Stave.Remove(staff);
            Tracks.RemoveAt(SelectedTrackIndex);
            SelectedTrackIndex = -1;
        }
    }

    [RelayCommand]
    private void CopyTrack()
    {
        if (SelectedTrackIndex >= 0 && SelectedTrackIndex < Tracks.Count)
        {
            var source = Tracks[SelectedTrackIndex];
            Tracks.Insert(SelectedTrackIndex + 1, new TrackInfo { Name = $"{source.Name} (Copy)" });
        }
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is { } window
            && _serviceProvider != null)
        {
            var vm = _serviceProvider.GetRequiredService<ProjectSettingsWindowViewModel>();
            var result = await ProjectSettingsWindow.ShowAsync(window, vm);
            if (result)
                LoadSettingsFromService();
        }
    }

    private static void ExtractElement(Injector element, string staffName, List<TimelineElement> results)
    {
        var decl = element.InjectedClassDeclaration;
        var elementType = decl.Name;

        double startTime = 0;
        double duration = 0;
        bool hasTime = false;

        foreach (var field in decl.InjectorFields)
        {
            var name = field.Name;
            if (IsTimeFieldName(name) && field.Type.BasicType == BasicType.Float
                && !element.GetInjectorFloatDefault(field.Index))
            {
                startTime = element.GetInjectorFloat(field.Index);
                hasTime = true;
            }
            if (name.Contains("duration", StringComparison.OrdinalIgnoreCase)
                && field.Type.BasicType == BasicType.Float
                && !element.GetInjectorFloatDefault(field.Index))
            {
                duration = element.GetInjectorFloat(field.Index);
            }
        }

        if (hasTime)
        {
            results.Add(new TimelineElement
            {
                Label = $"{staffName}.{elementType}",
                StartTime = startTime,
                Duration = duration,
                Category = elementType,
                SourceInjector = staffName
            });
        }
    }

    private static bool IsTimeFieldName(string name)
    {
        return name.Equals("time", StringComparison.OrdinalIgnoreCase)
               || name.Equals("hitTime", StringComparison.OrdinalIgnoreCase)
               || name.Equals("startTime", StringComparison.OrdinalIgnoreCase)
               || name.Equals("beat", StringComparison.OrdinalIgnoreCase)
               || name.Equals("startBeat", StringComparison.OrdinalIgnoreCase);
    }
}

public class TrackInfo
{
    public string Name { get; set; } = "";
}
