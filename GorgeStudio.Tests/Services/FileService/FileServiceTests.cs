using System.IO.Compression;
using GorgeStudio.Models;
using GorgeStudio.Services;
using Xunit;

namespace GorgeStudio.Tests.Services.FileService;

public class FileServiceTests : IDisposable
{
    private readonly GorgeStudio.Services.FileService.FileService _fileService;
    private readonly List<string> _tempFiles = new();
    private readonly List<string> _tempDirs = new();

    public FileServiceTests()
    {
        _fileService = new GorgeStudio.Services.FileService.FileService();
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

    #region Helpers

    private string CreateTempFile(string content, string? fileName = null)
    {
        var path = Path.Combine(Path.GetTempPath(), fileName ?? $"test_{Guid.NewGuid():N}.g");
        File.WriteAllText(path, content);
        _tempFiles.Add(path);
        return path;
    }

    private string CreateTempDirectory(params (string fileName, string content)[] files)
    {
        var dir = Path.Combine(Path.GetTempPath(), $"testdir_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        _tempDirs.Add(dir);
        foreach (var (fileName, content) in files)
        {
            var path = Path.Combine(dir, fileName);
            File.WriteAllText(path, content);
            _tempFiles.Add(path);
        }
        return dir;
    }

    private string CreateTempZip(params string[] fileNames)
    {
        var zipPath = Path.Combine(Path.GetTempPath(), $"testzip_{Guid.NewGuid():N}.zip");
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (var fileName in fileNames)
        {
            archive.CreateEntry(fileName);
        }
        _tempFiles.Add(zipPath);
        return zipPath;
    }

    private string CreateTempZipWithContent(params (string entryName, string content)[] entries)
    {
        var zipPath = Path.Combine(Path.GetTempPath(), $"testzip_{Guid.NewGuid():N}.zip");
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (var (entryName, content) in entries)
        {
            var entry = archive.CreateEntry(entryName);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }
        _tempFiles.Add(zipPath);
        return zipPath;
    }

    private static void AssertSuccess(CompileResult result)
    {
        Assert.True(result.Success, result.ErrorMessage ?? "Expected success but got failure.");
        Assert.NotNull(result.Project);
    }

    private static CompiledClassInfo FindClass(CompiledProject project, string className)
    {
        var cls = project.Classes.FirstOrDefault(c => c.ClassName == className);
        Assert.NotNull(cls);
        return cls!;
    }

    #endregion

    #region Compilation Tests

    private const string SimpleClass = @"
class Calculator {
    int Add(int a, int b) {
        return a + b;
    }
}";

    [Fact]
    public async Task CompileSimpleClass_ReturnsSuccess()
    {
        var path = CreateTempFile(SimpleClass);
        var result = await _fileService.LoadAndCompileFileAsync(path);

        AssertSuccess(result);
        Assert.NotEmpty(result.Project!.Classes);
        Assert.Single(result.SourceFilePaths);
    }

    [Fact]
    public async Task CompileSimpleClass_HasCorrectClassName()
    {
        var path = CreateTempFile(SimpleClass);
        var result = await _fileService.LoadAndCompileFileAsync(path);

        var cls = FindClass(result.Project!, "Calculator");
        Assert.Equal("Calculator", cls.ClassName);
        Assert.Equal("Calculator", cls.FullName);
    }

    [Fact]
    public async Task CompileSimpleClass_MapsMethodCorrectly()
    {
        var path = CreateTempFile(SimpleClass);
        var result = await _fileService.LoadAndCompileFileAsync(path);

        var cls = FindClass(result.Project!, "Calculator");
        Assert.Single(cls.Methods);
        Assert.Equal("Add", cls.Methods[0].Name);
        Assert.Equal("int", cls.Methods[0].ReturnType);
        Assert.Equal(2, cls.Methods[0].Parameters.Count);
        Assert.Equal("a", cls.Methods[0].Parameters[0].Name);
        Assert.Equal("int", cls.Methods[0].Parameters[0].Type);
        Assert.Equal("b", cls.Methods[0].Parameters[1].Name);
    }

    [Fact]
    public async Task CompileSimpleClass_IsChartCodeByDefault()
    {
        var path = CreateTempFile(SimpleClass);
        var result = await _fileService.LoadAndCompileFileAsync(path, isChart: true);

        var cls = FindClass(result.Project!, "Calculator");
        Assert.True(cls.IsChartCode);
        Assert.Single(result.Project!.ChartClasses);
        Assert.Empty(result.Project!.LibraryClasses);
    }

    [Fact]
    public async Task CompileSimpleClass_IsNotChartCodeWhenFalse()
    {
        var path = CreateTempFile(SimpleClass);
        var result = await _fileService.LoadAndCompileFileAsync(path, isChart: false);

        var cls = FindClass(result.Project!, "Calculator");
        Assert.False(cls.IsChartCode);
    }

    private const string ClassWithFields = @"
class Player {
    int Health;
    float Speed;
    string Name;
}";

    [Fact]
    public async Task CompileClassWithFields_MapsFieldsCorrectly()
    {
        var path = CreateTempFile(ClassWithFields);
        var result = await _fileService.LoadAndCompileFileAsync(path);

        var cls = FindClass(result.Project!, "Player");
        Assert.Equal(3, cls.Fields.Count);

        var health = cls.Fields.First(f => f.Name == "Health");
        Assert.Equal("int", health.Type);
        Assert.Equal(0, health.Index);

        var speed = cls.Fields.First(f => f.Name == "Speed");
        Assert.Equal("float", speed.Type);

        var name = cls.Fields.First(f => f.Name == "Name");
        Assert.Equal("string", name.Type);
    }

    private const string ClassWithStaticMethod = @"
class Utils {
    static int Square(int x) {
        return x * x;
    }

    int InstanceMethod() {
        return 42;
    }
}";

    [Fact]
    public async Task CompileClassWithStaticMethod_SeparatesStaticFromInstance()
    {
        var path = CreateTempFile(ClassWithStaticMethod);
        var result = await _fileService.LoadAndCompileFileAsync(path);

        var cls = FindClass(result.Project!, "Utils");
        Assert.Single(cls.Methods);
        Assert.Equal("InstanceMethod", cls.Methods[0].Name);
        Assert.Single(cls.StaticMethods);
        Assert.Equal("Square", cls.StaticMethods[0].Name);
        Assert.Equal("int", cls.StaticMethods[0].ReturnType);
    }

    private const string ClassWithConstructor = @"
class Vec2 {
    float X;
    float Y;

    Vec2(float x, float y) {
        X = x;
        Y = y;
    }
}";

    [Fact]
    public async Task CompileClassWithConstructor_MapsConstructorCorrectly()
    {
        var path = CreateTempFile(ClassWithConstructor);
        var result = await _fileService.LoadAndCompileFileAsync(path);

        var cls = FindClass(result.Project!, "Vec2");
        Assert.Single(cls.Constructors);
        Assert.Equal(2, cls.Constructors[0].Parameters.Count);
        Assert.Equal("x", cls.Constructors[0].Parameters[0].Name);
        Assert.Equal("float", cls.Constructors[0].Parameters[0].Type);
    }

    #endregion

    #region Namespace Tests

    private const string NamespacedClass = @"
namespace Game.Entities;
class Enemy {
    int Damage;
}

namespace Game.Items;
class Weapon {
    int AttackPower;
}";

    [Fact]
    public async Task CompileNamespacedClasses_GroupsByNamespace()
    {
        var path = CreateTempFile(NamespacedClass);
        var result = await _fileService.LoadAndCompileFileAsync(path);

        Assert.True(result.Project!.ClassesByNamespace.ContainsKey("Game.Entities"));
        Assert.True(result.Project!.ClassesByNamespace.ContainsKey("Game.Items"));

        var entitiesClasses = result.Project!.ClassesByNamespace["Game.Entities"];
        Assert.Single(entitiesClasses);
        Assert.Equal("Enemy", entitiesClasses[0].ClassName);

        var itemsClasses = result.Project!.ClassesByNamespace["Game.Items"];
        Assert.Single(itemsClasses);
        Assert.Equal("Weapon", itemsClasses[0].ClassName);
    }

    [Fact]
    public async Task CompileNamespacedClasses_HasFullNameWithNamespace()
    {
        var path = CreateTempFile(NamespacedClass);
        var result = await _fileService.LoadAndCompileFileAsync(path);

        var enemy = FindClass(result.Project!, "Enemy");
        Assert.Equal("Game.Entities.Enemy", enemy.FullName);
        Assert.Equal("Game.Entities", enemy.Namespace);

        var weapon = FindClass(result.Project!, "Weapon");
        Assert.Equal("Game.Items.Weapon", weapon.FullName);
        Assert.Equal("Game.Items", weapon.Namespace);
    }

    #endregion

    #region Annotation Tests

    private const string ClassWithAnnotation = @"
@EditableElement(type = ""Note"")
class TapNote {
    float Time;
    float Position;
}

@Editable
class LaneConfig {
    float Width;
}";

    [Fact]
    public async Task CompileClassWithAnnotation_MapsAnnotationCorrectly()
    {
        var path = CreateTempFile(ClassWithAnnotation);
        var result = await _fileService.LoadAndCompileFileAsync(path);

        var tapNote = FindClass(result.Project!, "TapNote");
        Assert.Contains("EditableElement", tapNote.AnnotationNames);
        Assert.Single(tapNote.Annotations);
        Assert.Equal("EditableElement", tapNote.Annotations[0].Name);
        Assert.True(tapNote.Annotations[0].Parameters.ContainsKey("type"));
        Assert.Equal("Note", tapNote.Annotations[0].Parameters["type"]);

        var laneConfig = FindClass(result.Project!, "LaneConfig");
        Assert.Contains("Editable", laneConfig.AnnotationNames);
    }

    #endregion

    #region Enum Tests

    private const string SimpleEnum = @"
enum Difficulty {
    Easy,
    Normal,
    Hard
}";

    [Fact]
    public async Task CompileEnum_MapsValuesCorrectly()
    {
        var path = CreateTempFile(SimpleEnum);
        var result = await _fileService.LoadAndCompileFileAsync(path);

        Assert.NotEmpty(result.Project!.Enums);
        var difficulty = result.Project!.Enums.First(e => e.FullName == "Difficulty");
        Assert.Equal(3, difficulty.Values.Count);
        Assert.Contains("Easy", difficulty.Values);
        Assert.Contains("Normal", difficulty.Values);
        Assert.Contains("Hard", difficulty.Values);
    }

    #endregion

    #region Interface Tests

    private const string SimpleInterface = @"
interface IDrawable {
    void Draw();
    int GetZOrder();
}";

    [Fact]
    public async Task CompileInterface_MapsMethodsCorrectly()
    {
        var path = CreateTempFile(SimpleInterface);
        var result = await _fileService.LoadAndCompileFileAsync(path);

        Assert.NotEmpty(result.Project!.Interfaces);
        var iface = result.Project!.Interfaces.First(i => i.FullName == "IDrawable");
        Assert.Equal(2, iface.Methods.Count);
        Assert.Contains(iface.Methods, m => m.Name == "Draw" && m.ReturnType == null);
        Assert.Contains(iface.Methods, m => m.Name == "GetZOrder" && m.ReturnType == "int");
    }

    #endregion

    #region Inheritance Tests

    private const string ClassWithInheritance = @"
class Animal {
    string Name;

    void Speak() {}
}

class Dog : Animal {
    void Bark() {}
}";

    [Fact]
    public async Task CompileClassWithInheritance_RecordsSuperClass()
    {
        var path = CreateTempFile(ClassWithInheritance);
        var result = await _fileService.LoadAndCompileFileAsync(path);

        var dog = FindClass(result.Project!, "Dog");
        Assert.Equal("Animal", dog.SuperClassName);

        var animal = FindClass(result.Project!, "Animal");
        Assert.Null(animal.SuperClassName);
    }

    [Fact]
    public async Task CompileClassWithInheritance_CalculatesInheritanceDepth()
    {
        var path = CreateTempFile(ClassWithInheritance);
        var result = await _fileService.LoadAndCompileFileAsync(path);

        var animal = FindClass(result.Project!, "Animal");
        Assert.Equal(0, animal.InheritanceDepth);

        var dog = FindClass(result.Project!, "Dog");
        Assert.Equal(1, dog.InheritanceDepth);
    }

    #endregion

    #region Injector Field Tests

    private const string ClassWithInjectFields = @"
class Config {
    @Inject
    float Volume;

    @Inject(name = ""bgmPath"")
    string AudioFile;
}";

    [Fact]
    public async Task CompileClassWithInjectFields_MapsInjectorFields()
    {
        var path = CreateTempFile(ClassWithInjectFields);
        var result = await _fileService.LoadAndCompileFileAsync(path);

        var config = FindClass(result.Project!, "Config");
        Assert.NotEmpty(config.InjectorFields);
        Assert.Contains(config.InjectorFields, f => f.Name == "Volume");
        Assert.Contains(config.InjectorFields, f => f.Name == "bgmPath");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task LoadFile_NonExistentFile_ReturnsError()
    {
        var result = await _fileService.LoadAndCompileFileAsync(
            Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.g"));

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task CompileInvalidSyntax_ReturnsError()
    {
        var path = CreateTempFile("class");

        var result = await _fileService.LoadAndCompileFileAsync(path);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task LoadDirectory_NoGFiles_ReturnsError()
    {
        var dir = CreateTempDirectory(("readme.txt", "hello"));
        var result = await _fileService.LoadAndCompileDirectoryAsync(dir);

        Assert.False(result.Success);
        Assert.Contains("No .g source files", result.ErrorMessage);
    }

    [Fact]
    public async Task LoadZip_NoGFiles_ReturnsError()
    {
        var zipPath = CreateTempZip("readme.txt", "image.png");
        var result = await _fileService.LoadAndCompileZipAsync(zipPath);

        Assert.False(result.Success);
        Assert.Contains("No .g source files", result.ErrorMessage);
    }

    [Fact]
    public async Task Cancellation_ReturnsCancelledResult()
    {
        var path = CreateTempFile(SimpleClass);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await _fileService.LoadAndCompileFileAsync(path, ct: cts.Token);

        Assert.False(result.Success);
        Assert.Contains("cancell", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Directory Loading Tests

    private const string ClassA = @"
class ClassA {
    int Value;
}";

    private const string ClassB = @"
class ClassB {
    string Name;
}";

    [Fact]
    public async Task LoadDirectory_LoadsAllGFiles()
    {
        var dir = CreateTempDirectory(
            ("ClassA.g", ClassA),
            ("ClassB.g", ClassB),
            ("readme.txt", "ignore me"));

        var result = await _fileService.LoadAndCompileDirectoryAsync(dir);

        AssertSuccess(result);
        Assert.Equal(2, result.SourceFilePaths.Count);
        Assert.Contains(result.Project!.Classes, c => c.ClassName == "ClassA");
        Assert.Contains(result.Project!.Classes, c => c.ClassName == "ClassB");
    }

    [Fact]
    public async Task LoadDirectory_MarksAllFilesAsChart()
    {
        var dir = CreateTempDirectory(("ClassA.g", ClassA));
        var result = await _fileService.LoadAndCompileDirectoryAsync(dir, isChart: true);

        var cls = FindClass(result.Project!, "ClassA");
        Assert.True(cls.IsChartCode);
    }

    [Fact]
    public async Task LoadDirectory_NonRecursive_SkipsSubdirs()
    {
        var rootDir = CreateTempDirectory(("Root.g", SimpleClass));
        var subDir = Path.Combine(rootDir, "sub");
        Directory.CreateDirectory(subDir);
        _tempDirs.Add(rootDir); // already added, but include sub
        var subPath = Path.Combine(subDir, "Sub.g");
        File.WriteAllText(subPath, ClassA);
        _tempFiles.Add(subPath);

        var result = await _fileService.LoadAndCompileDirectoryAsync(rootDir, recursive: false);

        AssertSuccess(result);
        Assert.Contains(result.Project!.Classes, c => c.ClassName == "Calculator");
        Assert.DoesNotContain(result.Project!.Classes, c => c.ClassName == "ClassA");
    }

    #endregion

    #region Zip Loading Tests

    [Fact]
    public async Task LoadZip_LoadsGFiles()
    {
        var zipPath = CreateTempZipWithContent(
            ("chart/Score.g", SimpleClass),
            ("chart/Config.g", ClassA));

        var result = await _fileService.LoadAndCompileZipAsync(zipPath);

        AssertSuccess(result);
        Assert.Equal(2, result.SourceFilePaths.Count);
        Assert.Contains(result.Project!.Classes, c => c.ClassName == "Calculator");
        Assert.Contains(result.Project!.Classes, c => c.ClassName == "ClassA");
    }

    [Fact]
    public async Task LoadZip_MarksFilesAsChart()
    {
        var zipPath = CreateTempZipWithContent(("Score.g", SimpleClass));
        var result = await _fileService.LoadAndCompileZipAsync(zipPath, isChart: true);

        var cls = FindClass(result.Project!, "Calculator");
        Assert.True(cls.IsChartCode);
    }

    [Fact]
    public async Task LoadZip_FromBytes_WorksCorrectly()
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("Test.g");
            using var writer = new StreamWriter(entry.Open());
            writer.Write(SimpleClass);
        }

        var result = await _fileService.LoadAndCompileZipAsync(ms.ToArray());

        AssertSuccess(result);
        Assert.Contains(result.Project!.Classes, c => c.ClassName == "Calculator");
    }

    #endregion

    #region Output Structure Tests

    private const string MultiTypeSource = @"
namespace Game;
class Player {
    int Health;
}

enum GameState {
    Playing,
    Paused,
    GameOver
}

interface IUpdatable {
    void Update(float dt);
}";

    [Fact]
    public async Task CompileMultiType_HasClassesEnumsAndInterfaces()
    {
        var path = CreateTempFile(MultiTypeSource);
        var result = await _fileService.LoadAndCompileFileAsync(path);

        Assert.NotEmpty(result.Project!.Classes);
        Assert.NotEmpty(result.Project!.Enums);
        Assert.NotEmpty(result.Project!.Interfaces);
    }

    [Fact]
    public async Task CompileMultiType_ClassesByNamespaceIsCorrect()
    {
        var path = CreateTempFile(MultiTypeSource);
        var result = await _fileService.LoadAndCompileFileAsync(path);

        Assert.True(result.Project!.ClassesByNamespace.ContainsKey("Game"));
        Assert.Single(result.Project!.ClassesByNamespace["Game"]);
        Assert.Equal("Player", result.Project!.ClassesByNamespace["Game"][0].ClassName);
    }

    [Fact]
    public async Task CompileMultiType_ChartAndLibraryClassesAreSeparated()
    {
        var path = CreateTempFile(MultiTypeSource);
        var result = await _fileService.LoadAndCompileFileAsync(path, isChart: true);

        // The Player class should be in ChartClasses since isChart=true
        Assert.NotEmpty(result.Project!.ChartClasses);
        Assert.Contains(result.Project!.ChartClasses, c => c.ClassName == "Player");
    }

    #endregion

    #region Compile Result Metadata Tests

    [Fact]
    public async Task CompileResult_HasSourceFilePaths()
    {
        var path = CreateTempFile(SimpleClass);
        var result = await _fileService.LoadAndCompileFileAsync(path);

        Assert.Single(result.SourceFilePaths);
        Assert.Equal(path, result.SourceFilePaths[0]);
    }

    [Fact]
    public async Task CompileResult_HasCompileTime()
    {
        var path = CreateTempFile(SimpleClass);
        var result = await _fileService.LoadAndCompileFileAsync(path);

        Assert.True(result.CompileTime > TimeSpan.Zero);
    }

    #endregion

    #region StatusChanged Event Tests

    [Fact]
    public async Task StatusChanged_FiresDuringCompilation()
    {
        var statusMessages = new List<string>();
        _fileService.StatusChanged += msg => statusMessages.Add(msg);

        var path = CreateTempFile(SimpleClass);
        await _fileService.LoadAndCompileFileAsync(path);

        Assert.NotEmpty(statusMessages);
        Assert.Contains(statusMessages, m => m.Contains("Loading"));
        Assert.Contains(statusMessages, m => m.Contains("Compil"));
        Assert.Contains(statusMessages, m => m.Contains("complete"));
    }

    #endregion
}
