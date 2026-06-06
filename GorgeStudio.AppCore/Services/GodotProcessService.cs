using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace GorgeStudio.AppCore.Services;

public record ProcessLaunchResult(bool Success, string? ErrorMessage = null, Process? Process = null);

public interface IGodotProcessService
{
    event Action<string>? StatusChanged;
    Process? CurrentProcess { get; }
    Task<ProcessLaunchResult> LaunchAsync(CancellationToken ct = default);
    void Dispose();
}

public sealed class GodotProcessService : IGodotProcessService, IDisposable
{
    public event Action<string>? StatusChanged;
    public Process? CurrentProcess { get; private set; }

    public Task<ProcessLaunchResult> LaunchAsync(CancellationToken ct = default)
    {
        try
        {
            var exePath = ResolveExePath();
            if (!File.Exists(exePath))
                return Task.FromResult(new ProcessLaunchResult(false, $"Godot executable not found: {exePath}"));

            StatusChanged?.Invoke("正在启动 Godot...");

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = false,
                CreateNoWindow = false,
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? ""
            };

            CurrentProcess = Process.Start(startInfo);
            if (CurrentProcess == null)
                return Task.FromResult(new ProcessLaunchResult(false, "Failed to start Godot process"));

            StatusChanged?.Invoke("Godot 进程已启动");
            return Task.FromResult(new ProcessLaunchResult(true, Process: CurrentProcess));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ProcessLaunchResult(false, ex.Message));
        }
    }

    private static string ResolveExePath()
    {
        var baseDir = AppContext.BaseDirectory;
        var exeName = GetPlatformExecutableName();

        var exePath = Path.Combine(baseDir, "GodotApplication", exeName);
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
        if (CurrentProcess is { HasExited: false })
        {
            try { CurrentProcess.CloseMainWindow(); } catch { }
            if (!CurrentProcess.WaitForExit(3000))
            {
                try { CurrentProcess.Kill(); } catch { }
            }
            CurrentProcess.Dispose();
        }
        CurrentProcess = null;
    }
}
