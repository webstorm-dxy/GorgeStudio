using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gorge.GorgeCompiler;
using Gorge.GorgeCompiler.CompileContext;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeLanguage.Objective;
using GorgeStudio.Models;

namespace GorgeStudio.Services.FileService;

public sealed class FileService : IFileService, IDisposable
{
    public event Action<string>? StatusChanged;

    public async Task<CompileResult> LoadAndCompileFileAsync(
        string filePath, bool isChart = true,
        IProgress<float>? progress = null, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            ReportStatus($"Loading file: {filePath}");
            var package = await LoadPackageFromFileAsync(filePath, isChart, ct);

            ReportStatus($"Compiling {package.SourceFiles.Count} source file(s)...");
            var project = await CompilePackageAsync(package, progress, ct);

            ReportStatus("Compilation complete.");
            return new CompileResult
            {
                Project = project,
                Success = true,
                SourceFilePaths = package.SourceFiles.Select(f => f.Path).ToList(),
                CompileTime = sw.Elapsed
            };
        }
        catch (OperationCanceledException)
        {
            return new CompileResult { Success = false, ErrorMessage = "Compilation cancelled.", CompileTime = sw.Elapsed };
        }
        catch (Exception ex)
        {
            return new CompileResult { Success = false, ErrorMessage = ex.Message, CompileTime = sw.Elapsed };
        }
    }

    public async Task<CompileResult> LoadAndCompileDirectoryAsync(
        string directoryPath, bool recursive = false, bool isChart = true,
        IProgress<float>? progress = null, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            ReportStatus($"Loading directory: {directoryPath}");
            var package = await LoadPackageFromDirectoryAsync(directoryPath, recursive, isChart, ct);

            if (package.SourceFiles.Count == 0)
            {
                return new CompileResult
                {
                    Success = false,
                    ErrorMessage = "No .g source files found in the directory.",
                    CompileTime = sw.Elapsed
                };
            }

            ReportStatus($"Compiling {package.SourceFiles.Count} source file(s)...");
            var project = await CompilePackageAsync(package, progress, ct);

            ReportStatus("Compilation complete.");
            return new CompileResult
            {
                Project = project,
                Success = true,
                SourceFilePaths = package.SourceFiles.Select(f => f.Path).ToList(),
                CompileTime = sw.Elapsed
            };
        }
        catch (OperationCanceledException)
        {
            return new CompileResult { Success = false, ErrorMessage = "Compilation cancelled.", CompileTime = sw.Elapsed };
        }
        catch (Exception ex)
        {
            return new CompileResult { Success = false, ErrorMessage = ex.Message, CompileTime = sw.Elapsed };
        }
    }

    public async Task<CompileResult> LoadAndCompileZipAsync(
        string zipFilePath, bool isChart = true,
        IProgress<float>? progress = null, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            ReportStatus($"Loading zip: {zipFilePath}");
            using var zipStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var package = LoadPackageFromZip(zipStream, zipFilePath, isChart);

            if (package.SourceFiles.Count == 0)
            {
                return new CompileResult
                {
                    Success = false,
                    ErrorMessage = "No .g source files found in the zip archive.",
                    CompileTime = sw.Elapsed
                };
            }

            ReportStatus($"Compiling {package.SourceFiles.Count} source file(s)...");
            var project = await CompilePackageAsync(package, progress, ct);

            ReportStatus("Compilation complete.");
            return new CompileResult
            {
                Project = project,
                Success = true,
                SourceFilePaths = package.SourceFiles.Select(f => f.Path).ToList(),
                CompileTime = sw.Elapsed
            };
        }
        catch (OperationCanceledException)
        {
            return new CompileResult { Success = false, ErrorMessage = "Compilation cancelled.", CompileTime = sw.Elapsed };
        }
        catch (Exception ex)
        {
            return new CompileResult { Success = false, ErrorMessage = ex.Message, CompileTime = sw.Elapsed };
        }
    }

    public async Task<CompileResult> LoadAndCompileZipAsync(
        byte[] zipData, bool isChart = true,
        IProgress<float>? progress = null, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            ReportStatus("Loading zip from memory...");
            using var zipStream = new MemoryStream(zipData);
            var package = LoadPackageFromZip(zipStream, "<memory>", isChart);

            if (package.SourceFiles.Count == 0)
            {
                return new CompileResult
                {
                    Success = false,
                    ErrorMessage = "No .g source files found in the zip data.",
                    CompileTime = sw.Elapsed
                };
            }

            ReportStatus($"Compiling {package.SourceFiles.Count} source file(s)...");
            var project = await CompilePackageAsync(package, progress, ct);

            ReportStatus("Compilation complete.");
            return new CompileResult
            {
                Project = project,
                Success = true,
                SourceFilePaths = package.SourceFiles.Select(f => f.Path).ToList(),
                CompileTime = sw.Elapsed
            };
        }
        catch (OperationCanceledException)
        {
            return new CompileResult { Success = false, ErrorMessage = "Compilation cancelled.", CompileTime = sw.Elapsed };
        }
        catch (Exception ex)
        {
            return new CompileResult { Success = false, ErrorMessage = ex.Message, CompileTime = sw.Elapsed };
        }
    }

    public void Dispose()
    {
        StatusChanged = null;
    }

    #region File Loading

    private static async Task<GorgePackage> LoadPackageFromFileAsync(string filePath, bool isChart,
        CancellationToken ct = default)
    {
        var fullPath = Path.GetFullPath(filePath);
        var code = await File.ReadAllTextAsync(fullPath, ct);
        var sourceFile = new SourceCodeFile(fullPath, code, isChart);

        return new GorgePackage
        {
            SourcePath = fullPath,
            SourceFiles = new[] { sourceFile },
            SourcePathIsChart = new Dictionary<string, bool> { { fullPath, isChart } }
        };
    }

    private static async Task<GorgePackage> LoadPackageFromDirectoryAsync(
        string directoryPath, bool recursive, bool isChart, CancellationToken ct = default)
    {
        var fullPath = Path.GetFullPath(directoryPath);
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var filePaths = Directory.EnumerateFiles(fullPath, "*.g", searchOption).ToList();

        var sourceFiles = new List<SourceCodeFile>();
        var assetPaths = new List<string>();
        var sourcePathIsChart = new Dictionary<string, bool>();

        foreach (var filePath in filePaths)
        {
            var code = await File.ReadAllTextAsync(filePath, ct);
            sourceFiles.Add(new SourceCodeFile(filePath, code, isChart));
            sourcePathIsChart[filePath] = isChart;
        }

        // Collect non-.g files as assets
        var allFiles = Directory.EnumerateFiles(fullPath, "*.*", searchOption);
        foreach (var file in allFiles)
        {
            if (!file.EndsWith(".g", StringComparison.OrdinalIgnoreCase))
            {
                assetPaths.Add(file);
            }
        }

        return new GorgePackage
        {
            SourcePath = fullPath,
            SourceFiles = sourceFiles,
            AssetPaths = assetPaths,
            SourcePathIsChart = sourcePathIsChart
        };
    }

    private static GorgePackage LoadPackageFromZip(Stream zipStream, string sourceName, bool isChart)
    {
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false);
        var sourceFiles = new List<SourceCodeFile>();
        var assetPaths = new List<string>();
        var sourcePathIsChart = new Dictionary<string, bool>();

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name))
                continue;

            var entryPath = $"{sourceName}/{entry.FullName}";

            if (entry.Name.EndsWith(".g", StringComparison.OrdinalIgnoreCase))
            {
                using var reader = new StreamReader(entry.Open());
                var code = reader.ReadToEnd();
                sourceFiles.Add(new SourceCodeFile(entryPath, code, isChart));
                sourcePathIsChart[entryPath] = isChart;
            }
            else
            {
                assetPaths.Add(entryPath);
            }
        }

        return new GorgePackage
        {
            SourcePath = sourceName,
            SourceFiles = sourceFiles,
            AssetPaths = assetPaths,
            SourcePathIsChart = sourcePathIsChart
        };
    }

    #endregion

    #region Compilation

    private static async Task<CompiledProject> CompilePackageAsync(
        GorgePackage package, IProgress<float>? progress, CancellationToken ct)
    {
        // File loading takes 0-10%, compilation takes 10-100%
        progress?.Report(0.1f);

        var compileProgress = progress != null
            ? new Progress<float>(p => progress.Report(0.1f + p * 0.9f))
            : null;

        var context = await Compiler.CompileAsync(package.SourceFiles, compileProgress, ct);

        progress?.Report(1.0f);

        return ProcessCompilationContext(context, package);
    }

    private static CompiledProject ProcessCompilationContext(
        ClassImplementationContext context, GorgePackage package)
    {
        var classes = new List<CompiledClassInfo>();
        var enums = new List<CompiledEnumInfo>();
        var interfaces = new List<CompiledInterfaceInfo>();

        WalkNamespace(context.GlobalScope, classes, enums, interfaces, package.SourcePathIsChart);

        return CompiledProject.Create(classes, enums, interfaces);
    }

    private static void WalkNamespace(
        NamespaceScope ns,
        List<CompiledClassInfo> classes,
        List<CompiledEnumInfo> enums,
        List<CompiledInterfaceInfo> interfaces,
        Dictionary<string, bool> sourcePathIsChart)
    {
        foreach (var (_, classScope) in ns.Classes)
        {
            if (!classScope.IsNative)
            {
                classes.Add(MapClass(classScope, sourcePathIsChart));
            }
        }

        foreach (var (_, enumScope) in ns.Enums)
        {
            if (!enumScope.EnumSymbol.IsNative && enumScope.Enum != null)
            {
                enums.Add(MapEnum(enumScope, sourcePathIsChart));
            }
        }

        foreach (var (_, interfaceScope) in ns.Interfaces)
        {
            if (!interfaceScope.InterfaceSymbol.IsNative && interfaceScope.Interface != null)
            {
                interfaces.Add(MapInterface(interfaceScope, sourcePathIsChart));
            }
        }

        foreach (var (_, subNs) in ns.SubNamespaces)
        {
            WalkNamespace(subNs, classes, enums, interfaces, sourcePathIsChart);
        }
    }

    #endregion

    #region Mapping

    private static CompiledClassInfo MapClass(ClassScope classScope, Dictionary<string, bool> sourcePathIsChart)
    {
        var declaration = classScope.Declaration;
        var fullName = declaration.Type.FullName ?? declaration.Name;
        var (ns, className) = SplitFullName(fullName);

        var isChart = sourcePathIsChart.TryGetValue(classScope.SourceFilePath, out var chart) && chart;

        var depth = 0;
        var super = declaration.SuperClass;
        while (super != null)
        {
            depth++;
            super = super.SuperClass;
        }

        return new CompiledClassInfo
        {
            FullName = fullName,
            ClassName = className,
            Namespace = ns,
            IsNative = classScope.IsNative,
            IsChartCode = isChart,
            SuperClassName = declaration.SuperClass?.Type.FullName,
            InterfaceNames = declaration.SuperInterfaces.Select(i => i.Type.FullName ?? i.Name).ToList(),
            AnnotationNames = declaration.Annotations.Select(a => a.Name).ToList(),
            Fields = declaration.Fields.Select(MapField).ToList(),
            Methods = declaration.Methods.Select(MapMethod).ToList(),
            StaticMethods = declaration.StaticMethods.Select(MapMethod).ToList(),
            Constructors = declaration.Constructors.Select(MapConstructor).ToList(),
            InjectorFields = declaration.InjectorFields.Select(MapInjectorField).ToList(),
            Annotations = classScope.Annotations.Select(a => MapAnnotationFromScope(a)).ToList(),
            InheritanceDepth = depth
        };
    }

    private static CompiledEnumInfo MapEnum(EnumScope enumScope, Dictionary<string, bool> sourcePathIsChart)
    {
        var compiledEnum = enumScope.Enum;
        var fullName = compiledEnum.Type.FullName;
        var (ns, _) = SplitFullName(fullName ?? compiledEnum.Name);

        return new CompiledEnumInfo
        {
            FullName = fullName ?? compiledEnum.Name,
            Namespace = ns,
            IsNative = enumScope.EnumSymbol.IsNative,
            Values = compiledEnum.Values.ToList(),
            DisplayNames = compiledEnum.DisplayNames.ToList()
        };
    }

    private static CompiledInterfaceInfo MapInterface(InterfaceScope interfaceScope,
        Dictionary<string, bool> sourcePathIsChart)
    {
        var compiledInterface = interfaceScope.Interface;
        var fullName = compiledInterface.Type.FullName;
        var (ns, _) = SplitFullName(fullName ?? compiledInterface.Name);

        return new CompiledInterfaceInfo
        {
            FullName = fullName ?? compiledInterface.Name,
            Namespace = ns,
            IsNative = interfaceScope.InterfaceSymbol.IsNative,
            Methods = compiledInterface.Methods.Select(MapMethod).ToList()
        };
    }

    private static Models.FieldInfo MapField(Gorge.GorgeLanguage.Objective.FieldInformation field)
    {
        return new Models.FieldInfo
        {
            Id = field.Id,
            Name = field.Name,
            Type = TypeToString(field.Type),
            Index = field.Index,
            Annotations = field.Annotations?.Select(MapAnnotation).ToList()
                         ?? new List<AnnotationInfo>()
        };
    }

    private static Models.MethodInfo MapMethod(Gorge.GorgeLanguage.Objective.MethodInformation method)
    {
        return new Models.MethodInfo
        {
            Id = method.Id,
            Name = method.Name,
            ReturnType = method.ReturnType != null ? TypeToString(method.ReturnType) : null,
            Parameters = method.Parameters?.Select(MapParameter).ToList()
                         ?? new List<ParameterInfo>(),
            Annotations = method.Annotations?.Select(MapAnnotation).ToList()
                          ?? new List<AnnotationInfo>()
        };
    }

    private static Models.ConstructorInfo MapConstructor(
        Gorge.GorgeLanguage.Objective.ConstructorInformation ctor)
    {
        return new Models.ConstructorInfo
        {
            Id = ctor.Id,
            Parameters = ctor.Parameters?.Select(MapParameter).ToList()
                         ?? new List<ParameterInfo>(),
            Annotations = ctor.Annotations?.Select(MapAnnotation).ToList()
                          ?? new List<AnnotationInfo>()
        };
    }

    private static Models.InjectorFieldInfo MapInjectorField(
        Gorge.GorgeLanguage.Objective.InjectorFieldInformation field)
    {
        return new Models.InjectorFieldInfo
        {
            Id = field.Id,
            Name = field.Name,
            Type = TypeToString(field.Type),
            Index = field.Index,
            DefaultValueIndex = field.DefaultValueIndex
        };
    }

    private static ParameterInfo MapParameter(Gorge.GorgeLanguage.Objective.ParameterInformation param)
    {
        return new ParameterInfo
        {
            Id = param.Id,
            Name = param.Name,
            Type = TypeToString(param.Type),
            Index = param.Index
        };
    }

    private static AnnotationInfo MapAnnotationFromScope(AnnotationScope annotationScope)
    {
        var parameters = new Dictionary<string, object?>();

        // Read annotation parameters from Symbols (e.g., @Anno(key = "value"))
        foreach (var (paramName, symbol) in annotationScope.Symbols)
        {
            if (symbol is AnnotationParameterSymbol paramSymbol)
            {
                parameters[paramName] = paramSymbol.Value;
            }
        }

        // Read metadata entries
        foreach (var (metaName, metaSymbol) in annotationScope.MetadataScope.Symbols)
        {
            if (metaSymbol is MetadataEntrySymbol entrySymbol)
            {
                parameters[metaName] = entrySymbol.Metadata.Value;
            }
        }

        return new AnnotationInfo
        {
            Name = annotationScope.AnnotationIdentifier,
            GenericType = annotationScope.GenericType?.ToGorgeType()?.FullName,
            Parameters = parameters
        };
    }

    private static AnnotationInfo MapAnnotation(Gorge.GorgeLanguage.Objective.Annotation annotation)
    {
        var parameters = new Dictionary<string, object?>();

        // Read metadata entries
        foreach (var (key, metadata) in annotation.Metadatas)
        {
            parameters[key] = metadata.Value is string s ? s : metadata.Value;
        }

        // Annotation parameters are stored privately; try accessing via reflection
        var paramField = typeof(Gorge.GorgeLanguage.Objective.Annotation)
            .GetField("_parameters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (paramField?.GetValue(annotation) is Dictionary<string, object> privateParams)
        {
            foreach (var (key, value) in privateParams)
            {
                parameters[key] = value is string s ? s : value;
            }
        }

        return new AnnotationInfo
        {
            Name = annotation.Name,
            GenericType = annotation.GenericType?.FullName,
            Parameters = parameters
        };
    }

    #endregion

    #region Helpers

    private static string TypeToString(GorgeType type)
    {
        if (type.ClassName == null)
        {
            return type.BasicType switch
            {
                BasicType.Int => "int",
                BasicType.Float => "float",
                BasicType.Bool => "bool",
                BasicType.String => "string",
                _ => type.BasicType.ToString().ToLowerInvariant()
            };
        }

        return type.FullName ?? type.ClassName;
    }

    private static (string ns, string className) SplitFullName(string fullName)
    {
        var lastDot = fullName.LastIndexOf('.');
        if (lastDot < 0)
            return ("", fullName);
        return (fullName[..lastDot], fullName[(lastDot + 1)..]);
    }

    private void ReportStatus(string message)
    {
        StatusChanged?.Invoke(message);
    }

    #endregion
}
