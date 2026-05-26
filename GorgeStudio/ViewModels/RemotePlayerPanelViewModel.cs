using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GorgeStudio.Services.RemotePlayer;

namespace GorgeStudio.ViewModels;

/// <summary>
/// ViewModel for controlling the Gorge remote player demo scene through UDP.
/// </summary>
public partial class RemotePlayerPanelViewModel : ViewModelBase
{
    private readonly IRemotePlayerService? _remotePlayerService;

    [ObservableProperty]
    private string _host = RemotePlayerEndpoint.Default.Host;

    [ObservableProperty]
    private int _port = RemotePlayerEndpoint.Default.Port;

    [ObservableProperty]
    private double _timeoutSeconds = RemotePlayerEndpoint.Default.Timeout.TotalSeconds;

    [ObservableProperty]
    private string _runtimePackagePath = string.Empty;

    [ObservableProperty]
    private string _chartPackagePath = string.Empty;

    [ObservableProperty]
    private double _seekSeconds;

    [ObservableProperty]
    private double _currentSeconds;

    [ObservableProperty]
    private double _durationSeconds;

    [ObservableProperty]
    private double _beginSeconds;

    [ObservableProperty]
    private double _endSeconds;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _connectionText = "未连接";

    [ObservableProperty]
    private string _messageText = "请先运行 Godot 远程播放 demo 场景";

    public string CurrentTimeText => FormatSeconds(CurrentSeconds);

    public string DurationText => FormatSeconds(DurationSeconds);

    public string RangeText => DurationSeconds > 0
        ? $"{FormatSeconds(BeginSeconds)} - {FormatSeconds(EndSeconds)}"
        : "-";

    public bool CanSendCommand => !IsBusy && _remotePlayerService != null && IsEndpointValid();

    public bool CanSetPackages => CanSendCommand
                                  && !string.IsNullOrWhiteSpace(RuntimePackagePath)
                                  && !string.IsNullOrWhiteSpace(ChartPackagePath);

    public RemotePlayerPanelViewModel()
    {
    }

    public RemotePlayerPanelViewModel(IRemotePlayerService remotePlayerService)
    {
        _remotePlayerService = remotePlayerService;
    }

    partial void OnHostChanged(string value)
    {
        NotifyCommandStatesChanged();
    }

    partial void OnPortChanged(int value)
    {
        NotifyCommandStatesChanged();
    }

    partial void OnTimeoutSecondsChanged(double value)
    {
        NotifyCommandStatesChanged();
    }

    partial void OnRuntimePackagePathChanged(string value)
    {
        SetPackagesCommand.NotifyCanExecuteChanged();
    }

    partial void OnChartPackagePathChanged(string value)
    {
        SetPackagesCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        NotifyCommandStatesChanged();
    }

    partial void OnCurrentSecondsChanged(double value)
    {
        OnPropertyChanged(nameof(CurrentTimeText));
    }

    partial void OnDurationSecondsChanged(double value)
    {
        OnPropertyChanged(nameof(DurationText));
        OnPropertyChanged(nameof(RangeText));
    }

    partial void OnBeginSecondsChanged(double value)
    {
        OnPropertyChanged(nameof(RangeText));
    }

    partial void OnEndSecondsChanged(double value)
    {
        OnPropertyChanged(nameof(RangeText));
    }

    [RelayCommand(CanExecute = nameof(CanSendCommand))]
    private async Task PlayAsync()
    {
        await SendNoAckCommandAsync("播放命令已发送", endpoint => _remotePlayerService!.SendPlayAsync(endpoint));
    }

    [RelayCommand(CanExecute = nameof(CanSendCommand))]
    private async Task PauseAsync()
    {
        await SendNoAckCommandAsync("暂停命令已发送", endpoint => _remotePlayerService!.SendPauseAsync(endpoint));
    }

    [RelayCommand(CanExecute = nameof(CanSendCommand))]
    private async Task StopAsync()
    {
        await SendNoAckCommandAsync("停止命令已发送", endpoint => _remotePlayerService!.SendStopAsync(endpoint));
    }

    [RelayCommand(CanExecute = nameof(CanSendCommand))]
    private async Task SeekAsync()
    {
        await SendNoAckCommandAsync(
            $"跳转命令已发送：{SeekSeconds:F3}s，远程端将保持暂停",
            endpoint => _remotePlayerService!.SendSeekAsync(endpoint, SeekSeconds));
    }

    [RelayCommand(CanExecute = nameof(CanSendCommand))]
    private async Task RefreshStatusAsync()
    {
        await RunRemoteOperationAsync(async endpoint =>
        {
            var status = await _remotePlayerService!.GetStatusAsync(endpoint);
            ApplyStatus(status);
        });
    }

