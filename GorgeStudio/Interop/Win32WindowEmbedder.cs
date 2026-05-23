using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using GorgeStudio.Services;
using GorgeStudio.Services.EmbedService;

namespace GorgeStudio.Interop;

/// <summary>
/// 将外部进程的所有顶层窗口通过 Win32 <c>SetParent</c> 嵌入到 Avalonia 控件区域。
/// 支持多窗口进程（如 Godot 引擎先弹出 splash 窗口再创建主窗口），
/// 通过分阶段窗口收集和后台轮询自动捕获延迟出现的窗口。
/// </summary>
/// <remarks>
/// 嵌入流程：
/// <list type="number">
/// <item>启动外部进程（<see cref="Process.Start"/>）</item>
/// <item>收集首批出现的可见顶层窗口并嵌入</item>
/// <item>等待 1.5 秒（Godot splash 关闭、主窗口出现）后再次收集</item>
/// <item>将最大的窗口作为主窗口，填充宿主区域；其他窗口隐藏</item>
/// <item>启动后台定时器，每秒扫描新出现的窗口</item>
/// <item>宿主区域尺寸变化时重新定位所有嵌入窗口</item>
/// </list>
/// 所有 P/Invoke 调用使用 <see cref="LibraryImportAttribute"/>（编译时源生成），
/// 64 位兼容（使用 GetWindowLongPtrW/SetWindowLongPtrW 入口点）。
/// </remarks>
[SupportedOSPlatform("windows7.0")]
internal sealed partial class Win32WindowEmbedder : IWindowEmbedder
{
    // ── P/Invoke ────────────────────────────────────────────────────────

    /// <summary>
    /// 将子窗口的父窗口更改为指定的新父窗口。这是窗口嵌入的核心 API。
    /// </summary>
    /// <param name="hWndChild">子窗口句柄。</param>
    /// <param name="hWndNewParent">新父窗口句柄。传 <see cref="IntPtr.Zero"/> 解除嵌入。</param>
    /// <returns>之前父窗口的句柄。</returns>
    [LibraryImport("user32.dll")]
    private static partial IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    /// <summary>
    /// 获取窗口的样式信息。64 位系统上使用 GetWindowLongPtrW 入口点。
    /// </summary>
    /// <param name="hWnd">窗口句柄。</param>
    /// <param name="nIndex">要获取的信息索引。GWL_STYLE (-16) 获取窗口样式；GWL_EXSTYLE (-20) 获取扩展样式。</param>
    /// <returns>指定索引的信息值。</returns>
    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static partial nint GetWindowLong(IntPtr hWnd, int nIndex);

    /// <summary>
    /// 设置窗口的样式信息。64 位系统上使用 SetWindowLongPtrW 入口点。
    /// </summary>
    /// <param name="hWnd">窗口句柄。</param>
    /// <param name="nIndex">要设置的信息索引。</param>
    /// <param name="dwNewLong">新的样式值。</param>
    /// <returns>之前的样式值。</returns>
    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static partial nint SetWindowLong(IntPtr hWnd, int nIndex, nint dwNewLong);

