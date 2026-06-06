using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace GorgeStudio.Services.EmbedService;

/// <summary>
/// macOS 平台的窗口"嵌入"器实现。
/// 由于 macOS 不支持原生窗口嵌入（SetParent），此实现仅将 Godot 进程
/// 以独立窗口模式启动，不对窗口进行嵌入操作。宿主控件和父窗口参数被忽略。
/// </summary>
/// <remarks>
/// 该类标记为 <see cref="SupportedOSPlatformAttribute"/>("macos")，
/// 仅在 macOS 上可用。进程使用 <c>UseShellExecute = true</c> 启动，
/// 以支持 macOS 应用程序包（.app）的正确启动。
/// </remarks>
[SupportedOSPlatform("macos")]
internal sealed class MacWindowEmbedder : IWindowEmbedder
{
    private Process? _process;
    private bool _disposed;

    /// <inheritdoc/>
    public event Action<string>? StatusChanged;

    /// <summary>
    /// 以独立窗口模式启动 Godot 可执行文件。宿主控件和父窗口参数在 macOS 上被忽略。
    /// </summary>
    /// <param name="hostControl">忽略。macOS 不支持原生窗口嵌入。</param>
    /// <param name="parentWindow">忽略。macOS 不支持原生窗口嵌入。</param>
    /// <param name="executablePath">Godot 可执行文件的完整路径。</param>
    /// <param name="workingDirectory">进程工作目录。为 <c>null</c> 时使用可执行文件所在目录。</param>
    /// <param name="timeout">忽略。独立窗口启动无需等待窗口出现。</param>
    /// <returns>
    /// 启动成功返回 <c>true</c>；文件不存在或进程启动失败返回 <c>false</c>。
    /// </returns>
    public Task<bool> EmbedAsync(
        Control hostControl,
        Window parentWindow,
        string executablePath,
        string? workingDirectory = null,
        TimeSpan? timeout = null,
        IReadOnlyList<string>? arguments = null)
    {
        // hostControl and parentWindow are ignored on macOS —
        // the process launches as a standalone window.
        _ = hostControl;
        _ = parentWindow;

        var wd = workingDirectory ?? Path.GetDirectoryName(executablePath) ?? string.Empty;

        if (!File.Exists(executablePath))
        {
            StatusChanged?.Invoke($"Error: executable not found — {executablePath}");
            return Task.FromResult(false);
        }

        StatusChanged?.Invoke("Launching Godot application (standalone mode)...");

        var psi = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = wd,
            UseShellExecute = true,
        };
        if (arguments != null)
        {
            foreach (var arg in arguments)
                psi.ArgumentList.Add(arg);
        }

        try
        {
            _process = Process.Start(psi);
            if (_process != null)
            {
                _process.EnableRaisingEvents = true;
                _process.Exited += OnProcessExited;
                StatusChanged?.Invoke("Godot application launched (standalone mode)");
                return Task.FromResult(true);
            }

            StatusChanged?.Invoke("Error: Process.Start returned null");
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke($"Error: failed to start process — {ex.Message}");
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 进程退出时的回调处理。通过 <see cref="StatusChanged"/> 事件通知 UI。
    /// </summary>
    /// <param name="sender">事件源（Process 实例）。</param>
    /// <param name="e">事件参数。</param>
    private void OnProcessExited(object? sender, EventArgs e)
    {
        StatusChanged?.Invoke("Godot application process exited");
    }

    /// <summary>
    /// 释放 macOS 嵌入器占用的资源。
    /// 尝试优雅关闭进程（CloseMainWindow + 3 秒等待），超时后强制终止。
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_process != null)
        {
            _process.Exited -= OnProcessExited;

            try
            {
                if (!_process.HasExited)
                {
                    _process.CloseMainWindow();
                    if (!_process.WaitForExit(3000))
                    {
                        _process.Kill();
                    }
                }
            }
            catch
            {
                // Best-effort cleanup
            }

            _process.Dispose();
            _process = null;
        }
    }
}
