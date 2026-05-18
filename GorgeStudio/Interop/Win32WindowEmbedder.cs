using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace GorgeStudio.Interop;

/// <summary>
/// 将外部进程的所有顶层窗口通过 Win32 SetParent 嵌入到 Avalonia 控件区域。
/// 支持多窗口进程（如 Godot 引擎先弹出 splash 再创建主窗口），自动捕获延迟出现的窗口。
/// </summary>
public sealed partial class Win32WindowEmbedder : IDisposable
{
    // ── P/Invoke ────────────────────────────────────────────────────────
    [LibraryImport("user32.dll")]
    private static partial IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    // 64-bit: GetWindowLong / SetWindowLong are macros; real entry is GetWindowLongPtrW / SetWindowLongPtrW
    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static partial nint GetWindowLong(IntPtr hWnd, int nIndex);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static partial nint SetWindowLong(IntPtr hWnd, int nIndex, nint dwNewLong);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool MoveWindow(
        IntPtr hWnd, int X, int Y, int nWidth, int nHeight, [MarshalAs(UnmanagedType.Bool)] bool bRepaint);

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindowVisible(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindow(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    // ── Win32 constants ─────────────────────────────────────────────────
    private const int GwlStyle = -16;
    private const int GwlExstyle = -20;

    private const int WsCaption     = 0x00C00000;
    private const int WsThickframe  = 0x00040000;
    private const int WsSysmenu     = 0x00080000;
    private const int WsMinimizebox = 0x00020000;
    private const int WsMaximizebox = 0x00010000;
    private const int WsChild       = 0x40000000;
    private const int WsVisible     = 0x10000000;

    private const int WsExClientedge = 0x00000200;
    private const int WsExStaticedge = 0x00020000;
    private const int WsExDlgmodalframe = 0x00000001;

    private static readonly IntPtr HwndTop = IntPtr.Zero;
    private const uint SwpNozorder      = 0x0004;
    private const uint SwpNoactivate    = 0x0010;
    private const uint SwpFramechanged  = 0x0020;
    private const uint SwpShowwindow    = 0x0040;

    private const int SwHide = 0;
    private const int SwShow = 5;

    private const uint GwOwner = 4;

    // ── Native structs ──────────────────────────────────────────────────
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    // ── State ───────────────────────────────────────────────────────────
    private Process? _process;
    private readonly HashSet<IntPtr> _childWindows = [];
    private IntPtr _primaryHwnd = IntPtr.Zero; // 面积最大的窗口
    private Control? _hostControl;
    private Window? _parentWindow;
    private Timer? _windowMonitor;
    private bool _disposed;
    private readonly object _lock = new();

    // ── Events ──────────────────────────────────────────────────────────
    /// <summary>状态变化时触发，可用于更新 UI 状态栏。</summary>
    public event Action<string>? StatusChanged;

    /// <summary>嵌入完成（含失败）时触发。</summary>
    public event Action<bool>? EmbedCompleted;

    // ── Public API ──────────────────────────────────────────────────────

    /// <summary>
    /// 启动外部 exe 并将其所有窗口嵌入到指定控件区域。
    /// </summary>
    public async Task<bool> EmbedAsync(
        Control hostControl,
        Window parentWindow,
        string executablePath,
        string? workingDirectory = null,
        TimeSpan? timeout = null)
    {
        _hostControl = hostControl ?? throw new ArgumentNullException(nameof(hostControl));
        _parentWindow = parentWindow ?? throw new ArgumentNullException(nameof(parentWindow));

        var timeoutVal = timeout ?? TimeSpan.FromSeconds(30);
        var wd = workingDirectory ?? Path.GetDirectoryName(executablePath) ?? string.Empty;

        if (!File.Exists(executablePath))
        {
            ReportStatus($"错误：找不到文件 {executablePath}");
            EmbedCompleted?.Invoke(false);
            return false;
        }

        // 1. 启动外部进程
        ReportStatus("正在启动 Godot 应用...");
        var psi = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = wd,
            UseShellExecute = false,
            CreateNoWindow = false,
        };

        try
        {
            _process = Process.Start(psi);
        }
        catch (Exception ex)
        {
            ReportStatus($"错误：启动进程失败 — {ex.Message}");
            EmbedCompleted?.Invoke(false);
            return false;
        }

        if (_process == null)
        {
            ReportStatus("错误：Process.Start 返回 null");
            EmbedCompleted?.Invoke(false);
            return false;
        }

        // 2. 分阶段收集：等首批窗口出现 → 嵌入 → 延迟 1.5s 再收集（抓 splash 之后的主窗口）
        ReportStatus("等待应用窗口创建...");
        var firstBatch = await CollectProcessWindowsAsync(_process, timeoutVal);
        if (firstBatch.Count == 0)
        {
            ReportStatus("错误：等待应用窗口超时");
            EmbedCompleted?.Invoke(false);
            return false;
        }

        // 3. 嵌入首批窗口
        ReportStatus($"正在嵌入 {firstBatch.Count} 个窗口...");
        EmbedWindows(firstBatch);

        // 4. 首次定位
        UpdateAllWindowPositions();

        // 5. 等待 Godot splash 关闭、主窗口出现，再次收集
        ReportStatus("等待主窗口就绪...");
        await Task.Delay(1500);
        var secondBatch = await CollectProcessWindowsAsync(_process, TimeSpan.FromSeconds(3));
        if (secondBatch.Count > 0)
        {
            EmbedWindows(secondBatch);
            UpdateAllWindowPositions();
        }

        ReportStatus($"嵌入完成（共 {_childWindows.Count} 个窗口）");

        // 6. 订阅宿主尺寸变化
        _hostControl.PropertyChanged += OnHostControlPropertyChanged;

        // 7. 启动后台轮询，捕获后续可能出现的窗口
        StartWindowMonitor();

        EmbedCompleted?.Invoke(true);
        return true;
    }

    // ── Window collection ───────────────────────────────────────────────

    /// <summary>
    /// 收集属于目标进程的所有可见顶层窗口。返回尚未嵌入的新窗口列表。
    /// </summary>
    private static async Task<List<IntPtr>> CollectProcessWindowsAsync(Process process, TimeSpan timeout)
    {
        var result = new List<IntPtr>();
        using var cts = new CancellationTokenSource(timeout);
        var token = cts.Token;

        // 先确保进程消息循环就绪
        try { await Task.Run(() => process.WaitForInputIdle(), token); }
        catch (OperationCanceledException) { }
        catch (InvalidOperationException) { }

        uint targetPid = (uint)process.Id;

        while (!token.IsCancellationRequested)
        {
            process.Refresh();
            var found = new List<IntPtr>();
            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out uint pid);
                if (pid == targetPid && IsWindowVisible(hWnd) && IsCandidateWindow(hWnd))
                {
                    // 立即隐藏，防止短暂闪现为独立顶层窗口
                    ShowWindow(hWnd, SwHide);
                    found.Add(hWnd);
                }
                return true;
            }, IntPtr.Zero);

