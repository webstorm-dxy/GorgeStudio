using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace GorgeStudio.Services.EmbedService;

/// <summary>
/// <see cref="IEmbedService"/> 的默认实现。
/// 管理 <see cref="IWindowEmbedder"/> 的生命周期，负责解析 Godot 可执行文件路径，
/// 并在每次启动时创建新的平台嵌入器实例。
/// </summary>
/// <remarks>
/// 平台选择由 DI 容器注入的 <c>Func&lt;IWindowEmbedder&gt;</c> 工厂处理。
/// 在 Windows 上注入 <see cref="Interop.Win32WindowEmbedder"/> 工厂，
/// 在 macOS 上注入 <see cref="MacWindowEmbedder"/> 工厂。
/// 父窗口关闭时自动调用 <see cref="Dispose"/> 清理资源。
/// </remarks>
public sealed class EmbedService : IEmbedService, IDisposable
{
    private readonly Control _hostControl;
    private readonly Window _parentWindow;
    private readonly Func<IWindowEmbedder> _embedderFactory;
    private IWindowEmbedder? _embedder;

    /// <inheritdoc/>
    public event Action<string>? StatusChanged;

    /// <summary>
    /// 初始化嵌入服务。
    /// </summary>
    /// <param name="hostControl">
    /// 嵌入宿主控件。取自 <see cref="Views.MainWindow"/> 的 <c>EmbedHostControl</c> 属性，
    /// 是 Godot 窗口在 IDE 中的嵌入容器。
    /// </param>
    /// <param name="parentWindow">
    /// 父窗口引用，用于获取平台句柄、计算坐标偏移，以及在其关闭时自动清理嵌入资源。
    /// </param>
    /// <param name="embedderFactory">
    /// 平台嵌入器的工厂方法。由 DI 容器根据当前 OS 注入对应的实现工厂。
    /// 每次调用 <see cref="LaunchAsync"/> 时通过此工厂创建新的嵌入器实例，
    /// 以支持多次启动/停止的外部进程。
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="hostControl"/>、<paramref name="parentWindow"/> 或
    /// <paramref name="embedderFactory"/> 为 <c>null</c> 时抛出。
    /// </exception>
    public EmbedService(Control hostControl, Window parentWindow, Func<IWindowEmbedder> embedderFactory)
    {
        _hostControl = hostControl ?? throw new ArgumentNullException(nameof(hostControl));
        _parentWindow = parentWindow ?? throw new ArgumentNullException(nameof(parentWindow));
        _embedderFactory = embedderFactory ?? throw new ArgumentNullException(nameof(embedderFactory));

        // 父窗口关闭时自动清理
        _parentWindow.Closing += (_, _) => Dispose();
    }

    /// <summary>
    /// 启动 Godot 外部进程并嵌入到宿主控件区域。
    /// 每次调用会先释放之前的嵌入器实例，然后通过工厂创建新的平台嵌入器。
    /// </summary>
    /// <returns>
    /// 嵌入成功返回 <c>true</c>；否则返回 <c>false</c>。
    /// </returns>
    /// <remarks>
    /// 状态变更事件通过嵌入器的 <see cref="IWindowEmbedder.StatusChanged"/> 转发，
    /// 供 ViewModel 订阅以更新 UI 状态栏。
    /// </remarks>
    public async Task<bool> LaunchAsync()
    {
        _embedder?.Dispose();
        _embedder = _embedderFactory();
        _embedder.StatusChanged += msg => StatusChanged?.Invoke(msg);

        string exePath = ResolveExePath();
        return await _embedder.EmbedAsync(_hostControl, _parentWindow, exePath);
    }

    /// <summary>
    /// 解析 Godot 可执行文件的完整路径。
    /// 根据当前操作系统选择对应的文件名（Windows: GorgeGodotPlugin.exe，
    /// macOS/Linux: GorgeGodotPlugin），优先在输出目录查找，
    /// 若找不到则回退到源目录（开发模式）。
    /// </summary>
    /// <returns>Godot 可执行文件的完整路径。</returns>
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

    /// <summary>
    /// 根据当前操作系统返回 Godot 可执行文件的名称。
    /// </summary>
    /// <returns>
    /// Windows 返回 "GorgeGodotPlugin.exe"；
    /// macOS 返回 "GorgeGodotPlugin"；
    /// 其他平台返回 "GorgeGodotPlugin"。
    /// </returns>
    private static string GetPlatformExecutableName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "GorgeGodotPlugin.exe";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "GorgeGodotPlugin";
        return "GorgeGodotPlugin";
    }

    /// <summary>
    /// 释放嵌入服务占用的所有资源。
    /// 包括释放当前的平台嵌入器、解除事件订阅。
    /// </summary>
    /// <remarks>
    /// 父窗口关闭时自动调用。也可以手动调用来提前停止外部进程。
    /// </remarks>
    public void Dispose()
    {
        _embedder?.Dispose();
        _embedder = null;
    }
}
