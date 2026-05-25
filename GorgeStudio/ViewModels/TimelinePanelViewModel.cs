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
using GorgeStudio.Models.Timeline;
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

    public const double MinPixelsPerSecond = 10.0;
    public const double MaxPixelsPerSecond = 2000.0;
    public const double DefaultPixelsPerSecond = 100.0;
    private const double ZoomStepMultiplier = 1.1;

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

    // Snap state (session-only, not persisted)
    [ObservableProperty]
    private bool _isSnapEnabled;

    [ObservableProperty]
    private TimelineSnapMode _snapMode = TimelineSnapMode.Subdivision;

    [ObservableProperty]
    private int _offset;

    public ObservableCollection<SnapModeOption> SnapModeOptions { get; } = new()
    {
        new() { Mode = null, DisplayName = "关" },
        new() { Mode = TimelineSnapMode.Bar, DisplayName = "小节" },
        new() { Mode = TimelineSnapMode.Beat, DisplayName = "拍" },
        new() { Mode = TimelineSnapMode.Subdivision, DisplayName = "细分" }
    };

    public SnapModeOption SelectedSnapModeOption
    {
        get
        {
            if (!IsSnapEnabled)
                return SnapModeOptions[0];
            return SnapModeOptions.FirstOrDefault(o => o.Mode == SnapMode) ?? SnapModeOptions[^1];
        }
        set
        {
            if (value?.Mode == null)
                IsSnapEnabled = false;
            else
            {
                IsSnapEnabled = true;
                SnapMode = value.Mode.Value;
            }
        }
    }

    partial void OnSnapModeChanged(TimelineSnapMode value)
    {
        OnPropertyChanged(nameof(SelectedSnapModeOption));
    }

    partial void OnIsSnapEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(SelectedSnapModeOption));
    }

    public double SnapTime(double seconds)
    {
        return TimelineSnapper.Snap(
            seconds, IsSnapEnabled, SnapMode,
            Bpm, Offset, BeatsPerBar, SubdivisionsPerBeat);
    }

    public double SnapDuration(double duration)
    {
        return TimelineSnapper.SnapDuration(
            duration, IsSnapEnabled, SnapMode,
            Bpm, BeatsPerBar, SubdivisionsPerBeat);
    }

    public bool TryZoomTimeline(double multiplier, out double oldPps, out double newPps)
    {
        oldPps = PixelsPerSecond;
        newPps = Math.Clamp(oldPps * multiplier, MinPixelsPerSecond, MaxPixelsPerSecond);

        if (Math.Abs(newPps - oldPps) < 0.001)
            return false;

        ZoomLevel = newPps / DefaultPixelsPerSecond;
        return true;
    }

    private double GetSnapIntervalSeconds()
    {
        var beatSeconds = Bpm > 0 ? 60.0 / Bpm : 0.5;
        return SnapMode switch
        {
            TimelineSnapMode.Bar => beatSeconds * BeatsPerBar,
            TimelineSnapMode.Beat => beatSeconds,
            TimelineSnapMode.Subdivision => beatSeconds / SubdivisionsPerBeat,
            _ => beatSeconds / SubdivisionsPerBeat
        };
    }

    public double TrackRowHeight => 40.0;
    public double TrackListHeight => TrackCount * TrackRowHeight;
    public double TimelineContentHeight => Math.Max(TrackListHeight, 480.0);

    public bool CanAddPeriod => SelectedTrackIndex >= 0 && SelectedTrackIndex < TrackCount;
    public bool CanDeletePeriod => SelectedPeriod != null;
    public bool CanCopyPeriod => SelectedPeriod != null;

    public void SelectPeriod(IPeriod? period)
    {
        SelectedPeriod = period;
        SelectionChanged?.Invoke(period);
    }

    private IPeriod? _previewPeriod;
    private double? _previewTimeOffsetSeconds;
    private double? _previewMinLengthSeconds;

    public void PreviewPeriodTimeOffset(IPeriod period, double timeOffsetSeconds)
    {
        var clamped = Math.Max(0, timeOffsetSeconds);
        _previewPeriod = period;
        _previewTimeOffsetSeconds = clamped;

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
        if (_previewPeriod == period && _previewTimeOffsetSeconds.HasValue)
        {
            _periodEditingService.UpdatePeriodTimeOffset(period, (float)_previewTimeOffsetSeconds.Value);
        }

        _previewPeriod = null;
        _previewTimeOffsetSeconds = null;
        _previewMinLengthSeconds = null;

        ClearAllPreviews();
        RefreshFromScore();
        SelectPeriod(period);
    }

    public void PreviewPeriodMinLength(IPeriod period, double minLengthSeconds)
    {
        var clamped = Math.Max(TimelineTimeConverter.MinimumPeriodLengthSeconds, minLengthSeconds);
        _previewPeriod = period;
        _previewMinLengthSeconds = clamped;

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
        if (_previewPeriod == period && _previewMinLengthSeconds.HasValue)
        {
            _periodEditingService.UpdatePeriodMinLength(period, (float)_previewMinLengthSeconds.Value);
        }

        _previewPeriod = null;
        _previewMinLengthSeconds = null;

        ClearAllPreviews();
        RefreshFromScore();
        SelectPeriod(period);
    }

    public void CancelPeriodTimeOffsetPreview()
    {
        _previewPeriod = null;
        _previewTimeOffsetSeconds = null;
        _previewMinLengthSeconds = null;
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
        PixelsPerSecond = Math.Clamp(DefaultPixelsPerSecond * value, MinPixelsPerSecond, MaxPixelsPerSecond);
    }

    partial void OnSelectedTrackIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CanAddPeriod));
    }

    partial void OnTrackCountChanged(int value)
    {
        OnPropertyChanged(nameof(CanAddPeriod));
        OnPropertyChanged(nameof(TrackListHeight));
        OnPropertyChanged(nameof(TimelineContentHeight));
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
            Offset = s.Offset;
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
        _previewTimeOffsetSeconds = null;
        _previewMinLengthSeconds = null;

        Elements.Clear();
        Tracks.Clear();

        // Build tracks from score.Stave with Staff references and Period block info
        foreach (var staff in _simulationScore.Stave)
        {
            var track = new TrackInfo { Staff = staff };
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
        Tracks.Add(new TrackInfo { Staff = newStaff });
        TrackCount = Tracks.Count;
        ScoreChanged?.Invoke();
    }

    [RelayCommand]
    private void DeleteTrack()
    {
        if (SelectedTrackIndex >= 0 && SelectedTrackIndex < Tracks.Count)
        {
            var track = Tracks[SelectedTrackIndex];
            if (track.Staff != null && _simulationScore != null)
                _simulationScore.Stave.Remove(track.Staff);
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
                var baseName = source.ClassName;
                var newName = baseName;
                var counter = 1;
                while (_simulationScore.CheckStaffNameConflict(newName))
                {
                    newName = $"{baseName}{counter}";
                    counter++;
                }
                clonedStaff.ClassName = newName;

                var copyDisplayName = $"{(string.IsNullOrWhiteSpace(source.Staff.DisplayName) ? source.ClassName : source.Staff.DisplayName)} 副本";
                if (copyDisplayName.Length > 64)
                    copyDisplayName = copyDisplayName[..64];
                clonedStaff.DisplayName = copyDisplayName;

                _simulationScore.Stave.Add(clonedStaff);
                Tracks.Insert(SelectedTrackIndex + 1, new TrackInfo { Staff = clonedStaff });
            }
            TrackCount = Tracks.Count;
            ScoreChanged?.Invoke();
        }
    }

    [RelayCommand]
    private async Task RenameTrackDisplayNameAsync(TrackInfo? track)
    {
        if (track?.Staff == null) return;

        var window = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
        if (window == null) return;

        var vm = new RenameStaffDisplayNameWindowViewModel(track.ClassName, track.Staff.DisplayName);
        var result = await RenameStaffDisplayNameWindow.ShowAsync(window, vm);
        if (!result) return;

        track.Staff.DisplayName = vm.DisplayName;
        track.RefreshDisplayName();
        ScoreChanged?.Invoke();
        SelectionChanged?.Invoke(track.Staff);
    }

    [RelayCommand]
    private void AddPeriod()
    {
        if (_simulationScore == null || _periodEditingService == null) return;
        if (SelectedTrackIndex < 0 || SelectedTrackIndex >= Tracks.Count) return;

        var track = Tracks[SelectedTrackIndex];
        if (track.Staff == null) return;

        var period = _periodEditingService.CreatePeriod(
            track.Staff,
            _simulationScore,
            (float)SnapTime(PlayheadPosition));
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

public class TrackInfo : ObservableObject
{
    private IStaff? _staff;

    public IStaff? Staff
    {
        get => _staff;
        set
        {
            if (SetProperty(ref _staff, value))
            {
                OnPropertyChanged(nameof(ClassName));
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public string ClassName => Staff?.ClassName ?? "";

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Staff?.DisplayName) ? ClassName : Staff.DisplayName;

    public string Name => DisplayName;

    public ObservableCollection<PeriodBlockInfo> Periods { get; set; } = new();

    public void RefreshDisplayName()
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(Name));
    }
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
            effectiveMinLength = Math.Max(TimelineTimeConverter.MinimumPeriodLengthSeconds, effectiveMinLength);
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

public sealed class SnapModeOption
{
    public TimelineSnapMode? Mode { get; init; }
    public string DisplayName { get; init; } = "";
}
