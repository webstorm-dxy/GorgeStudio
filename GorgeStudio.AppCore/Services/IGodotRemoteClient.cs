using System.Threading;
using System.Threading.Tasks;

namespace GorgeStudio.Services.GodotRemote;

public interface IGodotRemoteClient
{
    Task<GodotRemoteStatusResult> WaitUntilReadyAsync(CancellationToken ct = default);
    Task<GodotSetPackagesResult> SetGpkgAsync(string gpkgPath, CancellationToken ct = default);
    Task<PlaybackStatusResult> GetStatusAsync(CancellationToken ct = default);
    Task<PlaybackCommandResult> SendPlayAsync(CancellationToken ct = default);
    Task<PlaybackCommandResult> SendPauseAsync(CancellationToken ct = default);
    Task<PlaybackCommandResult> SendSeekAsync(double seconds, CancellationToken ct = default);
}
