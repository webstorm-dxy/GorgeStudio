using System;
using System.Threading;
using System.Threading.Tasks;
using GorgeStudio.AppCore.Sessions;
using GorgeStudio.Services.GodotRemote;

namespace GorgeStudio.AppCore.Services;

public record PlaybackResult(bool Success, string? Error = null, double CurrentSeconds = 0);

public interface IPlaybackWorkflow
{
    event Action<string>? StatusChanged;

    Task<PlaybackResult> PlayFromAsync(
        ChartSession session,
        double playheadSeconds,
        string? explicitSavePath = null,
        CancellationToken ct = default);

    Task<PlaybackResult> PauseAsync(CancellationToken ct = default);
    Task<PlaybackStatusResult> GetStatusAsync(CancellationToken ct = default);
}

public sealed class PlaybackWorkflow : IPlaybackWorkflow
{
    private readonly IChartWorkspaceService _workspaceService;
    private readonly IGodotProcessService _processService;
    private readonly IGodotRemoteClient _remoteClient;

    public event Action<string>? StatusChanged;

    public PlaybackWorkflow(
        IChartWorkspaceService workspaceService,
        IGodotProcessService processService,
        IGodotRemoteClient remoteClient)
    {
        _workspaceService = workspaceService;
        _processService = processService;
        _remoteClient = remoteClient;
    }

    public async Task<PlaybackResult> PlayFromAsync(
        ChartSession session,
        double playheadSeconds,
        string? explicitSavePath = null,
        CancellationToken ct = default)
    {
        // 1. Save chart
        StatusChanged?.Invoke("正在保存谱面...");
        var saveResult = await _workspaceService.SaveAsync(session, explicitSavePath, ct);
        if (saveResult.Cancelled)
            return new PlaybackResult(false, "已取消");
        if (!saveResult.Success)
            return new PlaybackResult(false, $"保存失败：{saveResult.ErrorMessage ?? "未知错误"}");
        if (saveResult.FilePath == null)
            return new PlaybackResult(false, "保存失败：未获取到文件路径");

        // 2. Launch Godot process
        StatusChanged?.Invoke("正在启动 Godot...");
        var launchResult = await _processService.LaunchAsync(ct);
        if (!launchResult.Success)
            return new PlaybackResult(false, $"Godot 启动失败：{launchResult.ErrorMessage ?? "未知错误"}");

        // 3. Wait for UDP ready
        StatusChanged?.Invoke("等待 Godot 就绪...");
        var readyResult = await _remoteClient.WaitUntilReadyAsync(ct);
        if (!readyResult.Success)
            return new PlaybackResult(false, $"Godot UDP 未就绪：{readyResult.Error ?? "超时"}");

        // 4. Send set_packages
        StatusChanged?.Invoke("正在加载谱面到 Godot...");
        var setResult = await _remoteClient.SetGpkgAsync(saveResult.FilePath, ct);
        if (!setResult.Success)
            return new PlaybackResult(false, $"set_packages 失败：{setResult.Error ?? "未知错误"}");

        // 5. Seek to playhead
        StatusChanged?.Invoke("正在定位播放位置...");
        var seekResult = await _remoteClient.SendSeekAsync(playheadSeconds, ct);
        if (!seekResult.Success)
            return new PlaybackResult(false, $"seek 命令发送失败：{seekResult.Error ?? "未知错误"}");

        // 6. Brief delay for Godot to process seek
        await Task.Delay(50, ct);

        // 7. Play
        var playResult = await _remoteClient.SendPlayAsync(ct);
        if (!playResult.Success)
            return new PlaybackResult(false, $"play 命令发送失败：{playResult.Error ?? "未知错误"}");

        // 8. Query status to confirm
        await Task.Delay(50, ct);
        var status = await _remoteClient.GetStatusAsync(ct);
        var currentSec = status.Success ? status.CurrentSeconds : playheadSeconds;

        StatusChanged?.Invoke("播放中");
        return new PlaybackResult(true, null, currentSec);
    }

    public async Task<PlaybackResult> PauseAsync(CancellationToken ct = default)
    {
        // 1. Send pause
        var pauseResult = await _remoteClient.SendPauseAsync(ct);
        if (!pauseResult.Success)
            return new PlaybackResult(false, $"pause 命令发送失败：{pauseResult.Error ?? "未知错误"}");

        // 2. Wait briefly, then query status
        await Task.Delay(50, ct);
        var status = await _remoteClient.GetStatusAsync(ct);
        if (!status.Success)
            return new PlaybackResult(false, $"status 查询失败：{status.Error ?? "未知错误"}");

        StatusChanged?.Invoke("已暂停");
        return new PlaybackResult(true, null, status.CurrentSeconds);
    }

    public async Task<PlaybackStatusResult> GetStatusAsync(CancellationToken ct = default)
    {
        return await _remoteClient.GetStatusAsync(ct);
    }
}
