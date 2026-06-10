using Microsoft.Extensions.DependencyInjection;
using GorgeStudio.AppCore.Services;
using GorgeStudio.Services;
using GorgeStudio.Services.ChartService;
using GorgeStudio.Services.CodeGeneration;
using GorgeStudio.Services.FileService;
using GorgeStudio.Services.GodotRemote;
using GorgeStudio.Services.Packaging;

namespace GorgeStudio.AppCore.DependencyInjection;

public static class GorgeStudioAppCoreServiceCollectionExtensions
{
    public static IServiceCollection AddGorgeStudioAppCore(this IServiceCollection services)
    {
        // Base services
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IChartService, ChartService>();
        services.AddSingleton<IGorgeCodeGenerator, GorgeCodeGenerator>();
        services.AddSingleton<IPackageWriter, PackageWriter>();
        services.AddSingleton<IProjectSettingsService, ProjectSettingsService>();
        services.AddSingleton<IPeriodEditingService, PeriodEditingService>();

        // Godot communication
        services.AddSingleton(new GodotRemoteOptions());
        services.AddSingleton<IGodotRemoteClient, GodotRemoteClient>();

        // New AppCore services
        services.AddSingleton<IGodotProcessService, GodotProcessService>();
        services.AddSingleton<IChartWorkspaceService, ChartWorkspaceService>();
        services.AddSingleton<IFormsCatalogService, FormsCatalogService>();
        services.AddSingleton<IGodotLaunchWorkflow, GodotLaunchWorkflow>();
        services.AddSingleton<IPlaybackWorkflow, PlaybackWorkflow>();
        services.AddSingleton<ITimelineEditingWorkflow, TimelineEditingWorkflow>();
        services.AddSingleton<IPropertyInspectionService, PropertyInspectionService>();

        return services;
    }
}
