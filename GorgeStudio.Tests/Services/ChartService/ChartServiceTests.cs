using System.IO.Compression;
using Gorge.GorgeCompiler;
using Gorge.GorgeLanguage.Objective;
using Gorge.Native.Gorge;
using GorgeStudio.Models;
using GorgeStudio.Models.Chart;
using GorgeStudio.Services.ChartService;
using GorgeStudio.Services.CodeGeneration;
using GorgeStudio.Services.FileService;
using GorgeStudio.Services.Packaging;
using Xunit;

namespace GorgeStudio.Tests.Services.ChartService;

public class ChartServiceTests : IDisposable
{
    private readonly GorgeStudio.Services.FileService.FileService _fileService;
    private readonly GorgeStudio.Services.ChartService.ChartService _chartService;
    private readonly IGorgeCodeGenerator _codeGenerator;
    private readonly IPackageWriter _packageWriter;
    private readonly List<string> _tempFiles = new();
    private readonly List<string> _tempDirs = new();

    public ChartServiceTests()
    {
        _fileService = new GorgeStudio.Services.FileService.FileService();
        _chartService = new GorgeStudio.Services.ChartService.ChartService();
        _codeGenerator = new GorgeCodeGenerator();
        _packageWriter = new PackageWriter();
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try { File.Delete(file); } catch { }
        }
        foreach (var dir in _tempDirs)
        {
            try { Directory.Delete(dir, recursive: true); } catch { }
        }
        _fileService.Dispose();
    }

    private string CreateTempFile(string content, string? fileName = null)
    {
        var path = Path.Combine(Path.GetTempPath(), fileName ?? $"test_{Guid.NewGuid():N}.g");
        File.WriteAllText(path, content);
        _tempFiles.Add(path);
        return path;
    }

    private static void AssertSuccess(CompileResult result)
    {
        Assert.True(result.Success, result.ErrorMessage ?? "Expected success but got failure.");
        Assert.NotNull(result.Project);
    }

    #region SimulationScore Model Tests

    [Fact]
    public void SimulationScore_Constructor_InitializesEmptyCollections()
    {
        var score = new SimulationScore(0f, 100f, 1f);

        Assert.Empty(score.Stave);
        Assert.Empty(score.ChartAssetFiles);
        Assert.Empty(score.AssetLoaders);
        Assert.Equal(0f, score.StartTime);
        Assert.Equal(100f, score.TerminateTime);
        Assert.Equal(1f, score.SimulationSpeed);
    }

    [Fact]
    public void ElementStaff_Constructor_SetsProperties()
    {
        var staff = new ElementStaff("TestStaff", true, "Test Display", "TestForm");

        Assert.Equal("TestStaff", staff.ClassName);
        Assert.True(staff.IsChartClass);
        Assert.Equal("Test Display", staff.DisplayName);
        Assert.Equal("TestForm", staff.FormName);
        Assert.Empty(staff.Periods);
    }

    [Fact]
    public void AudioStaff_Constructor_SetsProperties()
    {
        var staff = new AudioStaff("AudioStaff1", true, "Audio Display");

        Assert.Equal("AudioStaff1", staff.ClassName);
        Assert.True(staff.IsChartClass);
        Assert.Equal("Audio Display", staff.DisplayName);
        Assert.Empty(staff.Periods);
    }

    [Fact]
    public void ElementStaff_AddPeriod_IncreasesPeriodCount()
    {
        var staff = new ElementStaff("Test", true, "Test", "Form");
        var decl = CreateTestClassDeclaration();
        var config = new CompiledInjector(decl);
        var period = new ElementPeriod("Form", "Period1", config);

        staff.AddPeriod(period);

        Assert.Single(staff.Periods);
        Assert.Equal("Period1", staff.Periods[0].MethodName);
    }

    [Fact]
    public void ElementStaff_RemovePeriod_DecreasesPeriodCount()
    {
        var staff = new ElementStaff("Test", true, "Test", "Form");
        var decl = CreateTestClassDeclaration();
        var config = new CompiledInjector(decl);
        var period = new ElementPeriod("Form", "Period1", config);
        staff.AddPeriod(period);

        staff.RemovePeriod(period);

        Assert.Empty(staff.Periods);
    }

    [Fact]
    public void ElementPeriod_AddElement_IncreasesElementCount()
    {
        var decl = CreateTestClassDeclaration();
        var config = new CompiledInjector(decl);
        var period = new ElementPeriod("Form", "Period1", config);
        var element = new CompiledInjector(decl);

        period.Elements.Add(element);

        Assert.Single(period.Elements);
    }

    [Fact]
    public void ElementPeriod_RemoveElement_DecreasesElementCount()
    {
        var decl = CreateTestClassDeclaration();
        var config = new CompiledInjector(decl);
        var period = new ElementPeriod("Form", "Period1", config);
        var element = new CompiledInjector(decl);
        period.Elements.Add(element);

        period.Elements.RemoveAt(0);

        Assert.Empty(period.Elements);
    }

    [Fact]
    public void SimulationScore_CheckStaffNameConflict_ReturnsTrueForDuplicate()
    {
        var score = new SimulationScore(0f, 100f, 1f);
        var staff = new ElementStaff("MyStaff", true, "MyStaff", "Form");
        score.Stave.Add(staff);

        Assert.True(score.CheckStaffNameConflict("MyStaff"));
        Assert.False(score.CheckStaffNameConflict("OtherStaff"));
    }

    [Fact]
    public void SimulationScore_TryGetStaff_ReturnsCorrectStaff()
    {
        var score = new SimulationScore(0f, 100f, 1f);
        var staff = new ElementStaff("MyStaff", true, "MyStaff", "Form");
        score.Stave.Add(staff);

        Assert.True(score.TryGetStaff("MyStaff", out var found));
        Assert.Same(staff, found);
        Assert.False(score.TryGetStaff("NonExistent", out _));
    }

    [Fact]
    public void SimulationScore_TryGetPeriod_ReturnsCorrectPeriod()
    {
        var score = new SimulationScore(0f, 100f, 1f);
        var decl = CreateTestClassDeclaration();
        var config = new CompiledInjector(decl);
        var staff = new ElementStaff("MyStaff", true, "MyStaff", "Form");
        var period = new ElementPeriod("Form", "MyPeriod", config);
        staff.AddPeriod(period);
        score.Stave.Add(staff);

        Assert.True(score.TryGetPeriod("MyStaff", "MyPeriod", out var found));
        Assert.Same(period, found);
        Assert.False(score.TryGetPeriod("MyStaff", "NonExistent", out _));
    }

    [Fact]
    public void Staff_Clone_CreatesDeepCopy()
    {
        var decl = CreateTestClassDeclaration();
        var config = new CompiledInjector(decl);
        var staff = new ElementStaff("Original", true, "Original", "Form");
        var period = new ElementPeriod("Form", "Period1", config);
        var element = new CompiledInjector(decl);
        period.Elements.Add(element);
        staff.AddPeriod(period);

        var clone = (ElementStaff)((IStaff)staff).Clone();

        Assert.Equal("Original", clone.ClassName);
        Assert.Single(clone.Periods);
        Assert.Single(((ElementPeriod)clone.Periods[0]).Elements);
        Assert.NotSame(staff, clone);
        Assert.NotSame(period, clone.Periods[0]);
    }

    #endregion

    #region BuildFromClassDeclarations Tests

    private const string ElementStaffSource = @"
