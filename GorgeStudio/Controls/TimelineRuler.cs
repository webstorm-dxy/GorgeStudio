using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GorgeStudio.Models.Timeline;

namespace GorgeStudio.Controls;

public class TimelineRuler : Control
{
    private const double MinimumScrollableWidth = 2400.0;

    private static readonly IBrush DefaultBgBrush = new SolidColorBrush(Color.FromRgb(30, 30, 30));
    private static readonly IBrush DefaultMinorBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120));
    private static readonly IBrush DefaultMajorBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200));
    private static readonly IBrush DefaultBorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));

    public static readonly StyledProperty<double> PixelsPerSecondProperty =
        AvaloniaProperty.Register<TimelineRuler, double>(nameof(PixelsPerSecond), 100.0);

    public static readonly StyledProperty<double> ScrollOffsetXProperty =
        AvaloniaProperty.Register<TimelineRuler, double>(nameof(ScrollOffsetX));

    public static readonly StyledProperty<int> BpmProperty =
        AvaloniaProperty.Register<TimelineRuler, int>(nameof(Bpm), 120);

    public static readonly StyledProperty<int> BeatsPerBarProperty =
        AvaloniaProperty.Register<TimelineRuler, int>(nameof(BeatsPerBar), 4);

    public static readonly StyledProperty<int> SubdivisionsPerBeatProperty =
        AvaloniaProperty.Register<TimelineRuler, int>(nameof(SubdivisionsPerBeat), 4);

    public static readonly StyledProperty<int> OffsetMillisecondsProperty =
        AvaloniaProperty.Register<TimelineRuler, int>(nameof(OffsetMilliseconds));

    public static readonly StyledProperty<double> TotalDurationSecondsProperty =
        AvaloniaProperty.Register<TimelineRuler, double>(nameof(TotalDurationSeconds), 10.0);

    public int OffsetMilliseconds
    {
        get => GetValue(OffsetMillisecondsProperty);
        set => SetValue(OffsetMillisecondsProperty, value);
    }

    public double PixelsPerSecond
    {
        get => GetValue(PixelsPerSecondProperty);
        set => SetValue(PixelsPerSecondProperty, value);
    }

    public double ScrollOffsetX
    {
        get => GetValue(ScrollOffsetXProperty);
        set => SetValue(ScrollOffsetXProperty, value);
    }

    public int Bpm
    {
        get => GetValue(BpmProperty);
        set => SetValue(BpmProperty, value);
    }

    public int BeatsPerBar
    {
        get => GetValue(BeatsPerBarProperty);
        set => SetValue(BeatsPerBarProperty, value);
    }

    public int SubdivisionsPerBeat
    {
        get => GetValue(SubdivisionsPerBeatProperty);
        set => SetValue(SubdivisionsPerBeatProperty, value);
    }

    public double TotalDurationSeconds
    {
        get => GetValue(TotalDurationSecondsProperty);
        set => SetValue(TotalDurationSecondsProperty, value);
    }

    private static readonly AvaloniaProperty[] RenderProperties =
    {
        PixelsPerSecondProperty, ScrollOffsetXProperty, BpmProperty,
        BeatsPerBarProperty, SubdivisionsPerBeatProperty, TotalDurationSecondsProperty,
        OffsetMillisecondsProperty
    };

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        foreach (var p in RenderProperties)
        {
            if (change.Property == p)
            {
                InvalidateVisual();
                InvalidateMeasure();
                return;
            }
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var desiredWidth = Math.Max(TotalDurationSeconds * PixelsPerSecond, MinimumScrollableWidth);
        return new Size(desiredWidth, 0);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = Math.Max(Math.Max(TotalDurationSeconds * PixelsPerSecond, MinimumScrollableWidth), finalSize.Width);
        return new Size(width, finalSize.Height);
    }

    public override void Render(DrawingContext context)
    {
        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 0 || height <= 0) return;

        // Background
        context.FillRectangle(DefaultBgBrush.ToImmutable(), new Rect(0, 0, width, height));

        var division = TickDivision.Calculate(Bpm, BeatsPerBar, SubdivisionsPerBeat, PixelsPerSecond);
        if (division.TickIntervalSeconds <= 0) return;

        var scrollX = ScrollOffsetX;
        var offsetSeconds = OffsetMilliseconds / 1000.0;
        var startTime = scrollX / PixelsPerSecond;
        var endTime = (scrollX + width) / PixelsPerSecond;

        var firstTick = (int)Math.Floor((startTime - offsetSeconds) / division.TickIntervalSeconds);
        var lastTick = (int)Math.Ceiling((endTime - offsetSeconds) / division.TickIntervalSeconds);

        var minorPen = new Pen(DefaultMinorBrush, 1);
        var highlightPen = new Pen(DefaultMinorBrush, 1);
        var majorPen = new Pen(DefaultMajorBrush, 1.5);

        for (var i = firstTick; i <= lastTick; i++)
        {
            var time = offsetSeconds + i * division.TickIntervalSeconds;
            var x = time * PixelsPerSecond - scrollX;
            if (x < 0 || x > width) continue;

            var isMajor = i % division.MajorTickInterval == 0;
            var isHighlight = !isMajor && division.HighlightTickInterval > 0
                && i % division.HighlightTickInterval == 0;

            if (isMajor)
            {
                var tickHeight = height * 0.6;
                context.DrawLine(majorPen, new Point(x, height - tickHeight), new Point(x, height));

                var barNumber = i / division.MajorTickInterval + 1;
                var typeface = new Typeface("Segoe UI");
                var text = new FormattedText(
                    barNumber.ToString(),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    10,
                    DefaultMajorBrush);
                context.DrawText(text, new Point(x + 3, 2));
            }
            else if (isHighlight)
            {
                var tickHeight = height * 0.45;
                context.DrawLine(highlightPen, new Point(x, height - tickHeight), new Point(x, height));

                if (division.HighlightTickInterval > 0)
                {
                    var barNumber = i / division.MajorTickInterval + 1;
                    var beatInBar = (i % division.MajorTickInterval) / division.HighlightTickInterval + 1;
                    var typeface = new Typeface("Segoe UI");
                    var label = $"{barNumber}:{beatInBar}/{BeatsPerBar}";
                    var highlightText = new FormattedText(
                        label,
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        8,
                        DefaultMinorBrush);
                    context.DrawText(highlightText, new Point(x + 2, 2));
                }
            }
            else
            {
                var tickHeight = height * 0.2;
                context.DrawLine(minorPen, new Point(x, height - tickHeight), new Point(x, height));
            }
        }

        // Bottom border
        context.DrawLine(new Pen(DefaultBorderBrush, 1), new Point(0, height - 0.5), new Point(width, height - 0.5));
    }
}
