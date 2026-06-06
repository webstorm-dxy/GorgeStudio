using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GorgeStudio.Models;
using GorgeStudio.Services;

namespace GorgeStudio.AppCore.Services;

public interface IFormsCatalogService
{
    Task<List<FormInfo>> DiscoverFormsAsync(string? formsRootPath = null);
    string? ResolveAssetFormsPath();
    List<FormInfo>? RestoreFormsFromSettings(List<string> formDirNames);
}

public sealed class FormsCatalogService : IFormsCatalogService
{
    private readonly IFileService _fileService;

    public FormsCatalogService(IFileService fileService)
    {
        _fileService = fileService;
    }

    public string? ResolveAssetFormsPath()
    {
        return ChartWorkspaceService.ResolveAssetFormsPath();
    }

    public async Task<List<FormInfo>> DiscoverFormsAsync(string? formsRootPath = null)
    {
        var path = formsRootPath ?? ResolveAssetFormsPath();
        if (path == null)
            return new List<FormInfo>();

        return await _fileService.DiscoverFormsAsync(path);
    }

    public List<FormInfo>? RestoreFormsFromSettings(List<string> formDirNames)
    {
        var formsPath = ResolveAssetFormsPath();
        if (formsPath == null)
            return null;

        var forms = new List<FormInfo>();
        foreach (var dirName in formDirNames)
        {
            var dirPath = System.IO.Path.Combine(formsPath, dirName);
            if (System.IO.Directory.Exists(dirPath))
                forms.Add(new FormInfo { DirectoryName = dirName, Path = dirPath });
        }

        return forms.Count > 0 ? forms : null;
    }
}