[
    string form = ""Dremu"",
    string displayName = ""Dremu谱表""
]
@ElementStaff
class DremuStaff
{
    int Health;
    float Speed;

    @Chart
    static int Period()
    {
        return 42;
    }

    void Update() {}
}";

    [Fact]
    public async Task BuildFromClassDeclarations_ThrowsWhenChartMissingConfig()
    {
        var path = CreateTempFile(ElementStaffSource);
        var result = await _fileService.LoadAndCompileFileAsync(path, isChart: true);
        AssertSuccess(result);

        var score = new SimulationScore(0f, 100f, 1f);
        Assert.Throws<Exception>(() => score.BuildFromClassDeclarations(result.ClassDeclarations!));
    }

    [Fact]
    public async Task ChartService_BuildChartDocumentAsync_SetsEnumValues()
    {
        var path = CreateTempFile(ElementStaffSource);
        var result = await _fileService.LoadAndCompileFileAsync(path, isChart: true);
        AssertSuccess(result);

        // ChartService sets EnumValues before building from declarations.
        // With missing config metadata, this should throw.
        await Assert.ThrowsAsync<Exception>(() =>
            _chartService.BuildChartDocumentAsync(result));
    }

    #endregion

    #region Code Generation Tests

    [Fact]
    public void ElementStaff_ToGorgeCode_ContainsAnnotationAndClassName()
    {
        var staff = new ElementStaff("TestStaff", true, "Test Display", "TestForm");

        var code = staff.ToGorgeCode();

        Assert.Contains("@ElementStaff", code);
        Assert.Contains("class TestStaff", code);
        Assert.Contains("TestForm", code);
        Assert.Contains("Test Display", code);
    }

    [Fact]
    public void AudioStaff_ToGorgeCode_ContainsAudioStaffAnnotation()
    {
        var staff = new AudioStaff("AudioClass", true, "Audio");

        var code = staff.ToGorgeCode();

        Assert.Contains("@AudioStaff", code);
        Assert.Contains("class AudioClass", code);
        Assert.Contains("Audio", code);
    }

    [Fact]
    public void ElementStaff_ToGorgeCode_WithPeriod_ContainsChartAnnotation()
    {
        var staff = new ElementStaff("TestStaff", true, "Test", "Form");
        var decl = CreateTestClassDeclaration();
        var config = new CompiledInjector(decl);
        var period = new ElementPeriod("Form", "MyPeriod", config);
        staff.AddPeriod(period);

        var code = staff.ToGorgeCode();

        Assert.Contains("@Chart", code);
        Assert.Contains("MyPeriod", code);
    }

    [Fact]
    public void GorgeCodeGenerator_Generate_ReturnsSourceFiles()
    {
        var score = new SimulationScore(0f, 100f, 1f);
        var staff = new ElementStaff("TestStaff", true, "Test", "Form");
        score.Stave.Add(staff);

        var files = _codeGenerator.Generate(score);

        Assert.Single(files);
        Assert.Equal("TestStaff.g", files[0].Path);
        Assert.True(files[0].IsChartSourceCode);
        Assert.Contains("@ElementStaff", files[0].Code);
    }

    [Fact]
    public void GorgeCodeGenerator_Generate_SkipsNonChartStaff()
    {
        var score = new SimulationScore(0f, 100f, 1f);
        var staff = new ElementStaff("LibraryStaff", false, "Library", "Form");
        score.Stave.Add(staff);

        var files = _codeGenerator.Generate(score);

        Assert.Empty(files);
    }

    [Fact]
    public void GorgeCodeGenerator_Generate_IncludesAssetLoaders()
    {
        var score = new SimulationScore(0f, 100f, 1f);
        var loader = new AssetLoader("AssetLoader", true);
        score.AssetLoaders.Add(loader);

        var files = _codeGenerator.Generate(score);

        Assert.Single(files);
        Assert.Equal("AssetLoader.g", files[0].Path);
        Assert.Contains("@AudioStaff", files[0].Code);
    }

    #endregion

    #region Package / ZIP Tests

    [Fact]
    public void PackageWriter_WriteZip_ContainsSourceFiles()
    {
        var sourceFiles = new List<SourceCodeFile>
        {
            new("chart/Score.g", "class Score {}", true),
            new("chart/Config.g", "class Config {}", true),
        };

        var zipData = _packageWriter.WriteZip(sourceFiles);

        Assert.True(zipData.Length > 0);
        using var ms = new MemoryStream(zipData);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        Assert.Equal(2, archive.Entries.Count);
        Assert.Contains(archive.Entries, e => e.Name == "Score.g");
        Assert.Contains(archive.Entries, e => e.Name == "Config.g");
    }

    [Fact]
    public void PackageWriter_WriteZip_SkipsNonChartSourceFiles()
    {
        var sourceFiles = new List<SourceCodeFile>
        {
            new("chart/Score.g", "class Score {}", true),
            new("lib/Util.g", "class Util {}", false),
        };

        var zipData = _packageWriter.WriteZip(sourceFiles);

        using var ms = new MemoryStream(zipData);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        Assert.Single(archive.Entries);
        Assert.Equal("Score.g", archive.Entries[0].Name);
    }

    [Fact]
    public void PackageWriter_WriteZip_IncludesAssetFiles()
    {
        var sourceFiles = new List<SourceCodeFile>
        {
            new("Score.g", "class Score {}", true),
        };
        var assetFiles = new List<AssetFile>
        {
            new("assets/sprite.png", new byte[] { 0x89, 0x50, 0x4E, 0x47 }, true),
            new("assets/sound.wav", new byte[] { 0x52, 0x49, 0x46, 0x46 }, true),
        };

        var zipData = _packageWriter.WriteZip(sourceFiles, assetFiles);

        using var ms = new MemoryStream(zipData);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        Assert.Equal(3, archive.Entries.Count);
        Assert.Contains(archive.Entries, e => e.Name == "sprite.png");
        Assert.Contains(archive.Entries, e => e.Name == "sound.wav");

        var pngEntry = archive.Entries.First(e => e.Name == "sprite.png");
        Assert.Equal(4, pngEntry.Length);
    }

    [Fact]
    public void PackageWriter_WriteZip_SkipsNonChartAssets()
    {
        var sourceFiles = new List<SourceCodeFile>
        {
            new("Score.g", "class Score {}", true),
        };
        var assetFiles = new List<AssetFile>
        {
            new("assets/game.png", new byte[] { 1, 2, 3 }, true),
            new("assets/editor.png", new byte[] { 4, 5, 6 }, false),
        };

        var zipData = _packageWriter.WriteZip(sourceFiles, assetFiles);

        using var ms = new MemoryStream(zipData);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
        Assert.Equal(2, archive.Entries.Count);
        Assert.Contains(archive.Entries, e => e.Name == "game.png");
        Assert.DoesNotContain(archive.Entries, e => e.Name == "editor.png");
    }

    [Fact]
    public void Package_RoundTrip_PreservesContent()
    {
        var originalCode = "class Test { int Value; }";
        var originalAssetData = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        var sourceFiles = new List<SourceCodeFile>
        {
            new("Test.g", originalCode, true),
        };
        var assetFiles = new List<AssetFile>
        {
            new("data.bin", originalAssetData, true),
        };

        var zipData = _packageWriter.WriteZip(sourceFiles, assetFiles);

        // Read back
        using var ms = new MemoryStream(zipData);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        var codeEntry = archive.GetEntry("Test.g");
        using var codeReader = new StreamReader(codeEntry!.Open());
        var code = codeReader.ReadToEnd();
        Assert.Equal(originalCode, code);

        var assetEntry = archive.GetEntry("data.bin");
        using var assetStream = assetEntry!.Open();
        var assetBytes = new byte[assetEntry.Length];
        assetStream.ReadExactly(assetBytes);
        Assert.Equal(originalAssetData, assetBytes);
    }

    #endregion

    #region AssetFile Tests

    [Fact]
    public void AssetFile_Clone_CreatesIndependentCopy()
    {
        var original = new AssetFile("test.png", new byte[] { 1, 2, 3 }, true);
        var clone = original.Clone();

        Assert.Equal(original.Path, clone.Path);
        Assert.Equal(original.Data, clone.Data);
        Assert.Equal(original.IsChartAsset, clone.IsChartAsset);
        Assert.NotSame(original.Data, clone.Data);
    }

    [Fact]
    public void AssetFile_Constructor_SetsAllProperties()
    {
        var data = new byte[] { 10, 20, 30 };
        var file = new AssetFile("path/to/file.png", data, true);

        Assert.Equal("path/to/file.png", file.Path);
        Assert.Equal(data, file.Data);
        Assert.True(file.IsChartAsset);
    }

    #endregion

    #region LoadScoreFromElementList Tests

    [Fact]
    public void LoadScoreFromElementList_CreatesCorrectStructure()
    {
        var decl = CreateTestClassDeclaration();
        var element1 = new CompiledInjector(decl);
        var element2 = new CompiledInjector(decl);
        var elements = new List<Injector> { element1, element2 };

        var score = SimulationScore.LoadScoreFromElementList(
            "Dremu", elements, new List<Injector>(), 0f, 100f, 1f);

        Assert.Single(score.Stave);
        Assert.IsType<ElementStaff>(score.Stave[0]);
        var staff = (ElementStaff)score.Stave[0];
        Assert.Single(staff.Periods);
        Assert.Equal(2, ((ElementPeriod)staff.Periods[0]).Elements.Count);
    }

    [Fact]
    public void LoadScoreFromElementList_ThrowsOnEmptyElements()
    {
        Assert.Throws<InvalidOperationException>(() =>
            SimulationScore.LoadScoreFromElementList(
                "Dremu", new List<Injector>(), new List<Injector>(), 0f, 100f, 1f));
    }

    [Fact]
    public void LoadScoreFromElementList_IncludesAssetInjectors()
    {
        var decl = CreateTestClassDeclaration();
        var element = new CompiledInjector(decl);
        var asset = new CompiledInjector(decl);

        var score = SimulationScore.LoadScoreFromElementList(
            "Dremu", new List<Injector> { element }, new List<Injector> { asset }, 0f, 100f, 1f);

        Assert.Single(score.AssetLoaders);
        Assert.Single(score.AssetLoaders[0].AssetSets);
        Assert.Single(score.AssetLoaders[0].AssetSets[0].Assets);
    }

    #endregion

    #region Period Tests

    [Fact]
    public void Period_TimeOffset_ReturnsConfigValue()
    {
        var decl = CreateTestClassDeclarationWithTimeOffset();
        var config = new CompiledInjector(decl);
        // Set timeOffset field value
        if (decl.TryGetInjectorFieldByName("timeOffset", out var field))
        {
            config.SetInjectorFloat(field.Index, 5.5f);
        }

        var period = new ElementPeriod("Form", "Method", config);

        Assert.Equal(5.5f, period.TimeOffset);
    }

    [Fact]
    public void Period_MinLength_ReturnsConfigValue()
    {
        var decl = CreateTestClassDeclarationWithTimeOffset();
        var config = new CompiledInjector(decl);
        if (decl.TryGetInjectorFieldByName("minLength", out var field))
        {
            config.SetInjectorFloat(field.Index, 20f);
        }

        var period = new ElementPeriod("Form", "Method", config);

        Assert.Equal(20f, period.MinLength);
    }

    [Fact]
    public void Period_UpdateConfig_ReplacesConfigInjector()
    {
        var decl = CreateTestClassDeclarationWithTimeOffset();
        var oldConfig = new CompiledInjector(decl);
        var newConfig = new CompiledInjector(decl);
        if (decl.TryGetInjectorFieldByName("timeOffset", out var field))
        {
            newConfig.SetInjectorFloat(field.Index, 10f);
        }

        var period = new ElementPeriod("Form", "Method", oldConfig);
        period.UpdateConfig(newConfig);

        Assert.Equal(10f, period.TimeOffset);
    }

    #endregion

    #region Helpers

    private const string SimpleInjectClass = @"
class TestElement {
    @Inject
    float Value;

    @Inject
    string Name;

    @Inject
    int Count;
}";

    private ClassDeclaration CreateTestClassDeclaration()
    {
        var path = CreateTempFile(SimpleInjectClass);
        var result = _fileService.LoadAndCompileFileAsync(path).Result;
        AssertSuccess(result);
        Assert.NotEmpty(result.ClassDeclarations!);
        return result.ClassDeclarations!.Values.First();
    }

    private const string ClassWithTimeOffset = @"
class PeriodConfig {
    @Inject
    float timeOffset;

    @Inject
    float minLength;
}";

    private ClassDeclaration CreateTestClassDeclarationWithTimeOffset()
    {
        var path = CreateTempFile(ClassWithTimeOffset);
        var result = _fileService.LoadAndCompileFileAsync(path).Result;
        AssertSuccess(result);
        Assert.NotEmpty(result.ClassDeclarations!);
        return result.ClassDeclarations!.Values.First();
    }

    #endregion
}
