using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace GorgeStudio.Services.RemotePlayer;

/// <summary>
/// Sends UTF-8 JSON packets to the Gorge remote player UDP API.
/// </summary>
public sealed class RemotePlayerService : IRemotePlayerService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = null
    };

    public Task SendPlayAsync(RemotePlayerEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(endpoint, new { cmd = "play" }, cancellationToken);
    }

    public Task SendPauseAsync(RemotePlayerEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(endpoint, new { cmd = "pause" }, cancellationToken);
    }

    public Task SendStopAsync(RemotePlayerEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(endpoint, new { cmd = "stop" }, cancellationToken);
    }

    public Task SendSeekAsync(RemotePlayerEndpoint endpoint, double seconds, CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(endpoint, new { cmd = "seek", seconds }, cancellationToken);
    }

    public async Task<RemotePlayerStatus> GetStatusAsync(
        RemotePlayerEndpoint endpoint,
        CancellationToken cancellationToken = default)
    {
        using var document = await SendAndReceiveJsonAsync(endpoint, new { cmd = "status" }, cancellationToken);
        var root = document.RootElement;
        var ok = GetBoolean(root, "ok");

        return new RemotePlayerStatus(
            ok,
            ok ? GetDouble(root, "currentSeconds") : 0,
            ok ? GetDouble(root, "durationSeconds") : 0,
            ok ? GetDouble(root, "beginSeconds") : 0,
            ok ? GetDouble(root, "endSeconds") : 0,
            ok ? null : GetString(root, "error"));
    }

    public async Task<RemotePlayerPackageResult> SetPackagesAsync(
        RemotePlayerEndpoint endpoint,
        IReadOnlyList<string> runtimePackagePaths,
        IReadOnlyList<string> chartPackagePaths,
        CancellationToken cancellationToken = default)
    {
        if (runtimePackagePaths.Count == 0)
            throw new ArgumentException("At least one runtime package path is required.", nameof(runtimePackagePaths));
        if (chartPackagePaths.Count == 0)
            throw new ArgumentException("At least one chart package path is required.", nameof(chartPackagePaths));

        using var document = await SendAndReceiveJsonAsync(
            endpoint,
            new
            {
                cmd = "set_packages",
                runtimePackagePaths,
                chartPackagePaths
            },
            cancellationToken);

        var root = document.RootElement;
        var ok = GetBoolean(root, "ok");

        return new RemotePlayerPackageResult(
            ok,
            ok ? GetStringArray(root, "runtimePackagePaths") : Array.Empty<string>(),
            ok ? GetStringArray(root, "chartPackagePaths") : Array.Empty<string>(),
            ok ? GetDouble(root, "durationSeconds") : 0,
            ok ? GetDouble(root, "beginSeconds") : 0,
            ok ? GetDouble(root, "endSeconds") : 0,
            ok ? null : GetString(root, "error"));
    }

    private static async Task SendCommandAsync(
        RemotePlayerEndpoint endpoint,
        object payload,
        CancellationToken cancellationToken)
    {
        using var client = new UdpClient(AddressFamily.InterNetwork);
        var bytes = Serialize(payload);
        await client.SendAsync(bytes, endpoint.Host, endpoint.Port, cancellationToken);
    }

    private static async Task<JsonDocument> SendAndReceiveJsonAsync(
        RemotePlayerEndpoint endpoint,
        object payload,
        CancellationToken cancellationToken)
    {
        using var client = new UdpClient(AddressFamily.InterNetwork);
        var bytes = Serialize(payload);
        await client.SendAsync(bytes, endpoint.Host, endpoint.Port, cancellationToken);

        var receiveTask = client.ReceiveAsync(cancellationToken).AsTask();
        var timeoutTask = Task.Delay(endpoint.Timeout, cancellationToken);
        var completed = await Task.WhenAny(receiveTask, timeoutTask);

        if (completed != receiveTask)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException(BuildTimeoutMessage(endpoint));
        }

        var result = await receiveTask;
        return JsonDocument.Parse(result.Buffer);
    }

    private static byte[] Serialize(object payload)
    {
        return JsonSerializer.SerializeToUtf8Bytes(payload, SerializerOptions);
    }

    private static string BuildTimeoutMessage(RemotePlayerEndpoint endpoint)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "No UDP response received from {0}:{1} within {2:0.###}s.",
            endpoint.Host,
            endpoint.Port,
            endpoint.Timeout.TotalSeconds);
    }

    private static bool GetBoolean(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.True;
    }

    private static double GetDouble(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
            return 0;

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetDouble(out var value) => value,
            JsonValueKind.String when double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) => value,
            _ => 0
        };
    }

    private static string? GetString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static IReadOnlyList<string> GetStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();

        return property.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .ToArray();
    }
}
