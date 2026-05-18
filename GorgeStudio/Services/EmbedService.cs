using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using GorgeStudio.Interop;

namespace GorgeStudio.Services;

/// <summary>
/// IEmbedService 实现，封装 Win32WindowEmbedder 生命周期、路径解析和进程嵌入。
/// 在应用程序启动时由 App 层创建并注入到 ViewModel。
/// </summary>
public sealed class EmbedService : IEmbedService, IDisposable
{
    private readonly Control _hostControl;
    private readonly Window _parentWindow;
    private Win32WindowEmbedder? _embedder;

    public event Action<string>? StatusChanged;

    public EmbedService(Control hostControl, Window parentWindow)
    {
        _hostControl = hostControl ?? throw new ArgumentNullException(nameof(hostControl));
        _parentWindow = parentWindow ?? throw new ArgumentNullException(nameof(parentWindow));

        // 父窗口关闭时自动清理
        _parentWindow.Closing += (_, _) => Dispose();
    }

    public async Task<bool> LaunchAsync()
    {
        _embedder?.Dispose();
        _embedder = new Win32WindowEmbedder();
        _embedder.StatusChanged += msg => StatusChanged?.Invoke(msg);

        string exePath = ResolveExePath();
        return await _embedder.EmbedAsync(_hostControl, _parentWindow, exePath);
    }

    /// <summary>
    /// 计算 GorgeGodotPlugin.exe 路径：先查输出目录，再回退到源目录（开发模式）。
    /// </summary>
    private static string ResolveExePath()
    {
        string baseDir = AppContext.BaseDirectory;
        string exePath = Path.Combine(baseDir, "GodotApplication", "GorgeGodotPlugin.exe");
        if (!File.Exists(exePath))
        {
            exePath = Path.GetFullPath(Path.Combine(
                baseDir, "..", "..", "..", "GodotApplication", "GorgeGodotPlugin.exe"));
        }
        return exePath;
    }

    public void Dispose()
    {
        _embedder?.Dispose();
        _embedder = null;
    }
}
