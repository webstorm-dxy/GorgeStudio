namespace GorgeStudio.Services.GodotRemote;

public record GodotSetPackagesResult(bool Success, string? Error = null, double DurationSeconds = 0, double BeginSeconds = 0, double EndSeconds = 0);

public record GodotRemoteStatusResult(bool Success, string? Error = null);

public record PlaybackStatusResult(
    bool Success,
    string? Error = null,
    double CurrentSeconds = 0,
    double BeginSeconds = 0,
    double EndSeconds = 0,
    double DurationSeconds = 0);

public record PlaybackCommandResult(bool Success, string? Error = null);
