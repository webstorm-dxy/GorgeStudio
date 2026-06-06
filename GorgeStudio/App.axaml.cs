using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using GorgeStudio.AppCore.DependencyInjection;
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

            var mainWindow = new MainWindow();

            var services = new ServiceCollection();

            // Register all AppCore services (FileService, ChartService, CodeGeneration,
            // Packaging, GodotRemote, workflow services, etc.)
            services.AddGorgeStudioAppCore();

            // Platform-specific window embedder factory
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                services.AddSingleton<Func<IWindowEmbedder>>(_ => () => new Win32WindowEmbedder());
            }
            else
            {
                services.AddSingleton<Func<IWindowEmbedder>>(_ => () => new MacWindowEmbedder());
            }

            // Embed service (Avalonia-dependent: needs Control + Window)
            services.AddSingleton<IEmbedService>(sp =>
            {
                var factory = sp.GetRequiredService<Func<IWindowEmbedder>>();
                return new EmbedService(mainWindow.EmbedHostControl, mainWindow, factory);
            });

            // ViewModels
            services.AddTransient<ProjectSettingsWindowViewModel>();
            services.AddTransient<ElementListPanelViewModel>();
            services.AddTransient<PropertiesPanelViewModel>();
            services.AddTransient<TimelinePanelViewModel>();
            services.AddTransient<MainWindowViewModel>();

            _serviceProvider = services.BuildServiceProvider();

            var mainWindowViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            mainWindow.DataContext = mainWindowViewModel;

            desktop.MainWindow = mainWindow;

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
