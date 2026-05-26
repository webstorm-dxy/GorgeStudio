namespace GorgeStudio.Services.RemotePlayer;

/// <summary>
/// UI-friendly result shape for one-way remote player commands.
/// </summary>
public sealed record RemotePlayerCommandResult(
    bool Success,
    string Message,
    bool TimedOut,
    string? Error);
