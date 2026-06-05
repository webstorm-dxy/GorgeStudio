using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace GorgeStudio.Services.EmbedService;

/// <summary>
/// 抽象操作系统级别的窗口嵌入操作。
/// Windows 实现使用 Win32 <c>SetParent</c> 将外部进程窗口嵌入到宿主控件中；
/// macOS 实现将进程作为独立窗口启动（宿主控件参数被忽略）。
/// </summary>
/// <remarks>
/// 此接口通过 DI 工厂 <c>Func&lt;IWindowEmbedder&gt;</c> 注入，
/// <see cref="EmbedService"/> 在每次 <c>LaunchAsync</c> 时创建新实例以支持重复启动。
/// </remarks>
public interface IWindowEmbedder : IDisposable
{
    /// <summary>
    /// 启动外部可执行文件，并根据平台实现将其窗口嵌入或作为独立窗口运行。
    /// </summary>
    /// <param name="hostControl">
    /// 嵌入目标控件（Windows）。外部进程的窗口将被定位到此控件的边界内。
    /// 在 macOS 上被忽略。
    /// </param>
    /// <param name="parentWindow">
    /// 父窗口引用，用于获取平台句柄和计算坐标偏移。
    /// 在 macOS 上被忽略。
    /// </param>
    /// <param name="executablePath">外部可执行文件的完整路径。</param>
    /// <param name="workingDirectory">
    /// 进程的工作目录。为 <c>null</c> 时使用可执行文件所在目录。
    /// </param>
    /// <param name="timeout">
    /// 等待窗口出现的超时时间。为 <c>null</c> 时使用默认值 30 秒。
    /// </param>
    /// <param name="arguments">
    /// 传递给外部可执行文件的命令行参数。为 <c>null</c> 时不传递参数。
    /// 使用 <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList"/> 安全传递，防止注入。
    /// </param>
    /// <returns>
    /// 操作结果：成功返回 <c>true</c>；文件不存在、进程启动失败或窗口超时则返回 <c>false</c>。
    /// </returns>
    Task<bool> EmbedAsync(
        Control hostControl,
        Window parentWindow,
        string executablePath,
        string? workingDirectory = null,
        TimeSpan? timeout = null,
        IReadOnlyList<string>? arguments = null);

    /// <summary>
    /// 状态消息事件，用于向 UI 反馈嵌入进度。
    /// 消息包括 "正在启动..."、"嵌入完成" 以及各类错误提示。
    /// </summary>
    event Action<string>? StatusChanged;
}
