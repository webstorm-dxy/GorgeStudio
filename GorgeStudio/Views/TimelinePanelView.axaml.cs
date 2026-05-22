using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using GorgeStudio.Models.Chart;
using GorgeStudio.ViewModels;

namespace GorgeStudio.Views;

public partial class TimelinePanelView : UserControl
{
    private const double RulerHeight = 28;
    private const double LaneHeight = 22;
    private const double LaneSpacing = 4;
    private const double TrackStartY = RulerHeight + 4;

    private static readonly IReadOnlyList<IBrush> LaneBrushes = new[]
    {
        Brush.Parse("#4A90D9"),
        Brush.Parse("#E85D75"),
        Brush.Parse("#50C878"),
        Brush.Parse("#F5A623"),
        Brush.Parse("#9B59B6"),
        Brush.Parse("#1ABC9C"),
        Brush.Parse("#E67E22"),
        Brush.Parse("#3498DB"),
    };

    private TimelinePanelViewModel? _vm;
    private readonly Dictionary<string, int> _categoryLanes = new();
    private int _nextLane;

    public TimelinePanelView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vm != null)
            _vm.PropertyChanged -= OnViewModelPropertyChanged;

        _vm = DataContext as TimelinePanelViewModel;

        if (_vm != null)
            _vm.PropertyChanged += OnViewModelPropertyChanged;

        Redraw();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(TimelinePanelViewModel.Elements):
            case nameof(TimelinePanelViewModel.TotalDuration):
            case nameof(TimelinePanelViewModel.PixelsPerSecond):
            case nameof(TimelinePanelViewModel.PlayheadPosition):
                Redraw();
                break;
        }
    }

    private void Redraw()
    {
        TimelineCanvas.Children.Clear();
        _categoryLanes.Clear();
        _nextLane = 0;

        if (_vm == null) return;

        var totalWidth = _vm.TotalDuration * _vm.PixelsPerSecond + 200;
        TimelineCanvas.Width = Math.Max(totalWidth, TimelineScroller.Bounds.Width);

        DrawRuler();
        DrawElements();
        DrawPlayhead();
    }

    private void DrawRuler()
    {
        if (_vm == null) return;

        var totalSeconds = _vm.TotalDuration;
        var pps = _vm.PixelsPerSecond;

        // Ruler background
        TimelineCanvas.Children.Add(new Border
        {
            Background = Brush.Parse("#2A2A2E"),
            Width = TimelineCanvas.Width,
            Height = RulerHeight,
            [Canvas.LeftProperty] = 0.0,
            [Canvas.TopProperty] = 0.0,
        });

        // Tick interval: aim for ~100px between major ticks
        var majorInterval = CalculateTickInterval(pps);
        var minorInterval = majorInterval / 5.0;

        for (var t = 0.0; t <= totalSeconds + majorInterval; t += minorInterval)
        {
            var x = t * pps;
            var isMajor = Math.Abs(t % majorInterval) < 0.001 || t == 0;

            var tickHeight = isMajor ? RulerHeight * 0.55 : RulerHeight * 0.3;
            var tickColor = isMajor
                ? Brush.Parse("#888888")
                : Brush.Parse("#555555");

            TimelineCanvas.Children.Add(new Line
            {
                StartPoint = new Point(0, RulerHeight - tickHeight),
                EndPoint = new Point(0, RulerHeight),
                Stroke = tickColor,
                StrokeThickness = 1,
                [Canvas.LeftProperty] = x,
                [Canvas.TopProperty] = 0.0,
            });

            if (isMajor)
            {
                var label = new TextBlock
                {
                    Text = FormatTime(t),
                    FontSize = 10,
                    Foreground = Brush.Parse("#AAAAAA"),
                    [Canvas.LeftProperty] = x + 3,
                    [Canvas.TopProperty] = 2.0,
                };
                TimelineCanvas.Children.Add(label);
            }
        }

        // Bottom border of ruler
        TimelineCanvas.Children.Add(new Line
        {
            StartPoint = new Point(0, RulerHeight),
            EndPoint = new Point(TimelineCanvas.Width, RulerHeight),
            Stroke = Brush.Parse("#555555"),
            StrokeThickness = 1,
        });
    }

    private void DrawElements()
    {
        if (_vm == null) return;

        var pps = _vm.PixelsPerSecond;
        var elementsByCategory = _vm.Elements.GroupBy(e => e.Category);

        foreach (var group in elementsByCategory)
        {
            var lane = GetLane(group.Key);

            foreach (var element in group)
            {
                var x = element.StartTime * pps;
                var w = Math.Max(element.Duration * pps, 4);
                var y = TrackStartY + lane * (LaneHeight + LaneSpacing);

                var brush = LaneBrushes[lane % LaneBrushes.Count];
                var bar = new Border
                {
                    Background = brush,
                    Width = w,
                    Height = LaneHeight,
                    CornerRadius = new CornerRadius(3),
                    [Canvas.LeftProperty] = x,
                    [Canvas.TopProperty] = y,
                };

                // Tooltip
                ToolTip.SetTip(bar, $"{element.Label}\n{FormatTime(element.StartTime)}" +
                    (element.Duration > 0 ? $" — {FormatTime(element.StartTime + element.Duration)}" : ""));

                TimelineCanvas.Children.Add(bar);

                // Label on bar (if wide enough)
                if (w > 40)
                {
                    var label = new TextBlock
                    {
                        Text = element.Label,
                        FontSize = 9,
                        Foreground = Brushes.White,
                        [Canvas.LeftProperty] = x + 4,
                        [Canvas.TopProperty] = y + 3,
                    };
                    TimelineCanvas.Children.Add(label);
                }
            }
        }

        // Update canvas height to fit all lanes
        var totalHeight = TrackStartY + (_nextLane + 1) * (LaneHeight + LaneSpacing) + 16;
        TimelineCanvas.Height = Math.Max(totalHeight, TimelineScroller.Bounds.Height - 8);
    }

    private void DrawPlayhead()
    {
        if (_vm == null) return;

        var x = _vm.PlayheadPosition * _vm.PixelsPerSecond;

        TimelineCanvas.Children.Add(new Line
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, TimelineCanvas.Height),
            Stroke = Brush.Parse("#FF4444"),
            StrokeThickness = 2,
            [Canvas.LeftProperty] = x,
            [Canvas.TopProperty] = 0.0,
        });

        // Triangle handle at top
        var triangle = new Polygon
        {
            Points = new[] { new Point(0, 0), new Point(-6, RulerHeight), new Point(6, RulerHeight) },
            Fill = Brush.Parse("#FF4444"),
            [Canvas.LeftProperty] = x,
            [Canvas.TopProperty] = 0.0,
        };
        TimelineCanvas.Children.Add(triangle);
    }

    private int GetLane(string category)
    {
        if (!_categoryLanes.TryGetValue(category, out var lane))
        {
            lane = _nextLane++;
            _categoryLanes[category] = lane;
        }
        return lane;
    }

    private static double CalculateTickInterval(double pps)
    {
        // Aim for ~100px between major ticks
        var targetSeconds = 100.0 / pps;

        double[] intervals = { 0.1, 0.25, 0.5, 1, 2, 5, 10, 30, 60, 120, 300, 600 };
        foreach (var interval in intervals)
        {
            if (interval >= targetSeconds)
                return interval;
        }
        return 600;
    }

    private static string FormatTime(double seconds)
    {
        if (seconds < 60)
            return $"{seconds:F1}s";
        var min = (int)(seconds / 60);
        var sec = seconds % 60;
        return $"{min}:{sec:00.0}";
    }

    private void ZoomIn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_vm != null)
            _vm.ZoomLevel = Math.Min(_vm.ZoomLevel * 1.5, 20.0);
    }

    private void ZoomOut_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_vm != null)
            _vm.ZoomLevel = Math.Max(_vm.ZoomLevel / 1.5, 0.1);
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        // Keep playhead visible if needed
    }
}