    [RelayCommand(CanExecute = nameof(CanSetPackages))]
    private async Task SetPackagesAsync()
    {
        var runtimePaths = ParsePathList(RuntimePackagePath);
        var chartPaths = ParsePathList(ChartPackagePath);

        if (runtimePaths.Count == 0)
        {
            MessageText = "请先选择 runtime 包路径";
            return;
        }

        if (chartPaths.Count == 0)
        {
            MessageText = "请先选择谱面包路径";
            return;
        }

        await RunRemoteOperationAsync(async endpoint =>
        {
            var result = await _remotePlayerService!.SetPackagesAsync(endpoint, runtimePaths, chartPaths);
            if (!result.Ok)
            {
                ConnectionText = "设置包失败";
                MessageText = $"set_packages 失败：{result.Error ?? "unknown_error"}";
                return;
            }

            BeginSeconds = result.BeginSeconds;
            EndSeconds = result.EndSeconds;
            DurationSeconds = result.DurationSeconds;
            CurrentSeconds = result.BeginSeconds;
            ConnectionText = "包已准备";
            MessageText = $"set_packages 成功，谱面时长 {DurationText}，runtime {result.RuntimePackagePaths.Count} 个，chart {result.ChartPackagePaths.Count} 个";
        });
    }

    [RelayCommand]
    private async Task PickRuntimePackageAsync()
    {
        var path = await PickPackageFileAsync("选择 runtime 包");
        if (path != null)
            RuntimePackagePath = path;
    }

    [RelayCommand]
    private async Task PickChartPackageAsync()
    {
        var path = await PickPackageFileAsync("选择谱面包");
        if (path != null)
            ChartPackagePath = path;
    }

    private async Task SendNoAckCommandAsync(
        string sentMessage,
        Func<RemotePlayerEndpoint, Task> send)
    {
        await RunRemoteOperationAsync(async endpoint =>
        {
            await send(endpoint);
            ConnectionText = "命令已发送";
            MessageText = sentMessage;

            try
            {
                var status = await _remotePlayerService!.GetStatusAsync(endpoint);
                ApplyStatus(status);
            }
            catch (TimeoutException)
            {
                ConnectionText = "状态查询超时";
                MessageText = $"{sentMessage}，但 status 无回包";
            }
        });
    }

    private async Task RunRemoteOperationAsync(Func<RemotePlayerEndpoint, Task> operation)
    {
        if (_remotePlayerService == null)
        {
            MessageText = "远程播放服务不可用";
            return;
        }

        if (!TryCreateEndpoint(out var endpoint, out var validationError))
        {
            MessageText = validationError;
            return;
        }

        IsBusy = true;
        try
        {
            await operation(endpoint);
        }
        catch (TimeoutException)
        {
            ConnectionText = "未连接";
            MessageText = $"未收到 UDP 响应，请确认远程 demo 已监听 {endpoint.Host}:{endpoint.Port}";
        }
        catch (SocketException ex)
        {
            ConnectionText = "UDP 错误";
            MessageText = $"UDP 发送失败：{ex.Message}";
        }
        catch (JsonException ex)
        {
            ConnectionText = "响应格式错误";
            MessageText = $"远程响应不是有效 JSON：{ex.Message}";
        }
        catch (OperationCanceledException)
        {
            ConnectionText = "已取消";
            MessageText = "远程命令已取消";
        }
        catch (Exception ex)
        {
            ConnectionText = "命令失败";
            MessageText = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyStatus(RemotePlayerStatus status)
    {
        if (!status.Ok)
        {
            ConnectionText = "谱面未准备";
            MessageText = $"status 失败：{status.Error ?? "unknown_error"}";
            return;
        }

        CurrentSeconds = status.CurrentSeconds;
        DurationSeconds = status.DurationSeconds;
        BeginSeconds = status.BeginSeconds;
        EndSeconds = status.EndSeconds;
        SeekSeconds = status.CurrentSeconds;
        ConnectionText = "已连接";
        MessageText = $"ChartTime {CurrentTimeText} / {DurationText}";
    }

    private bool TryCreateEndpoint(out RemotePlayerEndpoint endpoint, out string error)
    {
        endpoint = RemotePlayerEndpoint.Default;
        error = string.Empty;

        if (!IsEndpointValid())
        {
            error = "请输入有效的 UDP host、port 和 timeout";
            return false;
        }

        endpoint = new RemotePlayerEndpoint(
            Host.Trim(),
            Port,
            TimeSpan.FromSeconds(TimeoutSeconds));
        return true;
    }

    private bool IsEndpointValid()
    {
        return !string.IsNullOrWhiteSpace(Host)
               && Port is > 0 and <= 65535
               && TimeoutSeconds > 0;
    }

    private void NotifyCommandStatesChanged()
    {
        PlayCommand.NotifyCanExecuteChanged();
        PauseCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        SeekCommand.NotifyCanExecuteChanged();
        RefreshStatusCommand.NotifyCanExecuteChanged();
        SetPackagesCommand.NotifyCanExecuteChanged();
    }

    private static async Task<string?> PickPackageFileAsync(string title)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
            || desktop.MainWindow is not { } window)
            return null;

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Gorge 包")
                {
                    Patterns = new[] { "*.zip", "*.gpkg" }
                }
            }
        });

        return files.Count == 0 ? null : files[0].Path.LocalPath;
    }

    private static IReadOnlyList<string> ParsePathList(string text)
    {
        return text.Split(new[] { "\r\n", "\n", ";" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();
    }

    private static string FormatSeconds(double seconds)
    {
        return seconds.ToString("0.000s", CultureInfo.InvariantCulture);
    }
}
