using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GorgeStudio.Services.GodotRemote;

public sealed class GodotRemoteClient : IGodotRemoteClient, IDisposable
{
    private readonly GodotRemoteOptions _options;
    private UdpClient? _client;

    public GodotRemoteClient(GodotRemoteOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<GodotRemoteStatusResult> WaitUntilReadyAsync(CancellationToken ct = default)
    {
        using var timeoutCts = new CancellationTokenSource(_options.ReadyTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        var linkedCt = linkedCts.Token;

        while (!linkedCt.IsCancellationRequested)
        {
            try
            {
                EnsureClient();
                var request = Encoding.UTF8.GetBytes("{\"cmd\":\"status\"}");
                await _client!.SendAsync(new ReadOnlyMemory<byte>(request), _options.Host, _options.Port, linkedCt);

                using var responseCts = new CancellationTokenSource(_options.SingleRequestTimeout);
                using var responseLinked = CancellationTokenSource.CreateLinkedTokenSource(linkedCt, responseCts.Token);
                var result = await _client.ReceiveAsync(responseLinked.Token);
                var json = Encoding.UTF8.GetString(result.Buffer);

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("ok", out var ok) && ok.GetBoolean())
                    return new GodotRemoteStatusResult(true);

                var error = root.TryGetProperty("error", out var err) ? err.GetString() : "Unknown response";
                return new GodotRemoteStatusResult(false, error);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                return new GodotRemoteStatusResult(false, "Godot remote not ready within timeout");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return new GodotRemoteStatusResult(false, "Cancelled");
            }
            catch (Exception)
            {
                // Retry after delay
            }

            try { await Task.Delay(_options.RetryDelay, linkedCt); }
            catch (OperationCanceledException) { }
        }

        return new GodotRemoteStatusResult(false, "Cancelled");
    }

    public async Task<GodotSetPackagesResult> SetGpkgAsync(string gpkgPath, CancellationToken ct = default)
    {
        var normalizedPath = Path.GetFullPath(gpkgPath).Replace('\\', '/');

        using var timeoutCts = new CancellationTokenSource(_options.SetPackagesTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        var linkedCt = linkedCts.Token;

        try
        {
            EnsureClient();
            var payload = JsonSerializer.Serialize(new { cmd = "set_packages", gpkgPath = normalizedPath });
            var request = Encoding.UTF8.GetBytes(payload);
            await _client!.SendAsync(new ReadOnlyMemory<byte>(request), _options.Host, _options.Port, linkedCt);

            using var responseCts = new CancellationTokenSource(_options.SingleRequestTimeout);
            using var responseLinked = CancellationTokenSource.CreateLinkedTokenSource(linkedCt, responseCts.Token);
            var result = await _client.ReceiveAsync(responseLinked.Token);
            var json = Encoding.UTF8.GetString(result.Buffer);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("ok", out var ok) && ok.GetBoolean())
            {
                double duration = root.TryGetProperty("duration", out var d) ? d.GetDouble() : 0;
                double begin = root.TryGetProperty("begin", out var b) ? b.GetDouble() : 0;
                double end = root.TryGetProperty("end", out var e) ? e.GetDouble() : 0;
                return new GodotSetPackagesResult(true, null, duration, begin, end);
            }

            var error = root.TryGetProperty("error", out var err) ? err.GetString() : "set_packages failed";
            return new GodotSetPackagesResult(false, error);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            return new GodotSetPackagesResult(false, "Godot set_packages timed out");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return new GodotSetPackagesResult(false, "Cancelled");
        }
        catch (Exception ex)
        {
            return new GodotSetPackagesResult(false, ex.Message);
        }
    }

    public async Task<PlaybackStatusResult> GetStatusAsync(CancellationToken ct = default)
    {
        try
        {
            EnsureClient();
            var request = Encoding.UTF8.GetBytes("{\"cmd\":\"status\"}");
            await _client!.SendAsync(new ReadOnlyMemory<byte>(request), _options.Host, _options.Port, ct);

            using var responseCts = new CancellationTokenSource(_options.SingleRequestTimeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, responseCts.Token);
            var result = await _client.ReceiveAsync(linked.Token);
            var json = Encoding.UTF8.GetString(result.Buffer);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("ok", out var ok) && ok.GetBoolean())
            {
                double current = root.TryGetProperty("currentSeconds", out var c) ? c.GetDouble() : 0;
                double begin = root.TryGetProperty("beginSeconds", out var b) ? b.GetDouble() : 0;
                double end = root.TryGetProperty("endSeconds", out var e) ? e.GetDouble() : 0;
                double duration = root.TryGetProperty("durationSeconds", out var d) ? d.GetDouble() : 0;
                return new PlaybackStatusResult(true, null, current, begin, end, duration);
            }

            var error = root.TryGetProperty("error", out var err) ? err.GetString() : "status failed";
            return new PlaybackStatusResult(false, error);
        }
        catch (OperationCanceledException)
        {
            return new PlaybackStatusResult(false, "Cancelled");
        }
        catch (Exception ex)
        {
            return new PlaybackStatusResult(false, ex.Message);
        }
    }

    public async Task<PlaybackCommandResult> SendPlayAsync(CancellationToken ct = default)
    {
        return await SendFireAndForgetAsync("{\"cmd\":\"play\"}", ct);
    }

    public async Task<PlaybackCommandResult> SendPauseAsync(CancellationToken ct = default)
    {
        return await SendFireAndForgetAsync("{\"cmd\":\"pause\"}", ct);
    }

    public async Task<PlaybackCommandResult> SendSeekAsync(double seconds, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(new { cmd = "seek", seconds });
        return await SendFireAndForgetAsync(payload, ct);
    }

    private async Task<PlaybackCommandResult> SendFireAndForgetAsync(string payload, CancellationToken ct)
    {
        try
        {
            EnsureClient();
            var request = Encoding.UTF8.GetBytes(payload);
            await _client!.SendAsync(new ReadOnlyMemory<byte>(request), _options.Host, _options.Port, ct);
            return new PlaybackCommandResult(true);
        }
        catch (OperationCanceledException)
        {
            return new PlaybackCommandResult(false, "Cancelled");
        }
        catch (Exception ex)
        {
            return new PlaybackCommandResult(false, ex.Message);
        }
    }

    private void EnsureClient()
    {
        _client ??= new UdpClient();
        try { _client.Client.ReceiveTimeout = (int)_options.SingleRequestTimeout.TotalMilliseconds; }
        catch { }
    }

    public void Dispose()
    {
        _client?.Dispose();
        _client = null;
    }
}
