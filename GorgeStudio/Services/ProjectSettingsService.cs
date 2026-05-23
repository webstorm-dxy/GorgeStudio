using GorgeStudio.Models;

namespace GorgeStudio.Services;

public class ProjectSettingsService : IProjectSettingsService
{
    public ProjectSettings CurrentSettings { get; private set; } = new();

    public void SaveSettings(ProjectSettings settings)
    {
        CurrentSettings = settings;
    }
}
