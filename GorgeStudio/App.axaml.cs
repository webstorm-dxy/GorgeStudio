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
using GorgeStudio.Services.FileService;
using GorgeStudio.Services.ChartService;
using GorgeStudio.Services.CodeGeneration;
using GorgeStudio.Services.Packaging;
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

            var mainWindow = new MainWindow();

            var services = new ServiceCollection();

            // 平台嵌入器工厂
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                services.AddSingleton<Func<IWindowEmbedder>>(_ => () => new Win32WindowEmbedder());
            }
            else
            {
                services.AddSingleton<Func<IWindowEmbedder>>(_ => () => new MacWindowEmbedder());
            }

            // 嵌入服务
            services.AddSingleton<IEmbedService>(sp =>
            {
                var factory = sp.GetRequiredService<Func<IWindowEmbedder>>();
                return new EmbedService(mainWindow.EmbedHostControl, mainWindow, factory);
            });

            // 文件服务
            services.AddSingleton<IFileService>(_ => new GorgeStudio.Services.FileService.FileService());

            // 谱面服务
            services.AddSingleton<IChartService, ChartService>();

            // 源码生成和打包
            services.AddSingleton<IGorgeCodeGenerator, GorgeCodeGenerator>();
            services.AddSingleton<IPackageWriter, PackageWriter>();

            // 项目设置服务
            services.AddSingleton<IProjectSettingsService, ProjectSettingsService>();

            // 面板 ViewModel
            services.AddTransient<ProjectSettingsWindowViewModel>();
            services.AddTransient<ElementListPanelViewModel>();
            services.AddTransient<PropertiesPanelViewModel>();
            services.AddTransient<TimelinePanelViewModel>();

            // 主窗口 ViewModel（注入所有依赖）
            services.AddTransient<MainWindowViewModel>();

            _serviceProvider = services.BuildServiceProvider();

            var mainWindowViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            mainWindow.DataContext = mainWindowViewModel;

            desktop.MainWindow = mainWindow;

            // 启动时自动加载 Assets/Forms 目录下的所有 .g 源文件
            mainWindow.Opened += async (_, _) =>
            {
                await mainWindowViewModel.AutoLoadAsync();
            };

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
