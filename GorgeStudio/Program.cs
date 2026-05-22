using Avalonia;
using System;

namespace GorgeStudio;

/// <summary>
/// 应用程序入口点。负责引导 Avalonia UI 框架，初始化平台检测、字体和日志。
/// 所有 Avalonia 相关的初始化必须在 <see cref="BuildAvaloniaApp"/> 返回的
/// <see cref="AppBuilder"/> 中完成，不应在 Main 方法中直接使用 Avalonia API。
/// </summary>
sealed class Program
{
    /// <summary>
    /// 应用程序的主入口点。使用经典的桌面应用程序生命周期启动 Avalonia。
    /// </summary>
    /// <param name="args">命令行参数，传递给 Avalonia 框架处理。</param>
    /// <remarks>
    /// 此方法标记为 <see cref="STAThreadAttribute"/>，因为 Avalonia 在 Windows 上
    /// 需要单线程单元（STA）模式。在调用 <c>AppMain</c> 之前不要使用任何 Avalonia
    /// 或第三方 UI API，也不要依赖 SynchronizationContext。
    /// </remarks>
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// 构建并配置 Avalonia 应用程序构建器。
    /// 注册平台检测、Inter 字体和 Trace 级别日志记录。
    /// </summary>
    /// <returns>配置完成的 <see cref="AppBuilder"/> 实例，可用于启动应用程序。</returns>
    /// <remarks>
    /// 此方法也供 Avalonia 可视化设计器使用，因此不可删除。
    /// </remarks>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
