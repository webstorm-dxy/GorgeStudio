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
    private readonly IPeriodEditingService? _periodEditingService;
    private SimulationScore? _simulationScore;

    public event Action? ScoreChanged;
    public event Action<object?>? SelectionChanged;

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

    public double ContentWidth => TotalDuration * PixelsPerSecond;

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

    [ObservableProperty]
    private int _trackCount;

    [ObservableProperty]
    private IPeriod? _selectedPeriod;

    public double TrackRowHeight => 40.0;

    public bool CanAddPeriod => SelectedTrackIndex >= 0 && SelectedTrackIndex < TrackCount;
    public bool CanDeletePeriod => SelectedPeriod != null;
    public bool CanCopyPeriod => SelectedPeriod != null;

    public void SelectPeriod(IPeriod? period)
    {
        SelectedPeriod = period;
        SelectionChanged?.Invoke(period);
    }

    private IPeriod? _previewPeriod;
    private double? _previewTimeOffset;
    private double? _previewMinLength;

    public void PreviewPeriodTimeOffset(IPeriod period, double timeOffset)
    {
        var clamped = Math.Max(0, timeOffset);
        _previewPeriod = period;
        _previewTimeOffset = clamped;

        // Find the matching PeriodBlockInfo and set its preview
        foreach (var track in Tracks)
        {
            foreach (var block in track.Periods)
            {
                if (block.Period == period)
                {
                    block.PreviewStartSeconds = clamped;

                    // Expand TotalDuration if preview extends beyond it
                    var previewEnd = clamped + block.DurationSeconds;
                    if (previewEnd > TotalDuration)
                        TotalDuration = Math.Ceiling(previewEnd);
                    return;
                }
            }
        }
    }

    public void CommitPeriodTimeOffset(IPeriod period)
    {
        if (_periodEditingService == null) return;
        if (_previewPeriod == period && _previewTimeOffset.HasValue)
        {
            _periodEditingService.UpdatePeriodTimeOffset(period, (float)_previewTimeOffset.Value);
        }

        _previewPeriod = null;
        _previewTimeOffset = null;
        _previewMinLength = null;

        ClearAllPreviews();
        RefreshFromScore();
        SelectPeriod(period);
    }

    public void PreviewPeriodMinLength(IPeriod period, double minLength)
    {
        var clamped = Math.Max(0.25, minLength);
        _previewPeriod = period;
        _previewMinLength = clamped;

        foreach (var track in Tracks)
        {
            foreach (var block in track.Periods)
            {
                if (block.Period == period)
                {
                    block.PreviewMinLengthSeconds = clamped;

                    var previewEnd = block.StartSeconds + block.DurationSeconds;
                    if (previewEnd > TotalDuration)
                        TotalDuration = Math.Ceiling(previewEnd);
                    return;
                }
            }
        }
    }

    public void CommitPeriodMinLength(IPeriod period)
    {
        if (_periodEditingService == null) return;
        if (_previewPeriod == period && _previewMinLength.HasValue)
        {
            _periodEditingService.UpdatePeriodMinLength(period, (float)_previewMinLength.Value);
        }

        _previewPeriod = null;
        _previewMinLength = null;

        ClearAllPreviews();
        RefreshFromScore();
        SelectPeriod(period);
    }

    public void CancelPeriodTimeOffsetPreview()
    {
        _previewPeriod = null;
        _previewTimeOffset = null;
        _previewMinLength = null;
        ClearAllPreviews();
    }

    private void ClearAllPreviews()
    {
        foreach (var track in Tracks)
        {
            foreach (var block in track.Periods)
            {
                block.PreviewStartSeconds = null;
                block.PreviewMinLengthSeconds = null;
            }
        }
    }

    public TimelinePanelViewModel()
    {
    }

    public TimelinePanelViewModel(IProjectSettingsService settingsService, IServiceProvider serviceProvider, IPeriodEditingService periodEditingService)
    {
        _settingsService = settingsService;
        _serviceProvider = serviceProvider;
        _periodEditingService = periodEditingService;
        LoadSettingsFromService();
    }

    partial void OnZoomLevelChanged(double value)
    {
        PixelsPerSecond = Math.Max(10, Math.Min(2000, 100.0 * value));
    }

    partial void OnSelectedTrackIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CanAddPeriod));
    }

    partial void OnTrackCountChanged(int value)
    {
        OnPropertyChanged(nameof(CanAddPeriod));
    }

    partial void OnSelectedPeriodChanged(IPeriod? value)
    {
        OnPropertyChanged(nameof(CanDeletePeriod));
        OnPropertyChanged(nameof(CanCopyPeriod));
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
        RefreshFromScore();
    }

    private void RefreshFromScore()
    {
        if (_simulationScore == null) return;

        _previewPeriod = null;
        _previewTimeOffset = null;
        _previewMinLength = null;

        Elements.Clear();
        Tracks.Clear();

        // Build tracks from score.Stave with Staff references and Period block info
        foreach (var staff in _simulationScore.Stave)
        {
            var track = new TrackInfo { Name = staff.ClassName, Staff = staff };
            foreach (var period in staff.Periods)
            {
                track.Periods.Add(new PeriodBlockInfo { Staff = staff, Period = period });
            }
            Tracks.Add(track);
        }
        TrackCount = Tracks.Count;

        // Populate TimelineElement list for element rendering on tracks
        var newElements = new List<TimelineElement>();
        foreach (var staff in _simulationScore.Stave)
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

        // Calculate TotalDuration from elements and period blocks
        double maxEnd = 1.0;
        foreach (var e in newElements)
            maxEnd = Math.Max(maxEnd, e.StartTime + e.Duration);
        foreach (var track in Tracks)
            foreach (var block in track.Periods)
                maxEnd = Math.Max(maxEnd, block.StartSeconds + block.DurationSeconds);
        TotalDuration = Math.Ceiling(Math.Max(maxEnd, 1.0));

        ScoreChanged?.Invoke();
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
        Tracks.Add(new TrackInfo { Name = className, Staff = newStaff });
        TrackCount = Tracks.Count;
        ScoreChanged?.Invoke();
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
            TrackCount = Tracks.Count;
            ScoreChanged?.Invoke();
        }
    }

    [RelayCommand]
    private void CopyTrack()
    {
        if (_simulationScore == null) return;
        if (SelectedTrackIndex >= 0 && SelectedTrackIndex < Tracks.Count)
        {
            var source = Tracks[SelectedTrackIndex];
            if (source.Staff != null)
            {
                var clonedStaff = source.Staff.Clone();
                var baseName = source.Name;
                var newName = baseName;
                var counter = 1;
                while (_simulationScore.CheckStaffNameConflict(newName))
                {
                    newName = $"{baseName}{counter}";
                    counter++;
                }
                clonedStaff.ClassName = newName;
                clonedStaff.DisplayName = newName;
                _simulationScore.Stave.Add(clonedStaff);
                Tracks.Insert(SelectedTrackIndex + 1, new TrackInfo { Name = newName, Staff = clonedStaff });
            }
            TrackCount = Tracks.Count;
            ScoreChanged?.Invoke();
        }
    }

    [RelayCommand]
    private void AddPeriod()
    {
        if (_simulationScore == null || _periodEditingService == null) return;
        if (SelectedTrackIndex < 0 || SelectedTrackIndex >= Tracks.Count) return;

        var track = Tracks[SelectedTrackIndex];
        if (track.Staff == null) return;

        var period = _periodEditingService.CreatePeriod(track.Staff, _simulationScore, (float)PlayheadPosition);
        _periodEditingService.InsertPeriod(track.Staff, period);
        RefreshFromScore();
    }

    [RelayCommand]
    private void DeletePeriod()
    {
        if (_simulationScore == null || _periodEditingService == null) return;
        if (SelectedPeriod == null) return;

        foreach (var staff in _simulationScore.Stave)
        {
            if (staff.Periods.Any(p => p == SelectedPeriod))
            {
                _periodEditingService.RemovePeriod(staff, SelectedPeriod);
                SelectedPeriod = null;
                RefreshFromScore();
                return;
            }
        }
    }

    [RelayCommand]
    private void CopyPeriod()
    {
        if (_simulationScore == null || _periodEditingService == null) return;
        if (SelectedPeriod == null) return;

        foreach (var staff in _simulationScore.Stave)
        {
            if (staff.Periods.Any(p => p == SelectedPeriod))
            {
                _periodEditingService.DuplicatePeriod(staff, SelectedPeriod);
                RefreshFromScore();
                return;
            }
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
    public IStaff? Staff { get; set; }
    public ObservableCollection<PeriodBlockInfo> Periods { get; set; } = new();
}

public partial class PeriodBlockInfo : ObservableObject
{
    public IStaff Staff { get; set; } = null!;
    public IPeriod Period { get; set; } = null!;
    public string DisplayName => Period.MethodName;

    [ObservableProperty]
    private double? _previewStartSeconds;

    [ObservableProperty]
    private double? _previewMinLengthSeconds;

    public double StartSeconds => PreviewStartSeconds ?? Period.TimeOffset;
    public double DurationSeconds
    {
        get
        {
            var effectiveMinLength = PreviewMinLengthSeconds ?? Period.MinLength;
            effectiveMinLength = Math.Max(0.25, effectiveMinLength);
            if (Period is ElementPeriod ep)
            {
                var maxElementEnd = ep.Elements.Count > 0
                    ? ep.Elements.Max(e =>
                    {
                        var decl = e.InjectedClassDeclaration;
                        if (decl.TryGetInjectorFieldByName("time", out var tf) && !e.GetInjectorFloatDefault(tf.Index))
                            return e.GetInjectorFloat(tf.Index);
                        if (decl.TryGetInjectorFieldByName("hitTime", out var htf) && !e.GetInjectorFloatDefault(htf.Index))
                            return e.GetInjectorFloat(htf.Index);
                        if (decl.TryGetInjectorFieldByName("startTime", out var stf) && !e.GetInjectorFloatDefault(stf.Index))
                            return e.GetInjectorFloat(stf.Index);
                        return 0f;
                    })
                    : 0;
                return Math.Max(effectiveMinLength, maxElementEnd - StartSeconds);
            }
            return effectiveMinLength;
        }
    }
    public bool IsSelected { get; set; }
    public bool IsAudio => Period is AudioPeriod;
}
