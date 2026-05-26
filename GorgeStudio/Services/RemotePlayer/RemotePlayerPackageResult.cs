using System.Collections.Generic;

namespace GorgeStudio.Services.RemotePlayer;

/// <summary>
/// Parsed response for the remote player's set_packages command.
/// </summary>
public sealed record RemotePlayerPackageResult(
    bool Ok,
    IReadOnlyList<string> RuntimePackagePaths,
    IReadOnlyList<string> ChartPackagePaths,
    double DurationSeconds,
    double BeginSeconds,
    double EndSeconds,
    string? Error);
