using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GorgeStudio.Models.Chart;
using GorgeStudio.ViewModels;

namespace GorgeStudio.Controls;

public class TimelineTrackArea : Control
{
    private const double MinimumScrollableWidth = 2400.0;
    private const double MinimumScrollableHeight = 480.0;

    private static readonly IBrush DefaultBgBrush = new SolidColorBrush(Color.FromRgb(35, 35, 35));
    private static readonly IBrush DefaultLineBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80));
    private static readonly IBrush DefaultSelectedHighlightBrush = new SolidColorBrush(Color.FromRgb(35, 74, 108));
    private static readonly Color DefaultAccentColor = Color.FromRgb(45, 140, 235);
    private static readonly IBrush ElementPeriodBrush = new SolidColorBrush(Color.FromRgb(30, 160, 140));
    private static readonly IBrush AudioPeriodBrush = new SolidColorBrush(Color.FromRgb(130, 110, 160));
    private static readonly IBrush PeriodBorderBrush = new SolidColorBrush(Color.FromRgb(20, 120, 105));
    private static readonly IBrush AudioPeriodBorderBrush = new SolidColorBrush(Color.FromRgb(100, 80, 130));
    private static readonly IBrush PeriodLabelBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220));

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

    public static readonly StyledProperty<ObservableCollection<TrackInfo>?> TracksProperty =
        AvaloniaProperty.Register<TimelineTrackArea, ObservableCollection<TrackInfo>?>(nameof(Tracks));

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

    public ObservableCollection<TrackInfo>? Tracks
    {
        get => GetValue(TracksProperty);
        set => SetValue(TracksProperty, value);
    }

    private static readonly AvaloniaProperty[] RenderProperties =
    {
        PixelsPerSecondProperty, ScrollOffsetXProperty, ScrollOffsetYProperty,
        PlayheadPositionSecondsProperty, TrackCountProperty, TrackRowHeightProperty,
        TotalDurationSecondsProperty, SelectedTrackIndexProperty, TracksProperty
    };

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TracksProperty)
        {
            if (change.OldValue is ObservableCollection<TrackInfo> oldCol)
            {
                oldCol.CollectionChanged -= OnTracksCollectionChanged;
                UnsubscribeAllPeriods(oldCol);
            }
            if (change.NewValue is ObservableCollection<TrackInfo> newCol)
            {
                newCol.CollectionChanged += OnTracksCollectionChanged;
                SubscribeAllPeriods(newCol);
            }
        }
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

    private void OnTracksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (TrackInfo track in e.OldItems)
            {
                track.Periods.CollectionChanged -= OnPeriodsCollectionChanged;
                UnsubscribePeriodBlocks(track);
            }
        }
        if (e.NewItems != null)
        {
            foreach (TrackInfo track in e.NewItems)
            {
                track.Periods.CollectionChanged += OnPeriodsCollectionChanged;
                SubscribePeriodBlocks(track);
            }
        }
        InvalidateVisual();
    }

    private void OnPeriodsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (PeriodBlockInfo block in e.OldItems)
                block.PropertyChanged -= OnPeriodBlockPropertyChanged;
        }
        if (e.NewItems != null)
        {
            foreach (PeriodBlockInfo block in e.NewItems)
                block.PropertyChanged += OnPeriodBlockPropertyChanged;
        }
        InvalidateVisual();
    }

    private void OnPeriodBlockPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private void SubscribeAllPeriods(ObservableCollection<TrackInfo> tracks)
    {
        foreach (var track in tracks)
        {
            track.Periods.CollectionChanged += OnPeriodsCollectionChanged;
            SubscribePeriodBlocks(track);
        }
    }

    private void UnsubscribeAllPeriods(ObservableCollection<TrackInfo> tracks)
    {
        foreach (var track in tracks)
        {
            track.Periods.CollectionChanged -= OnPeriodsCollectionChanged;
            UnsubscribePeriodBlocks(track);
        }
    }

    private void SubscribePeriodBlocks(TrackInfo track)
    {
        foreach (var block in track.Periods)
            block.PropertyChanged += OnPeriodBlockPropertyChanged;
    }

    private void UnsubscribePeriodBlocks(TrackInfo track)
    {
        foreach (var block in track.Periods)
            block.PropertyChanged -= OnPeriodBlockPropertyChanged;
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

        var scrollX = ScrollOffsetX;
        var scrollY = ScrollOffsetY;
        var pps = PixelsPerSecond;

        // Selected track highlight
        var selectedIndex = SelectedTrackIndex;
        if (selectedIndex >= 0 && selectedIndex < trackCount)
        {
            var sy = selectedIndex * rowHeight - scrollY;
            if (sy >= -rowHeight && sy <= height)
                context.FillRectangle(DefaultSelectedHighlightBrush.ToImmutable(),
                    new Rect(0, sy, width, rowHeight));
        }

        // Draw period blocks
        var tracks = Tracks;
        if (tracks != null)
        {
            for (var t = 0; t < tracks.Count; t++)
            {
                var track = tracks[t];
                var trackY = t * rowHeight - scrollY;
                if (trackY + rowHeight < 0 || trackY > height) continue;

                foreach (var period in track.Periods)
                {
                    var blockX = period.StartSeconds * pps - scrollX;
                    var blockW = Math.Max(period.DurationSeconds * pps, 4);
                    if (blockX + blockW < 0 || blockX > width) continue;

                    var blockRect = new Rect(blockX, trackY + 2, blockW, rowHeight - 4);
                    var isAudio = period.IsAudio;
                    var fillBrush = (isAudio ? AudioPeriodBrush : ElementPeriodBrush).ToImmutable();
                    var borderPen = new Pen((isAudio ? AudioPeriodBorderBrush : PeriodBorderBrush).ToImmutable(), 1);

                    context.FillRectangle(fillBrush, blockRect);
                    context.DrawRectangle(borderPen, blockRect, 4);

                    // Resize handle (right 6px)
                    if (blockW > 6)
                    {
                        var handleRect = new Rect(blockX + blockW - 6, trackY + 2, 6, rowHeight - 4);
                        var handleBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
                        context.FillRectangle(handleBrush, handleRect);
                    }

                    // Label
                    if (blockW > 30)
                    {
                        var label = period.DisplayName;
                        var ft = new FormattedText(label, System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight, Typeface.Default, 11, PeriodLabelBrush);
                        var labelX = blockX + 4;
                        var labelY = trackY + (rowHeight - ft.Height) / 2;
                        if (labelX + ft.Width < width && labelX > 0)
                            context.DrawText(ft, new Point(labelX, labelY));
                    }
                }
            }
        }

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
        var playheadX = PlayheadPositionSeconds * pps - scrollX;
        if (playheadX >= 0 && playheadX <= width)
        {
            var playheadPen = new Pen(new SolidColorBrush(DefaultAccentColor), 2);
            context.DrawLine(playheadPen, new Point(playheadX, 0), new Point(playheadX, height));
        }
    }

    public PeriodBlockInfo? HitTestPeriod(Point viewportPoint)
    {
        var result = HitTestPeriodPart(viewportPoint);
        return result.Block;
    }

    public PeriodHitResult HitTestPeriodPart(Point viewportPoint)
    {
        var tracks = Tracks;
        if (tracks == null) return new PeriodHitResult(null, PeriodHitKind.None);

        var trackIndex = (int)((viewportPoint.Y + ScrollOffsetY) / TrackRowHeight);
        if (trackIndex < 0 || trackIndex >= tracks.Count) return new PeriodHitResult(null, PeriodHitKind.None);

        var timeX = (viewportPoint.X + ScrollOffsetX) / PixelsPerSecond;
        var pps = PixelsPerSecond;

        foreach (var period in tracks[trackIndex].Periods)
        {
            var start = period.StartSeconds;
            var end = start + period.DurationSeconds;
            if (timeX >= start && timeX <= end)
            {
                var blockEndVp = end * pps - ScrollOffsetX;
                var resizeHandleStartVp = blockEndVp - 6.0;
                if (viewportPoint.X >= resizeHandleStartVp)
                    return new PeriodHitResult(period, PeriodHitKind.RightResizeHandle);
                return new PeriodHitResult(period, PeriodHitKind.Body);
            }
        }

        return new PeriodHitResult(null, PeriodHitKind.None);
    }
}

public enum PeriodHitKind
{
    None,
    Body,
    RightResizeHandle
}

public record PeriodHitResult(PeriodBlockInfo? Block, PeriodHitKind Kind);
