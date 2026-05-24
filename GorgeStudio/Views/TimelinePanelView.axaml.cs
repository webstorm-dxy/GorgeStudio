using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using GorgeStudio.Controls;
using GorgeStudio.ViewModels;

namespace GorgeStudio.Views;

public partial class TimelinePanelView : UserControl
{
    private bool _isSyncing;

    // Drag state
    private const double DragThresholdPixels = 4;
    private PeriodBlockInfo? _draggingPeriod;
    private Point _dragStartPoint;
    private double _dragStartTimeOffset;
    private bool _isDraggingPeriod;

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
        if (DataContext is not TimelinePanelViewModel vm) return;

        var point = e.GetCurrentPoint(TrackAreaControl);
        var props = point.Properties;

        if (props.IsRightButtonPressed)
        {
            // Right-click: pre-select period under cursor for context menu
            var hit = TrackAreaControl.HitTestPeriod(point.Position);
            vm.SelectPeriod(hit?.Period);
            return;
        }

        // Left-click
        var hitPeriod = TrackAreaControl.HitTestPeriod(point.Position);
        if (hitPeriod != null)
        {
            // Start potential drag
            vm.SelectPeriod(hitPeriod.Period);
            _draggingPeriod = hitPeriod;
            _dragStartPoint = point.Position;
            _dragStartTimeOffset = hitPeriod.Period.TimeOffset;
            _isDraggingPeriod = false;
            e.Pointer.Capture(TrackAreaControl);
        }
        else
        {
            vm.SelectPeriod(null);
            var time = (point.Position.X + TrackAreaControl.ScrollOffsetX) / TrackAreaControl.PixelsPerSecond;
            vm.PlayheadPosition = Math.Max(0, time);
        }
    }

    private void OnTrackAreaPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggingPeriod == null || DataContext is not TimelinePanelViewModel vm) return;

        var point = e.GetCurrentPoint(TrackAreaControl);
        if (!point.Properties.IsLeftButtonPressed)
        {
            CancelDrag(vm);
            return;
        }

        var deltaX = point.Position.X - _dragStartPoint.X;

        if (!_isDraggingPeriod && Math.Abs(deltaX) >= DragThresholdPixels)
        {
            _isDraggingPeriod = true;
        }

        if (_isDraggingPeriod)
        {
            var pps = TrackAreaControl.PixelsPerSecond;
            var previewTime = Math.Max(0, _dragStartTimeOffset + deltaX / pps);
            vm.PreviewPeriodTimeOffset(_draggingPeriod.Period, previewTime);
        }
    }

    private void OnTrackAreaPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is not TimelinePanelViewModel vm) return;

        if (_isDraggingPeriod && _draggingPeriod != null)
        {
            vm.CommitPeriodTimeOffset(_draggingPeriod.Period);
        }

        ClearDragState();
    }

    private void CancelDrag(TimelinePanelViewModel vm)
    {
        if (_isDraggingPeriod)
            vm.CancelPeriodTimeOffsetPreview();
        ClearDragState();
    }

    private void ClearDragState()
    {
        _draggingPeriod = null;
        _isDraggingPeriod = false;
        // Release pointer capture if we have it
        // Pointer capture is released automatically on pointer up, but clean up state
    }
}
