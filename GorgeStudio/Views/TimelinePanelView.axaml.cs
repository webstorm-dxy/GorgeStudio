using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using GorgeStudio.ViewModels;

namespace GorgeStudio.Views;

public partial class TimelinePanelView : UserControl
{
    private bool _isSyncing;

    public TimelinePanelView()
    {
        InitializeComponent();
    }

    private void OnTrackScrollerScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_isSyncing) return;
        _isSyncing = true;

        RulerScroller.Offset = RulerScroller.Offset.WithX(TrackScroller.Offset.X);
        TrackNameScroller.Offset = TrackNameScroller.Offset.WithY(TrackScroller.Offset.Y);

        RulerControl.ScrollOffsetX = TrackScroller.Offset.X;
        TrackAreaControl.ScrollOffsetX = TrackScroller.Offset.X;
        TrackAreaControl.ScrollOffsetY = TrackScroller.Offset.Y;

        _isSyncing = false;
    }

    private void OnTrackAreaPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(TrackAreaControl);
        var time = (point.Position.X + TrackAreaControl.ScrollOffsetX) / TrackAreaControl.PixelsPerSecond;
        if (DataContext is TimelinePanelViewModel vm)
            vm.PlayheadPosition = Math.Max(0, time);
    }
}
