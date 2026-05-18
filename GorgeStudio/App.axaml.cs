using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using GorgeStudio.Services;
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

            // IEmbedService 需要 View 层控件，使用实例注册
            services.AddSingleton<IEmbedService>(
                new EmbedService(mainWindow.EmbedHostControl, mainWindow));

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
