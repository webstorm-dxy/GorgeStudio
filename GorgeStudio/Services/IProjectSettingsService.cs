using GorgeStudio.Models;

namespace GorgeStudio.Services;

public interface IProjectSettingsService
{
    ProjectSettings CurrentSettings { get; }
    void SaveSettings(ProjectSettings settings);
}
