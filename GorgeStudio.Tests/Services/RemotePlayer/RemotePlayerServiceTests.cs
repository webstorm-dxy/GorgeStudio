using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using GorgeStudio.Services.RemotePlayer;
using Xunit;

namespace GorgeStudio.Tests.Services.RemotePlayer;

public sealed class RemotePlayerServiceTests
{
    [Fact]
    public async Task SendPlayAsync_SendsPlayCommand()
    {
        var received = await CaptureSingleCommandAsync(service => endpoint => service.SendPlayAsync(endpoint));

        Assert.Equal("play", GetString(received, "cmd"));
    }

    [Fact]
    public async Task SendPauseAsync_SendsPauseCommand()
    {
        var received = await CaptureSingleCommandAsync(service => endpoint => service.SendPauseAsync(endpoint));

        Assert.Equal("pause", GetString(received, "cmd"));
    }

    [Fact]
    public async Task SendStopAsync_SendsStopCommand()
    {
        var received = await CaptureSingleCommandAsync(service => endpoint => service.SendStopAsync(endpoint));

        Assert.Equal("stop", GetString(received, "cmd"));
    }

    [Fact]
    public async Task SendSeekAsync_SendsSeekSeconds()
    {
        var received = await CaptureSingleCommandAsync(service =>
            endpoint => service.SendSeekAsync(endpoint, 12.5));

        Assert.Equal("seek", GetString(received, "cmd"));
        Assert.Equal(12.5, GetDouble(received, "seconds"));
    }

    [Fact]
    public async Task GetStatusAsync_ParsesSuccessResponse()
    {
        var (service, endpoint, server) = CreateServer();
        var receiveTask = ReceiveAndReplyAsync(server,
            """{"type":"status","ok":true,"currentSeconds":10.0,"durationSeconds":136.0,"beginSeconds":-1.0,"endSeconds":135.0}""");

        var status = await service.GetStatusAsync(endpoint);
        var request = await receiveTask;

        Assert.Equal("status", GetString(request, "cmd"));
        Assert.True(status.Ok);
        Assert.Equal(10.0, status.CurrentSeconds);
        Assert.Equal(136.0, status.DurationSeconds);
        Assert.Equal(-1.0, status.BeginSeconds);
        Assert.Equal(135.0, status.EndSeconds);
        Assert.Null(status.Error);
    }

    [Fact]
    public async Task GetStatusAsync_ParsesChartNotReady()
    {
        var (service, endpoint, server) = CreateServer();
        _ = ReceiveAndReplyAsync(server,
            """{"type":"status","ok":false,"error":"chart_not_ready"}""");

        var status = await service.GetStatusAsync(endpoint);

        Assert.False(status.Ok);
        Assert.Equal("chart_not_ready", status.Error);
        Assert.Equal(0, status.CurrentSeconds);
        Assert.Equal(0, status.DurationSeconds);
    }

    [Fact]
    public async Task SetPackagesAsync_SendsPathArraysAndParsesSuccess()
    {
        var (service, endpoint, server) = CreateServer();
        var receiveTask = ReceiveAndReplyAsync(server,
            """
            {"type":"set_packages","ok":true,"runtimePackagePaths":["res://addons/gorgeplugin/Native.zip","/runtime.zip"],"chartPackagePaths":["/chart.zip"],"durationSeconds":136.0,"beginSeconds":-1.0,"endSeconds":135.0}
            """);

        var result = await service.SetPackagesAsync(
            endpoint,
            new[] { "/runtime.zip" },
            new[] { "/chart.zip" });
        var request = await receiveTask;

        Assert.Equal("set_packages", GetString(request, "cmd"));
        Assert.Equal(new[] { "/runtime.zip" }, GetStringArray(request, "runtimePackagePaths"));
        Assert.Equal(new[] { "/chart.zip" }, GetStringArray(request, "chartPackagePaths"));
        Assert.True(result.Ok);
        Assert.Equal(new[] { "res://addons/gorgeplugin/Native.zip", "/runtime.zip" }, result.RuntimePackagePaths);
        Assert.Equal(new[] { "/chart.zip" }, result.ChartPackagePaths);
        Assert.Equal(136.0, result.DurationSeconds);
        Assert.Equal(-1.0, result.BeginSeconds);
        Assert.Equal(135.0, result.EndSeconds);
    }

    [Fact]
    public async Task SetPackagesAsync_ParsesPrepareFailed()
    {
        var (service, endpoint, server) = CreateServer();
        _ = ReceiveAndReplyAsync(server,
            """{"type":"set_packages","ok":false,"error":"prepare_failed"}""");

        var result = await service.SetPackagesAsync(
            endpoint,
            new[] { "/missing-runtime.zip" },
            new[] { "/missing-chart.zip" });

        Assert.False(result.Ok);
        Assert.Equal("prepare_failed", result.Error);
        Assert.Empty(result.RuntimePackagePaths);
        Assert.Empty(result.ChartPackagePaths);
    }

    [Fact]
    public async Task GetStatusAsync_ThrowsTimeoutExceptionWhenNoResponseArrives()
    {
        using var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)server.Client.LocalEndPoint!).Port;
        var service = new RemotePlayerService();
        var endpoint = new RemotePlayerEndpoint("127.0.0.1", port, TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAsync<TimeoutException>(() => service.GetStatusAsync(endpoint));
    }

    private static async Task<JsonElement> CaptureSingleCommandAsync(
        Func<RemotePlayerService, Func<RemotePlayerEndpoint, Task>> commandFactory)
    {
        var (service, endpoint, server) = CreateServer();
        var receiveTask = ReceiveOneAsync(server);

        await commandFactory(service)(endpoint);

        return await receiveTask;
    }

    private static (RemotePlayerService Service, RemotePlayerEndpoint Endpoint, UdpClient Server) CreateServer()
    {
        var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)server.Client.LocalEndPoint!).Port;
        var endpoint = new RemotePlayerEndpoint("127.0.0.1", port, TimeSpan.FromSeconds(1));
        return (new RemotePlayerService(), endpoint, server);
    }

    private static async Task<JsonElement> ReceiveAndReplyAsync(UdpClient server, string responseJson)
    {
        try
        {
            var result = await server.ReceiveAsync();
            var responseBytes = Encoding.UTF8.GetBytes(responseJson);
            await server.SendAsync(responseBytes, result.RemoteEndPoint);
            return Parse(result.Buffer);
        }
        finally
        {
            server.Dispose();
        }
    }

    private static async Task<JsonElement> ReceiveOneAsync(UdpClient server)
    {
        try
        {
            var result = await server.ReceiveAsync();
            return Parse(result.Buffer);
        }
        finally
        {
            server.Dispose();
        }
    }

    private static JsonElement Parse(byte[] buffer)
    {
        using var document = JsonDocument.Parse(buffer);
        return document.RootElement.Clone();
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName).GetString();
    }

    private static double GetDouble(JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName).GetDouble();
    }

    private static string[] GetStringArray(JsonElement element, string propertyName)
    {
        return element.GetProperty(propertyName)
            .EnumerateArray()
            .Select(item => item.GetString()!)
            .ToArray();
    }
}
