using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GorgeStudio.Services.RemotePlayer;

/// <summary>
/// UDP client for the Gorge remote player demo scene.
/// </summary>
public interface IRemotePlayerService
{
    Task SendPlayAsync(RemotePlayerEndpoint endpoint, CancellationToken cancellationToken = default);

    Task SendPauseAsync(RemotePlayerEndpoint endpoint, CancellationToken cancellationToken = default);

    Task SendStopAsync(RemotePlayerEndpoint endpoint, CancellationToken cancellationToken = default);

    Task SendSeekAsync(RemotePlayerEndpoint endpoint, double seconds, CancellationToken cancellationToken = default);

    Task<RemotePlayerStatus> GetStatusAsync(RemotePlayerEndpoint endpoint, CancellationToken cancellationToken = default);

    Task<RemotePlayerPackageResult> SetPackagesAsync(
        RemotePlayerEndpoint endpoint,
        IReadOnlyList<string> runtimePackagePaths,
        IReadOnlyList<string> chartPackagePaths,
        CancellationToken cancellationToken = default);
}
