using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using GorgeStudio.Controls;
using GorgeStudio.ViewModels;

namespace GorgeStudio.Views;

public enum PeriodDragMode
{
    None,
    Move,
    ResizeMinLength
}

public partial class TimelinePanelView : UserControl
{
    private bool _isSyncing;

    // Drag state
    private const double DragThresholdPixels = 4;
    private PeriodDragMode _dragMode;
    private PeriodBlockInfo? _draggingPeriod;
    private Point _dragStartPoint;
    private double _dragStartTimeOffset;
    private double _dragStartMinLength;
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
        var hitResult = TrackAreaControl.HitTestPeriodPart(point.Position);
        if (hitResult is { Kind: not PeriodHitKind.None, Block: not null })
        {
            vm.SelectPeriod(hitResult.Block.Period);
            _draggingPeriod = hitResult.Block;
            _dragStartPoint = point.Position;
            _dragStartTimeOffset = hitResult.Block.Period.TimeOffset;
            _dragStartMinLength = hitResult.Block.Period.MinLength;
            _dragMode = hitResult.Kind == PeriodHitKind.RightResizeHandle
                ? PeriodDragMode.ResizeMinLength
                : PeriodDragMode.Move;
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
        if (DataContext is not TimelinePanelViewModel vm) return;

        var point = e.GetCurrentPoint(TrackAreaControl);

        // Cursor feedback when not dragging
        if (_draggingPeriod == null)
        {
            var hoverHit = TrackAreaControl.HitTestPeriodPart(point.Position);
            TrackAreaControl.Cursor = hoverHit.Kind == PeriodHitKind.RightResizeHandle
                ? new Cursor(StandardCursorType.SizeWestEast)
                : Cursor.Default;
            return;
        }

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
            var deltaSeconds = deltaX / pps;

            if (_dragMode == PeriodDragMode.ResizeMinLength)
            {
                var previewMinLength = Math.Max(0.25, _dragStartMinLength + deltaSeconds);
                vm.PreviewPeriodMinLength(_draggingPeriod.Period, previewMinLength);
            }
            else
            {
                var previewTime = Math.Max(0, _dragStartTimeOffset + deltaSeconds);
                vm.PreviewPeriodTimeOffset(_draggingPeriod.Period, previewTime);
            }
        }
    }

    private void OnTrackAreaPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is not TimelinePanelViewModel vm) return;

        if (_isDraggingPeriod && _draggingPeriod != null)
        {
            if (_dragMode == PeriodDragMode.ResizeMinLength)
                vm.CommitPeriodMinLength(_draggingPeriod.Period);
            else
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
        _dragMode = PeriodDragMode.None;
        TrackAreaControl.Cursor = Cursor.Default;
    }
}
