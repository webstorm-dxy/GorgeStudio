using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GorgeStudio.AppCore.Models.Results;
using GorgeStudio.AppCore.Sessions;
using GorgeStudio.Services.GodotRemote;

namespace GorgeStudio.AppCore.Services;

public interface IGodotLaunchWorkflow
{
    event Action<string>? StatusChanged;

    Task<LaunchGodotResult> SaveLaunchAndLoadAsync(
        ChartSession session,
        string? explicitSavePath = null,
        CancellationToken ct = default);
}

public sealed class GodotLaunchWorkflow : IGodotLaunchWorkflow
{
    private readonly IChartWorkspaceService _workspaceService;
    private readonly IGodotRemoteClient _remoteClient;

    public event Action<string>? StatusChanged;

    public GodotLaunchWorkflow(
        IChartWorkspaceService workspaceService,
        IGodotRemoteClient remoteClient)
    {
        _workspaceService = workspaceService;
        _remoteClient = remoteClient;
    }

    public async Task<LaunchGodotResult> SaveLaunchAndLoadAsync(
        ChartSession session,
        string? explicitSavePath = null,
        CancellationToken ct = default)
    {
        // 1. Save chart
        StatusChanged?.Invoke("正在保存谱面...");
        var saveResult = await _workspaceService.SaveAsync(session, explicitSavePath, ct);
        if (saveResult.Cancelled)
            return new LaunchGodotResult(false, "已取消加载");
        if (!saveResult.Success)
            return new LaunchGodotResult(false, $"保存失败：{saveResult.ErrorMessage ?? "未知错误"}");
        if (saveResult.FilePath == null)
            return new LaunchGodotResult(false, "保存失败：未获取到文件路径");

        // 2. Wait for Godot UDP ready (进程由嵌入器启动，此处仅复用)
        StatusChanged?.Invoke("等待 Godot 就绪...");
        var readyResult = await _remoteClient.WaitUntilReadyAsync(ct);
        if (!readyResult.Success)
            return new LaunchGodotResult(false, $"Godot 未就绪：{readyResult.Error ?? "超时"}");

        // 3. Send set_packages command
        StatusChanged?.Invoke("正在加载谱面到 Godot...");
        var setResult = await _remoteClient.SetGpkgAsync(saveResult.FilePath, ct);
        if (!setResult.Success)
            return new LaunchGodotResult(false, $"加载失败：{setResult.Error ?? "未知错误"}");

        StatusChanged?.Invoke($"Godot 已加载当前谱面，时长 {setResult.DurationSeconds:F1}s");
        return new LaunchGodotResult(true, null,
            setResult.DurationSeconds, setResult.BeginSeconds, setResult.EndSeconds);
    }
}
