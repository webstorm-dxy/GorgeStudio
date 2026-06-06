using System.Threading;
using System.Threading.Tasks;

namespace GorgeStudio.Services.GodotRemote;

public interface IGodotRemoteClient
{
    Task<GodotRemoteStatusResult> WaitUntilReadyAsync(CancellationToken ct = default);
    Task<GodotSetPackagesResult> SetGpkgAsync(string gpkgPath, CancellationToken ct = default);
}
