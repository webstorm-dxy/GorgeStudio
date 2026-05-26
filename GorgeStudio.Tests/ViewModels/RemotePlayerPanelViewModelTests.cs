using GorgeStudio.Services.RemotePlayer;
using GorgeStudio.ViewModels;
using Xunit;

namespace GorgeStudio.Tests.ViewModels;

public sealed class RemotePlayerPanelViewModelTests
{
    [Fact]
    public void SetPackagesCommand_IsDisabledUntilBothPackagePathsAreProvided()
    {
        var vm = new RemotePlayerPanelViewModel(new FakeRemotePlayerService());

        Assert.False(vm.SetPackagesCommand.CanExecute(null));

        vm.RuntimePackagePath = "/runtime.zip";
        Assert.False(vm.SetPackagesCommand.CanExecute(null));

        vm.ChartPackagePath = "/chart.zip";
        Assert.True(vm.SetPackagesCommand.CanExecute(null));
    }

    [Fact]
    public async Task PlayCommand_SendsPlayThenRefreshesStatus()
    {
        var service = new FakeRemotePlayerService
        {
            Status = new RemotePlayerStatus(true, 10, 136, -1, 135, null)
        };
        var vm = new RemotePlayerPanelViewModel(service);

        await vm.PlayCommand.ExecuteAsync(null);

        Assert.Equal(1, service.PlayCount);
        Assert.Equal(1, service.StatusCount);
        Assert.Equal(10, vm.CurrentSeconds);
        Assert.Equal(136, vm.DurationSeconds);
        Assert.Equal("已连接", vm.ConnectionText);
    }

    [Fact]
    public async Task SeekCommand_SendsSeekSecondsAndKeepsPauseMessageWhenStatusTimesOut()
    {
        var service = new FakeRemotePlayerService
        {
            ThrowStatusTimeout = true
        };
        var vm = new RemotePlayerPanelViewModel(service)
        {
            SeekSeconds = 12.5
        };

        await vm.SeekCommand.ExecuteAsync(null);

        Assert.Equal(12.5, service.LastSeekSeconds);
        Assert.Equal("状态查询超时", vm.ConnectionText);
        Assert.Contains("远程端将保持暂停", vm.MessageText);
    }

    [Fact]
    public async Task SetPackagesCommand_SendsParsedPathLists()
    {
        var service = new FakeRemotePlayerService
        {
            PackageResult = new RemotePlayerPackageResult(
                true,
                new[] { "res://addons/gorgeplugin/Native.zip", "/runtime-a.zip", "/runtime-b.zip" },
                new[] { "/chart.zip" },
                136,
                -1,
                135,
                null)
        };
        var vm = new RemotePlayerPanelViewModel(service)
        {
            RuntimePackagePath = "/runtime-a.zip\n/runtime-b.zip",
            ChartPackagePath = "/chart.zip"
        };

        await vm.SetPackagesCommand.ExecuteAsync(null);

        Assert.Equal(new[] { "/runtime-a.zip", "/runtime-b.zip" }, service.LastRuntimePackagePaths);
        Assert.Equal(new[] { "/chart.zip" }, service.LastChartPackagePaths);
        Assert.Equal(136, vm.DurationSeconds);
        Assert.Equal(-1, vm.CurrentSeconds);
        Assert.Equal("包已准备", vm.ConnectionText);
    }

    private sealed class FakeRemotePlayerService : IRemotePlayerService
    {
        public int PlayCount { get; private set; }

        public int StatusCount { get; private set; }

        public double? LastSeekSeconds { get; private set; }

        public IReadOnlyList<string> LastRuntimePackagePaths { get; private set; } = Array.Empty<string>();

        public IReadOnlyList<string> LastChartPackagePaths { get; private set; } = Array.Empty<string>();

        public bool ThrowStatusTimeout { get; init; }

        public RemotePlayerStatus Status { get; init; } = new(true, 0, 0, 0, 0, null);

        public RemotePlayerPackageResult PackageResult { get; init; } = new(
            true,
            Array.Empty<string>(),
            Array.Empty<string>(),
            0,
            0,
            0,
            null);

        public Task SendPlayAsync(RemotePlayerEndpoint endpoint, CancellationToken cancellationToken = default)
        {
            PlayCount++;
            return Task.CompletedTask;
        }

        public Task SendPauseAsync(RemotePlayerEndpoint endpoint, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SendStopAsync(RemotePlayerEndpoint endpoint, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SendSeekAsync(RemotePlayerEndpoint endpoint, double seconds, CancellationToken cancellationToken = default)
        {
            LastSeekSeconds = seconds;
            return Task.CompletedTask;
        }

        public Task<RemotePlayerStatus> GetStatusAsync(RemotePlayerEndpoint endpoint, CancellationToken cancellationToken = default)
        {
            StatusCount++;
            if (ThrowStatusTimeout)
                throw new TimeoutException();

            return Task.FromResult(Status);
        }

        public Task<RemotePlayerPackageResult> SetPackagesAsync(
            RemotePlayerEndpoint endpoint,
            IReadOnlyList<string> runtimePackagePaths,
            IReadOnlyList<string> chartPackagePaths,
            CancellationToken cancellationToken = default)
        {
            LastRuntimePackagePaths = runtimePackagePaths.ToArray();
            LastChartPackagePaths = chartPackagePaths.ToArray();
            return Task.FromResult(PackageResult);
        }
    }
}
