using System;

namespace GorgeStudio.Services.GodotRemote;

public sealed class GodotRemoteOptions
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 9000;
    public TimeSpan SingleRequestTimeout { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan ReadyTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan SetPackagesTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(250);
}