            if (found.Count > 0)
            {
                result = found;
                break;
            }

            try { await Task.Delay(200, token); }
            catch (OperationCanceledException) { break; }
        }

        return result;
    }

    private static bool IsCandidateWindow(IntPtr hWnd)
    {
        // 可见、还存在、有标题（或即使无标题也算，因为 splash 可能没标题）
        return IsWindow(hWnd) && IsWindowVisible(hWnd);
    }

    // ── Embedding ───────────────────────────────────────────────────────

    private void EmbedWindows(List<IntPtr> batch)
    {
        lock (_lock)
        {
            foreach (var hWnd in batch)
            {
                if (_childWindows.Contains(hWnd)) continue; // 已嵌入
                if (!IsWindow(hWnd)) continue;               // 窗口已销毁

                EmbedSingleWindow(hWnd);
                _childWindows.Add(hWnd);

                // 选面积最大的作为主窗口（用于定位）
                if (_primaryHwnd == IntPtr.Zero || GetWindowArea(hWnd) > GetWindowArea(_primaryHwnd))
                {
                    _primaryHwnd = hWnd;
                }
            }
        }
    }

    private void EmbedSingleWindow(IntPtr childHwnd)
    {
        IntPtr parentHwnd = GetParentHwnd();
        if (parentHwnd == IntPtr.Zero) return;

        SetParent(childHwnd, parentHwnd);

        int style = (int)GetWindowLong(childHwnd, GwlStyle);
        style &= ~(WsCaption | WsThickframe | WsSysmenu | WsMinimizebox | WsMaximizebox);
        style |= WsChild; // 不设 WS_VISIBLE，由 UpdateAllWindowPositions 统一控制显隐
        SetWindowLong(childHwnd, GwlStyle, style);

        int exStyle = (int)GetWindowLong(childHwnd, GwlExstyle);
        exStyle &= ~(WsExClientedge | WsExStaticedge | WsExDlgmodalframe);
        SetWindowLong(childHwnd, GwlExstyle, exStyle);

        SetWindowPos(childHwnd, HwndTop,
            0, 0, 0, 0,
            SwpNozorder | SwpNoactivate | SwpFramechanged);
    }

    private static int GetWindowArea(IntPtr hWnd)
    {
        if (!GetWindowRect(hWnd, out Rect rect)) return 0;
        return rect.Width * rect.Height;
    }

    private void UpdateAllWindowPositions()
    {
        if (_hostControl == null || _parentWindow == null) return;

        var hostScreenPos = _hostControl.PointToScreen(new Avalonia.Point(0, 0));
        var windowScreenPos = _parentWindow.PointToScreen(new Avalonia.Point(0, 0));
        var hostSize = _hostControl.Bounds.Size;
        double scaling = _parentWindow.RenderScaling;

        int x = (int)((hostScreenPos.X - windowScreenPos.X) * scaling);
        int y = (int)((hostScreenPos.Y - windowScreenPos.Y) * scaling);
        int w = Math.Max(1, (int)(hostSize.Width * scaling));
        int h = Math.Max(1, (int)(hostSize.Height * scaling));

        lock (_lock)
        {
            foreach (var hWnd in _childWindows.ToList())
            {
                if (!IsWindow(hWnd))
                {
                    _childWindows.Remove(hWnd);
                    if (hWnd == _primaryHwnd) _primaryHwnd = _childWindows.FirstOrDefault();
                    continue;
                }

                // 主窗口填满宿主区域；非主窗口隐藏（如 splash 通常会自动消亡）
                if (hWnd == _primaryHwnd || _primaryHwnd == IntPtr.Zero)
                {
                    MoveWindow(hWnd, x, y, w, h, true);
                    ShowWindow(hWnd, SwShow);
                }
                else
                {
                    ShowWindow(hWnd, SwHide);
                }
            }
        }
    }

    private IntPtr GetParentHwnd()
    {
        if (_parentWindow == null) return IntPtr.Zero;
        var handle = _parentWindow.TryGetPlatformHandle();
        return handle?.Handle ?? IntPtr.Zero;
    }

    // ── Background window monitor ───────────────────────────────────────

    private void StartWindowMonitor()
    {
        _windowMonitor?.Dispose();
        _windowMonitor = new Timer(_ => ScanForNewWindows(), null,
            TimeSpan.FromSeconds(2),   // 首次延迟
            TimeSpan.FromSeconds(1));  // 每秒扫描
    }

    private void ScanForNewWindows()
    {
        if (_disposed || _process == null || _process.HasExited) return;

        try
        {
            uint targetPid = (uint)_process.Id;
            var newWindows = new List<IntPtr>();

            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out uint pid);
                if (pid == targetPid && IsWindowVisible(hWnd) && IsWindow(hWnd))
                {
                    lock (_lock)
                    {
                        if (!_childWindows.Contains(hWnd))
                            newWindows.Add(hWnd);
                    }
                }
                return true;
            }, IntPtr.Zero);

            if (newWindows.Count > 0)
            {
                EmbedWindows(newWindows);
                Dispatcher.UIThread.Post(UpdateAllWindowPositions);
            }
        }
        catch
        {
            // 后台扫描不应崩溃
        }
    }

    // ── Layout sync ────────────────────────────────────────────────────

    private void OnHostControlPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Visual.BoundsProperty)
        {
            Dispatcher.UIThread.Post(UpdateAllWindowPositions);
        }
    }

    // ── Status reporting ────────────────────────────────────────────────

    private void ReportStatus(string message)
    {
        Dispatcher.UIThread.Post(() => StatusChanged?.Invoke(message));
    }

    // ── IDisposable ─────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _windowMonitor?.Dispose();
        _windowMonitor = null;

        if (_hostControl != null)
        {
            _hostControl.PropertyChanged -= OnHostControlPropertyChanged;
        }

        lock (_lock)
        {
            foreach (var hWnd in _childWindows)
            {
                if (IsWindow(hWnd))
                {
                    SetParent(hWnd, IntPtr.Zero);
                }
            }
            _childWindows.Clear();
        }

        try
        {
            if (_process != null && !_process.HasExited)
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
            // ignored
        }

        _process?.Dispose();
        _process = null;
    }
}