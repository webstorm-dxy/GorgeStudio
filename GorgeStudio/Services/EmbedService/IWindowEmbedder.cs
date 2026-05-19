using System;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace GorgeStudio.Services.EmbedService;

/// <summary>
/// Abstracts OS-level window embedding for an external process.
/// Windows implementation uses SetParent to embed; macOS implementation
/// launches the process as a standalone window.
/// </summary>
public interface IWindowEmbedder : IDisposable
{
    /// <summary>
    /// Launch the external process. On Windows, embed its windows into <paramref name="hostControl"/>.
    /// On macOS, launch as a standalone window (hostControl/parentWindow are ignored).
    /// </summary>
    Task<bool> EmbedAsync(
        Control hostControl,
        Window parentWindow,
        string executablePath,
        string? workingDirectory = null,
        TimeSpan? timeout = null);

    /// <summary>Status messages for UI feedback.</summary>
    event Action<string>? StatusChanged;
}