    /// <summary>
    /// 改变窗口的位置、大小和 Z 序。
    /// </summary>
    /// <param name="hWnd">窗口句柄。</param>
    /// <param name="hWndInsertAfter">Z 序插入位置。使用 HWND_TOP (IntPtr.Zero) 置于顶层。</param>
    /// <param name="X">新位置的 X 坐标。</param>
    /// <param name="Y">新位置的 Y 坐标。</param>
    /// <param name="cx">新宽度。</param>
    /// <param name="cy">新高度。</param>
    /// <param name="uFlags">大小和位置修改标志组合。</param>
    /// <returns>成功返回非零值。</returns>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    /// <summary>
    /// 移动窗口到新的位置并调整大小。
    /// </summary>
    /// <param name="hWnd">窗口句柄。</param>
    /// <param name="X">新左边缘位置。</param>
    /// <param name="Y">新顶边缘位置。</param>
    /// <param name="nWidth">新宽度。</param>
    /// <param name="nHeight">新高度。</param>
    /// <param name="bRepaint">是否重绘窗口。</param>
    /// <returns>成功返回非零值。</returns>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool MoveWindow(
        IntPtr hWnd, int X, int Y, int nWidth, int nHeight, [MarshalAs(UnmanagedType.Bool)] bool bRepaint);

    /// <summary>
    /// 获取创建指定窗口的线程 ID 和进程 ID。
    /// </summary>
    /// <param name="hWnd">窗口句柄。</param>
    /// <param name="lpdwProcessId">输出参数，接收进程 ID。</param>
    /// <returns>创建窗口的线程 ID。</returns>
    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    /// <summary>
    /// 判断窗口是否可见。
    /// </summary>
    /// <param name="hWnd">窗口句柄。</param>
    /// <returns>窗口可见返回 <c>true</c>。</returns>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindowVisible(IntPtr hWnd);

    /// <summary>
    /// 判断窗口句柄是否仍然有效。
    /// </summary>
    /// <param name="hWnd">窗口句柄。</param>
    /// <returns>句柄有效返回 <c>true</c>。</returns>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindow(IntPtr hWnd);

    /// <summary>
    /// 枚举桌面上的所有顶层窗口，对每个窗口调用指定的回调函数。
    /// </summary>
    /// <param name="lpEnumFunc">回调函数委托。</param>
    /// <param name="lParam">传递给回调函数的用户定义值。</param>
    /// <returns>枚举成功返回非零值。</returns>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    /// <summary>
    /// 设置窗口的显示状态（显示、隐藏等）。
    /// </summary>
    /// <param name="hWnd">窗口句柄。</param>
    /// <param name="nCmdShow">显示命令。SW_HIDE (0) 隐藏，SW_SHOW (5) 显示。</param>
    /// <returns>窗口之前可见返回非零值。</returns>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    /// <summary>
    /// 获取窗口的边界矩形（屏幕坐标）。
    /// </summary>
    /// <param name="hWnd">窗口句柄。</param>
    /// <param name="lpRect">输出参数，接收矩形的 Left, Top, Right, Bottom 坐标。</param>
    /// <returns>成功返回非零值。</returns>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    /// <summary>
    /// 获取与指定窗口有指定关系的窗口句柄。
    /// </summary>
    /// <param name="hWnd">窗口句柄。</param>
    /// <param name="uCmd">关系类型。GW_OWNER (4) 获取所有者窗口。</param>
    /// <returns>相关窗口的句柄。</returns>
    [LibraryImport("user32.dll")]
    private static partial IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    /// <summary>
    /// <see cref="EnumWindows"/> 使用的回调函数委托。
    /// </summary>
    /// <param name="hWnd">当前枚举到的窗口句柄。</param>
    /// <param name="lParam">用户定义的参数。</param>
    /// <returns>返回 <c>true</c> 继续枚举，返回 <c>false</c> 停止。</returns>
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    // ── Win32 constants ─────────────────────────────────────────────────

    /// <summary>获取/设置窗口样式的索引值。</summary>
    private const int GwlStyle = -16;
    /// <summary>获取/设置窗口扩展样式的索引值。</summary>
    private const int GwlExstyle = -20;

    // 窗口样式位标志 — 嵌入时需要移除的样式
    private const int WsCaption     = 0x00C00000; // 标题栏
    private const int WsThickframe  = 0x00040000; // 可调整大小的边框
    private const int WsSysmenu     = 0x00080000; // 系统菜单
    private const int WsMinimizebox = 0x00020000; // 最小化按钮
    private const int WsMaximizebox = 0x00010000; // 最大化按钮
    private const int WsChild       = 0x40000000; // 子窗口样式（嵌入时添加）
    private const int WsVisible     = 0x10000000; // 窗口可见

    // 扩展样式位标志 — 嵌入时需要移除的样式
    private const int WsExClientedge = 0x00000200; // 3D 凹陷边框
    private const int WsExStaticedge = 0x00020000; // 3D 静态边框
    private const int WsExDlgmodalframe = 0x00000001; // 对话框模态边框

    private static readonly IntPtr HwndTop = IntPtr.Zero;
    private const uint SwpNozorder      = 0x0004; // 不改变 Z 序
    private const uint SwpNoactivate    = 0x0010; // 不激活窗口
    private const uint SwpFramechanged  = 0x0020; // 重新计算客户区
    private const uint SwpShowwindow    = 0x0040; // 显示窗口

    private const int SwHide = 0;
    private const int SwShow = 5;

    private const uint GwOwner = 4;

    // ── Native structs ──────────────────────────────────────────────────

    /// <summary>
    /// Win32 RECT 结构体，表示屏幕坐标系中的一个矩形。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    /// <summary>
    /// Win32 POINT 结构体，表示屏幕坐标系中的一个点。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    // ── State ───────────────────────────────────────────────────────────

    private Process? _process;
    /// <summary>已嵌入的子窗口句柄集合，用于去重和清理。</summary>
    private readonly HashSet<IntPtr> _childWindows = [];
    /// <summary>面积最大的窗口句柄，作为主窗口填充宿主区域。</summary>
    private IntPtr _primaryHwnd = IntPtr.Zero;
    private Control? _hostControl;
    private Window? _parentWindow;
    /// <summary>后台窗口监控定时器，每秒扫描新出现的窗口。</summary>
    private Timer? _windowMonitor;
    private bool _disposed;
    private readonly object _lock = new();

    // ── Events ──────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public event Action<string>? StatusChanged;

    // ── Public API ──────────────────────────────────────────────────────

    /// <summary>
    /// 启动外部可执行文件并将其所有窗口嵌入到指定的 Avalonia 宿主控件区域。
    /// </summary>
    /// <param name="hostControl">
    /// 嵌入目标控件。外部进程的主窗口将被调整大小以填充此控件的边界。
    /// </param>
    /// <param name="parentWindow">
    /// 父窗口引用。用于获取平台窗口句柄和计算渲染缩放比例。
    /// </param>
    /// <param name="executablePath">外部可执行文件的完整路径。必须存在。</param>
    /// <param name="workingDirectory">
    /// 进程的工作目录。为 <c>null</c> 时使用可执行文件所在目录。
    /// </param>
    /// <param name="timeout">
    /// 等待首个窗口出现的超时时间。为 <c>null</c> 时使用默认值 30 秒。
    /// </param>
    /// <returns>
    /// 嵌入成功返回 <c>true</c>；可执行文件不存在、进程启动失败或窗口出现超时返回 <c>false</c>。
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="hostControl"/> 或 <paramref name="parentWindow"/> 为 <c>null</c> 时抛出。
    /// </exception>
    /// <remarks>
    /// 嵌入流程分为以下阶段：
    /// <list type="number">
    /// <item><b>进程启动</b> — 使用 <c>UseShellExecute = false</c> 启动外部 exe</item>
    /// <item><b>首批窗口收集</b> — 等待可见顶层窗口出现，最多等待 <paramref name="timeout"/></item>
    /// <item><b>嵌入</b> — 调用 <see cref="SetParent"/> 将窗口绑定到宿主，修改样式去除标题栏和边框</item>
    /// <item><b>首次定位</b> — 将主窗口定位并缩放到填满宿主区域</item>
    /// <item><b>第二批窗口收集</b> — 等待 1.5 秒后再次扫描（捕获 splash 关闭后的主窗口）</item>
    /// <item><b>后台上报</b> — 启动每秒轮询的定时器，捕获后续出现的窗口</item>
    /// </list>
    /// </remarks>
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
            return false;
        }

        if (_process == null)
        {
            ReportStatus("错误：Process.Start 返回 null");
            return false;
        }

        // 2. 分阶段收集：等首批窗口出现 → 嵌入 → 延迟 1.5s 再收集（抓 splash 之后的主窗口）
        ReportStatus("等待应用窗口创建...");
        var firstBatch = await CollectProcessWindowsAsync(_process, timeoutVal);
        if (firstBatch.Count == 0)
        {
            ReportStatus("错误：等待应用窗口超时");
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

        return true;
    }

    // ── Window collection ───────────────────────────────────────────────

    /// <summary>
    /// 异步收集属于目标进程的所有可见顶层窗口。
    /// 先等待进程消息循环就绪（<see cref="Process.WaitForInputIdle"/>），
    /// 然后轮询枚举窗口直到发现窗口或超时。
    /// </summary>
    /// <param name="process">目标进程。</param>
    /// <param name="timeout">最大等待时间。</param>
    /// <returns>找到的窗口句柄列表。超时时返回空列表。</returns>
    /// <remarks>
    /// 找到的窗口会被立即隐藏（<c>SW_HIDE</c>）以避免短暂闪现为独立顶层窗口，
    /// 随后再统一嵌入并选择性显示。
    /// </remarks>
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

    /// <summary>
    /// 判断窗口是否为有效的候选窗口（可见且句柄有效）。
    /// </summary>
    /// <param name="hWnd">窗口句柄。</param>
    /// <returns>窗口可见且句柄有效时返回 <c>true</c>。</returns>
    private static bool IsCandidateWindow(IntPtr hWnd)
    {
        return IsWindow(hWnd) && IsWindowVisible(hWnd);
    }

    // ── Embedding ───────────────────────────────────────────────────────

    /// <summary>
    /// 批量嵌入一组窗口。跳过已嵌入的窗口和已销毁的窗口。
    /// 自动选择面积最大的窗口作为主窗口。
    /// </summary>
    /// <param name="batch">待嵌入的窗口句柄列表。</param>
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

    /// <summary>
    /// 对单个窗口执行嵌入操作。
    /// 调用 <see cref="SetParent"/> 将窗口绑定到宿主，去除标题栏、边框和系统菜单等
    /// 顶层窗口样式，添加 <c>WS_CHILD</c> 样式使其成为子窗口。
    /// </summary>
    /// <param name="childHwnd">待嵌入的窗口句柄。</param>
    /// <remarks>
    /// 嵌入后窗口默认不显示（不设置 <c>WS_VISIBLE</c>），
    /// 由 <see cref="UpdateAllWindowPositions"/> 统一控制主窗口显示和非主窗口隐藏。
    /// </remarks>
    private void EmbedSingleWindow(IntPtr childHwnd)
    {
        IntPtr parentHwnd = GetParentHwnd();
        if (parentHwnd == IntPtr.Zero) return;

        SetParent(childHwnd, parentHwnd);

        int style = (int)GetWindowLong(childHwnd, GwlStyle);
        style &= ~(WsCaption | WsThickframe | WsSysmenu | WsMinimizebox | WsMaximizebox);
        style |= WsChild;
        SetWindowLong(childHwnd, GwlStyle, style);

        int exStyle = (int)GetWindowLong(childHwnd, GwlExstyle);
        exStyle &= ~(WsExClientedge | WsExStaticedge | WsExDlgmodalframe);
        SetWindowLong(childHwnd, GwlExstyle, exStyle);

        SetWindowPos(childHwnd, HwndTop,
            0, 0, 0, 0,
            SwpNozorder | SwpNoactivate | SwpFramechanged);
    }

    /// <summary>
    /// 计算窗口的屏幕坐标面积（宽度 × 高度）。
    /// </summary>
    /// <param name="hWnd">窗口句柄。</param>
    /// <returns>窗口面积。如果获取矩形失败，返回 0。</returns>
    private static int GetWindowArea(IntPtr hWnd)
    {
        if (!GetWindowRect(hWnd, out Rect rect)) return 0;
        return rect.Width * rect.Height;
    }

    /// <summary>
    /// 根据宿主控件的当前尺寸和屏幕位置，更新所有嵌入窗口的位置。
    /// 主窗口被缩放以填满宿主区域；非主窗口被隐藏。
    /// </summary>
    /// <remarks>
    /// 该方法通过 <see cref="PointToScreen"/> 计算宿主控件在屏幕上的实际位置，
    /// 并考虑 <see cref="Window.RenderScaling"/>（DPI 缩放），
    /// 确保嵌入窗口在屏幕上的像素位置与宿主控件的视觉位置一致。
    /// 已销毁的窗口会被自动从集合中移除。
    /// </remarks>
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

    /// <summary>
    /// 获取 Avalonia 父窗口的 Win32 平台窗口句柄。
    /// </summary>
    /// <returns>父窗口的 HWND；如果获取失败返回 <see cref="IntPtr.Zero"/>。</returns>
    private IntPtr GetParentHwnd()
    {
        if (_parentWindow == null) return IntPtr.Zero;
        var handle = _parentWindow.TryGetPlatformHandle();
        return handle?.Handle ?? IntPtr.Zero;
    }

    // ── Background window monitor ───────────────────────────────────────

    /// <summary>
    /// 启动后台窗口监控定时器。首次延迟 2 秒后，每 1 秒扫描一次新出现的窗口。
    /// 如果已有定时器运行，会先释放旧的再创建新的。
    /// </summary>
    private void StartWindowMonitor()
    {
        _windowMonitor?.Dispose();
        _windowMonitor = new Timer(_ => ScanForNewWindows(), null,
            TimeSpan.FromSeconds(2),   // 首次延迟
            TimeSpan.FromSeconds(1));  // 每秒扫描
    }

    /// <summary>
    /// 扫描目标进程的新增可见顶层窗口，自动嵌入新发现的窗口。
    /// </summary>
    /// <remarks>
    /// 此方法由后台定时器调用。通过比较进程 ID 和已嵌入窗口集合进行去重，
    /// 新发现的窗口在 UI 线程上进行定位更新。
    /// 内部异常被静默捕获，避免后台扫描崩溃影响主程序。
    /// </remarks>
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

    /// <summary>
    /// 宿主控件的属性变更回调。当宿主控件的边界（Bounds）发生变化时，
    /// 在 UI 线程上重新定位所有嵌入窗口以匹配新尺寸。
    /// </summary>
    /// <param name="sender">属性变更的源控件。</param>
    /// <param name="e">包含变更属性信息的事件参数。仅处理 <see cref="Visual.BoundsProperty"/> 变更。</param>
    private void OnHostControlPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Visual.BoundsProperty)
        {
            Dispatcher.UIThread.Post(UpdateAllWindowPositions);
        }
    }

    // ── Status reporting ────────────────────────────────────────────────

    /// <summary>
    /// 在 UI 线程上报告状态消息。通过 <see cref="StatusChanged"/> 事件发出，
    /// 供 ViewModel 订阅以更新状态栏。
    /// </summary>
    /// <param name="message">状态消息文本。</param>
    private void ReportStatus(string message)
    {
        Dispatcher.UIThread.Post(() => StatusChanged?.Invoke(message));
    }

    // ── IDisposable ─────────────────────────────────────────────────────

    /// <summary>
    /// 释放 Win32 窗口嵌入器占用的所有资源。
    /// 包括：停止后台窗口监控、解除事件订阅、还原所有嵌入窗口的父窗口、
    /// 关闭外部进程。
    /// </summary>
    /// <remarks>
    /// 清理顺序：
    /// <list type="number">
    /// <item>停止并释放后台窗口监控定时器</item>
    /// <item>解除宿主控件的属性变更订阅</item>
    /// <item>对所有嵌入窗口调用 <c>SetParent(hWnd, IntPtr.Zero)</c> 解除嵌入关系</item>
    /// <item>尝试优雅关闭外部进程（<c>CloseMainWindow</c> + 3 秒等待），失败则强制终止（<c>Kill</c>）</item>
    /// </list>
    /// 多次调用 <see cref="Dispose"/> 是安全的（幂等操作）。
    /// </remarks>
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
            // Best-effort cleanup
        }

        _process?.Dispose();
        _process = null;
    }
}
