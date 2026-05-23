using System;
using Avalonia.Controls;
using GorgeStudio.ViewModels;

namespace GorgeStudio.Views;

public partial class TimelinePanelView : UserControl
{
    public TimelinePanelView()
    {
        InitializeComponent();
    }

    private void ZoomIn_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is TimelinePanelViewModel vm)
            vm.ZoomLevel = Math.Min(vm.ZoomLevel * 1.5, 20.0);
    }

    private void ZoomOut_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is TimelinePanelViewModel vm)
            vm.ZoomLevel = Math.Max(vm.ZoomLevel / 1.5, 0.1);
    }
}
