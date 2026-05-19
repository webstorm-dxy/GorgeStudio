using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace GorgeStudio.Services;

/// <summary>
/// macOS window embedder. Launches the Godot process as a standalone window.
/// Native window embedding is not available on macOS, so the process runs independently.
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class MacWindowEmbedder : IWindowEmbedder
{
    private Process? _process;
    private bool _disposed;

    public event Action<string>? StatusChanged;

    public Task<bool> EmbedAsync(
        Control hostControl,
        Window parentWindow,
        string executablePath,
        string? workingDirectory = null,
        TimeSpan? timeout = null)
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

    private void OnProcessExited(object? sender, EventArgs e)
    {
        StatusChanged?.Invoke("Godot application process exited");
    }

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
