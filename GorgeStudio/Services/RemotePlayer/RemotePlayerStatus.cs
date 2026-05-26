namespace GorgeStudio.Services.RemotePlayer;

/// <summary>
/// Parsed response for the remote player's status command.
/// </summary>
public sealed record RemotePlayerStatus(
    bool Ok,
    double CurrentSeconds,
    double DurationSeconds,
    double BeginSeconds,
    double EndSeconds,
    string? Error);
