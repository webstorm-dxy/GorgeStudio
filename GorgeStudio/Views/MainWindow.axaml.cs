using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using GorgeStudio.Interop;
using GorgeStudio.ViewModels;

namespace GorgeStudio.Views;

public partial class MainWindow : Window
{
    private Win32WindowEmbedder? _embedder;

    public MainWindow()
    {
        InitializeComponent();
        Opened += OnWindowOpened;
        Closing += OnWindowClosing;
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.EmbedAction = () => LaunchEmbedAsync(vm);
        }
    }

    private async Task<bool> LaunchEmbedAsync(MainWindowViewModel vm)
    {
        _embedder?.Dispose();
        _embedder = new Win32WindowEmbedder();
        _embedder.StatusChanged += msg => Avalonia.Threading.Dispatcher.UIThread.Post(() => vm.StatusText = msg);

        // 计算 exe 路径：先找输出目录，再回退到源目录
        string baseDir = AppContext.BaseDirectory;
        string exePath = Path.Combine(baseDir, "GodotApplication", "GorgeGodotPlugin.exe");
        if (!File.Exists(exePath))
        {
            exePath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "GodotApplication", "GorgeGodotPlugin.exe"));
        }

        return await _embedder.EmbedAsync(EmbedHost, this, exePath);
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        _embedder?.Dispose();
        _embedder = null;
    }
}
