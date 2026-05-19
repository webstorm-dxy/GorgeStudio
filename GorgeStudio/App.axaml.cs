using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using GorgeStudio.Interop;
using GorgeStudio.Services;
using GorgeStudio.Services.EmbedService;
using GorgeStudio.ViewModels;
using GorgeStudio.Views;

namespace GorgeStudio;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            // 1. 创建 View（EmbedService 依赖 MainWindow）
            var mainWindow = new MainWindow();

            // 2. 配置 DI 容器
            var services = new ServiceCollection();

            // 根据平台注册 IWindowEmbedder 工厂
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                services.AddSingleton<Func<IWindowEmbedder>>(_ => () => new Win32WindowEmbedder());
            }
            else
            {
                // macOS / Linux：以独立窗口方式启动
                services.AddSingleton<Func<IWindowEmbedder>>(_ => () => new MacWindowEmbedder());
            }

            // IEmbedService 需要 View 层控件，通过工厂方法解析 IWindowEmbedder 并注入
            services.AddSingleton<IEmbedService>(sp =>
            {
                var factory = sp.GetRequiredService<Func<IWindowEmbedder>>();
                return new EmbedService(mainWindow.EmbedHostControl, mainWindow, factory);
            });

            // ViewModel 由容器解析，自动注入 IEmbedService
            services.AddTransient<MainWindowViewModel>();

            _serviceProvider = services.BuildServiceProvider();

            // 3. 从容器解析 ViewModel 并绑定
            mainWindow.DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>();

            desktop.MainWindow = mainWindow;

            // 应用程序退出时释放容器
            desktop.Exit += (_, _) =>
            {
                _serviceProvider?.Dispose();
                _serviceProvider = null;
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
