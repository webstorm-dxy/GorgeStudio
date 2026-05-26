using System;

namespace GorgeStudio.Services.RemotePlayer;

/// <summary>
/// UDP endpoint configuration for the Gorge remote player demo scene.
/// </summary>
public sealed record RemotePlayerEndpoint(string Host, int Port, TimeSpan Timeout)
{
    public static RemotePlayerEndpoint Default { get; } = new("127.0.0.1", 9000, TimeSpan.FromSeconds(2));
}
