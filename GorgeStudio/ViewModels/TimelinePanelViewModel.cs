using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Gorge.GorgeLanguage.Objective;
using Gorge.Native.Gorge;
using GorgeStudio.Models.Chart;

namespace GorgeStudio.ViewModels;

public partial class TimelinePanelViewModel : ViewModelBase
{
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

    partial void OnZoomLevelChanged(double value)
    {
        PixelsPerSecond = Math.Max(10, Math.Min(2000, 100.0 * value));
    }

    public void SetChartDocument(SimulationScore score)
    {
        Elements.Clear();
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

        if (newElements.Count > 0)
        {
            var maxEnd = newElements.Max(e => e.StartTime + e.Duration);
            TotalDuration = Math.Ceiling(Math.Max(maxEnd, 1.0));
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
