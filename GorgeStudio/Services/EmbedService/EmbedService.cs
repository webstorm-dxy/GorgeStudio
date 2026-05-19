using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace GorgeStudio.Services.EmbedService;

/// <summary>
/// IEmbedService 实现，管理 IWindowEmbedder 生命周期、路径解析和进程嵌入。
/// 平台选择由 DI 注入的 IWindowEmbedder 工厂处理。
/// 在应用程序启动时由 App 层创建并注入到 ViewModel。
/// </summary>
public sealed class EmbedService : IEmbedService, IDisposable
{
    private readonly Control _hostControl;
    private readonly Window _parentWindow;
    private readonly Func<IWindowEmbedder> _embedderFactory;
    private IWindowEmbedder? _embedder;

    public event Action<string>? StatusChanged;

    public EmbedService(Control hostControl, Window parentWindow, Func<IWindowEmbedder> embedderFactory)
    {
        _hostControl = hostControl ?? throw new ArgumentNullException(nameof(hostControl));
        _parentWindow = parentWindow ?? throw new ArgumentNullException(nameof(parentWindow));
        _embedderFactory = embedderFactory ?? throw new ArgumentNullException(nameof(embedderFactory));

        // 父窗口关闭时自动清理
        _parentWindow.Closing += (_, _) => Dispose();
    }

    public async Task<bool> LaunchAsync()
    {
        _embedder?.Dispose();
        _embedder = _embedderFactory();
        _embedder.StatusChanged += msg => StatusChanged?.Invoke(msg);

        string exePath = ResolveExePath();
        return await _embedder.EmbedAsync(_hostControl, _parentWindow, exePath);
    }

    /// <summary>
    /// 计算 Godot 可执行文件路径：根据平台选择不同的文件名，先查输出目录，再回退到源目录（开发模式）。
    /// </summary>
    private static string ResolveExePath()
    {
        string baseDir = AppContext.BaseDirectory;
        string exeName = GetPlatformExecutableName();

        string exePath = Path.Combine(baseDir, "GodotApplication", exeName);
        if (!File.Exists(exePath))
        {
            exePath = Path.GetFullPath(Path.Combine(
                baseDir, "..", "..", "..", "GodotApplication", exeName));
        }
        return exePath;
    }

    private static string GetPlatformExecutableName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "GorgeGodotPlugin.exe";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "GorgeGodotPlugin";
        return "GorgeGodotPlugin";
    }

    public void Dispose()
    {
        _embedder?.Dispose();
        _embedder = null;
    }
}
