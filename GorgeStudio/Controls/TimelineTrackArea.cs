using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace GorgeStudio.Controls;

public class TimelineTrackArea : Control
{
    private const double MinimumScrollableWidth = 2400.0;
    private const double MinimumScrollableHeight = 480.0;

    private static readonly IBrush DefaultBgBrush = new SolidColorBrush(Color.FromRgb(35, 35, 35));
    private static readonly IBrush DefaultLineBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
    private static readonly IBrush DefaultSelectedHighlightBrush = new SolidColorBrush(Color.FromRgb(35, 74, 108));
    private static readonly Color DefaultAccentColor = Color.FromRgb(45, 140, 235);

    public static readonly StyledProperty<double> PixelsPerSecondProperty =
        AvaloniaProperty.Register<TimelineTrackArea, double>(nameof(PixelsPerSecond), 100.0);

    public static readonly StyledProperty<double> ScrollOffsetXProperty =
        AvaloniaProperty.Register<TimelineTrackArea, double>(nameof(ScrollOffsetX));

    public static readonly StyledProperty<double> ScrollOffsetYProperty =
        AvaloniaProperty.Register<TimelineTrackArea, double>(nameof(ScrollOffsetY));

    public static readonly StyledProperty<double> PlayheadPositionSecondsProperty =
        AvaloniaProperty.Register<TimelineTrackArea, double>(nameof(PlayheadPositionSeconds));

    public static readonly StyledProperty<int> TrackCountProperty =
        AvaloniaProperty.Register<TimelineTrackArea, int>(nameof(TrackCount));

    public static readonly StyledProperty<double> TrackRowHeightProperty =
        AvaloniaProperty.Register<TimelineTrackArea, double>(nameof(TrackRowHeight), 40.0);

    public static readonly StyledProperty<double> TotalDurationSecondsProperty =
        AvaloniaProperty.Register<TimelineTrackArea, double>(nameof(TotalDurationSeconds), 10.0);

    public static readonly StyledProperty<int> SelectedTrackIndexProperty =
        AvaloniaProperty.Register<TimelineTrackArea, int>(nameof(SelectedTrackIndex), -1);

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

    public double ScrollOffsetY
    {
        get => GetValue(ScrollOffsetYProperty);
        set => SetValue(ScrollOffsetYProperty, value);
    }

    public double PlayheadPositionSeconds
    {
        get => GetValue(PlayheadPositionSecondsProperty);
        set => SetValue(PlayheadPositionSecondsProperty, value);
    }

    public int TrackCount
    {
        get => GetValue(TrackCountProperty);
        set => SetValue(TrackCountProperty, value);
    }

    public double TrackRowHeight
    {
        get => GetValue(TrackRowHeightProperty);
        set => SetValue(TrackRowHeightProperty, value);
    }

    public double TotalDurationSeconds
    {
        get => GetValue(TotalDurationSecondsProperty);
        set => SetValue(TotalDurationSecondsProperty, value);
    }

    public int SelectedTrackIndex
    {
        get => GetValue(SelectedTrackIndexProperty);
        set => SetValue(SelectedTrackIndexProperty, value);
    }

    private static readonly AvaloniaProperty[] RenderProperties =
    {
        PixelsPerSecondProperty, ScrollOffsetXProperty, ScrollOffsetYProperty,
        PlayheadPositionSecondsProperty, TrackCountProperty, TrackRowHeightProperty,
        TotalDurationSecondsProperty, SelectedTrackIndexProperty
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
        var desiredHeight = Math.Max(TrackCount * TrackRowHeight, MinimumScrollableHeight);
        return new Size(desiredWidth, desiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = Math.Max(Math.Max(TotalDurationSeconds * PixelsPerSecond, MinimumScrollableWidth), finalSize.Width);
        var height = Math.Max(Math.Max(TrackCount * TrackRowHeight, MinimumScrollableHeight), finalSize.Height);
        return new Size(width, height);
    }

    public override void Render(DrawingContext context)
    {
        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 0 || height <= 0) return;

        // Background
        context.FillRectangle(DefaultBgBrush.ToImmutable(), new Rect(0, 0, width, height));

        var trackCount = TrackCount;
        var rowHeight = TrackRowHeight;
        if (trackCount <= 0 || rowHeight <= 0) return;

        // Selected track highlight
        var selectedIndex = SelectedTrackIndex;
        if (selectedIndex >= 0 && selectedIndex < trackCount)
        {
            var sy = selectedIndex * rowHeight - ScrollOffsetY;
            if (sy >= -rowHeight && sy <= height)
                context.FillRectangle(DefaultSelectedHighlightBrush.ToImmutable(),
                    new Rect(0, sy, width, rowHeight));
        }
        var scrollY = ScrollOffsetY;

        // Separator lines between tracks
        var linePen = new Pen(DefaultLineBrush, 1);

        var firstTrack = Math.Max(0, (int)(scrollY / rowHeight) - 1);
        var lastTrack = Math.Min(trackCount, (int)((scrollY + height) / rowHeight) + 1);

        for (var t = firstTrack; t <= lastTrack; t++)
        {
            var y = t * rowHeight - scrollY;
            if (y >= 0 && y <= height)
                context.DrawLine(linePen, new Point(0, y), new Point(width, y));
        }

        // Playhead
        var playheadX = PlayheadPositionSeconds * PixelsPerSecond - ScrollOffsetX;
        if (playheadX >= 0 && playheadX <= width)
        {
            var playheadPen = new Pen(new SolidColorBrush(DefaultAccentColor), 2);
            context.DrawLine(playheadPen, new Point(playheadX, 0), new Point(playheadX, height));
        }
    }
}
