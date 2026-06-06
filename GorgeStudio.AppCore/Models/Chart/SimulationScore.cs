using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gorge.GorgeCompiler;
using Gorge.GorgeLanguage.Objective;
using Gorge.Native.Gorge;
using GorgeStudio.Services.CodeGeneration;

namespace GorgeStudio.Models.Chart;

/// <summary>
/// Gorge仿真总谱，包含来自所有模态和谱面的元素和资源信息。
/// 向上与运行时环境对接，向下与谱面文件互相转换。
/// </summary>
public class SimulationScore
{
    public float StartTime { get; }
    public float TerminateTime { get; }
    public float SimulationSpeed { get; }

    /// <summary>
    /// 谱面资源文件表
    /// </summary>
    public readonly List<AssetFile> ChartAssetFiles;

    /// <summary>
    /// 资源加载器表
    /// </summary>
    public readonly List<AssetLoader> AssetLoaders;

    /// <summary>
    /// 谱表列表
    /// </summary>
    public readonly List<IStaff> Stave;

    /// <summary>
    /// 所有运行时类声明映射（类完全限定名 → ClassDeclaration），包含 Native 类型。
    /// 供 PeriodEditingService 创建 GorgeFramework.PeriodConfig 等内建类型的 Injector。
    /// </summary>
    public IReadOnlyDictionary<string, ClassDeclaration>? ClassDeclarations { get; set; }

    public SimulationScore(float startTime, float terminateTime, float simulationSpeed)
    {
        StartTime = startTime;
        TerminateTime = terminateTime;
        SimulationSpeed = simulationSpeed;
        Stave = new List<IStaff>();
        ChartAssetFiles = new List<AssetFile>();
        AssetLoaders = new List<AssetLoader>();
    }

    public static SimulationScore LoadScoreFromElementList(string formName, List<Injector> elementInjectors,
        List<Injector> assetInjectors, float startTime, float terminateTime, float simulationSpeed)
    {
        var score = new SimulationScore(startTime, terminateTime, simulationSpeed);

        var staff = new ElementStaff("Chart", true, "Chart", formName);
        score.Stave.Add(staff);

        // Create a default PeriodConfig injector using the first available declaration
        Injector config;
        if (elementInjectors.Count > 0)
        {
            var decl = elementInjectors[0].InjectedClassDeclaration;
            config = new CompiledInjector(decl);
        }
        else
        {
            throw new InvalidOperationException("Cannot create period without elements.");
        }

        var period = new ElementPeriod(formName, "Period", config);
        foreach (var element in elementInjectors)
            period.Elements.Add(element);

        staff.AddPeriod(period);

        if (assetInjectors.Count > 0)
        {
            var assetLoader = new AssetLoader("Asset", true);
            score.AssetLoaders.Add(assetLoader);

            var assetSet = new AssetSet("AssetSet");
            assetLoader.AssetSets.Add(assetSet);

            foreach (var asset in assetInjectors)
                assetSet.Assets.Add(asset);
        }

        return score;
    }

    /// <summary>
    /// 从编译结果的 ClassDeclarations 构建谱表。
    /// 遍历每个类声明，根据注解识别 ElementStaff、AudioStaff、AssetLoader。
    /// </summary>
    public void BuildFromClassDeclarations(IReadOnlyDictionary<string, ClassDeclaration> classDeclarations)
    {
        Stave.Clear();

        foreach (var (_, classDecl) in classDeclarations)
        {
            if (classDecl.TryGetAnnotationByName("AudioStaff", out var audioStaffAnnotation))
            {
                var className = classDecl.Name;
                var displayName = className;
                if (audioStaffAnnotation.TryGetMetadata("displayName", out var displayNameObj))
                    displayName = (string)displayNameObj.Value;

                var staff = new AudioStaff(className, true, displayName);
                Stave.Add(staff);

                foreach (var method in classDecl.StaticMethods)
                {
                    if (method.TryGetAnnotationByName("Song", out var songAnnotation))
                    {
                        if (!songAnnotation.TryGetMetadata("config", out var configMetadata))
                            throw new Exception("Song注解没有名为config的元数据");

                        var periodConfig = (Injector)configMetadata.Value;
                        var returnTypeDecl = classDecl;
                        if (ClassDeclarations != null)
                        {
                            // method.ReturnType for Injector types has the inner type in SubTypes[0]
                            var typeToLookup = method.ReturnType;
                            if (typeToLookup?.SubTypes.Length > 0)
                                typeToLookup = typeToLookup.SubTypes[0];
                            if (typeToLookup != null && ClassDeclarations.TryGetValue(typeToLookup.FullName, out var resolvedDecl))
                                returnTypeDecl = resolvedDecl;
                        }
                        var audio = new CompiledInjector(returnTypeDecl);
                        var period = new AudioPeriod(method.Name, periodConfig, audio);
                        staff.Periods.Add(period);
                    }
                }
            }
            else if (classDecl.TryGetAnnotationByName("ElementStaff", out var elementStaffAnnotation))
            {
                var className = classDecl.Name;
                var displayName = className;
                if (elementStaffAnnotation.TryGetMetadata("displayName", out var displayNameObj))
                    displayName = (string)displayNameObj.Value;

                if (!elementStaffAnnotation.TryGetMetadata("form", out var formNameObj))
                    throw new Exception("ElementStaff注解没有名为form的元数据");

                var formName = (string)formNameObj.Value;
                var staff = new ElementStaff(className, true, displayName, formName);
                Stave.Add(staff);

                foreach (var method in classDecl.StaticMethods)
                {
                    if (method.TryGetAnnotationByName("Chart", out var chartAnnotation))
                    {
                        if (!chartAnnotation.TryGetMetadata("config", out var configMetadata))
                            throw new Exception("Chart注解没有名为config的元数据");

                        var periodConfig = (Injector)configMetadata.Value;
                        var block = new ElementPeriod(formName, method.Name, periodConfig);
                        staff.Periods.Add(block);

                        if (chartAnnotation.TryGetMetadata("elements", out var elementsMetadata)
                            && elementsMetadata.Value is ObjectList elementArray)
                        {
                            for (var i = 0; i < elementArray.length; i++)
                                block.Elements.Add((Injector)elementArray.Get(i));
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 从资源包中提取资产
    /// </summary>
    public void ExtractAssetsFromPackage(Package package)
    {
        ChartAssetFiles.AddRange(package.AssetFiles);
    }

    public async Task<Package> ExportChartPackage()
    {
        var stave = Stave.Where(s => s.IsChartClass).ToList();
        var assetLoaders = AssetLoaders.Where(a => a.IsChartClass).ToList();
        var chartAssetFiles = ChartAssetFiles.Where(f => f.IsChartAsset).ToList();

        await Task.Yield();

        var package = new Package();

        foreach (var staff in stave)
        {
            var clone = staff.Clone();
            package.SourceCodeFiles.Add(new SourceCodeFile(clone.ClassName + ".g", clone.ToGorgeCode(), true));
        }

        foreach (var assetLoader in assetLoaders)
        {
            var clone = assetLoader.Clone();
            package.SourceCodeFiles.Add(new SourceCodeFile(clone.ClassName + ".g", clone.ToGorgeCode(), true));
        }

        foreach (var chartAssetFile in chartAssetFiles)
            package.AssetFiles.Add(chartAssetFile.Clone());

        return package;
    }

    public bool TryGetStaff(string staffName, out IStaff staff)
    {
        staff = Stave.FirstOrDefault(s => s.ClassName == staffName);
        return staff != null;
    }

    public bool TryGetPeriod(string staffName, string periodName, out IPeriod period)
    {
        if (TryGetStaff(staffName, out var staff) && staff.TryGetPeriod(periodName, out period))
            return true;

        period = null!;
        return false;
    }

    /// <summary>
    /// 检查目标谱表名是否和已有谱表名冲突
    /// </summary>
    /// <returns>true表示冲突，不可添加该名字的谱表</returns>
    public bool CheckStaffNameConflict(string staffNameToInsert)
    {
        return Stave.Any(s => s.ClassName == staffNameToInsert);
    }
}

/// <summary>
/// 资源加载器，对应一个Gorge类
/// </summary>
public class AssetLoader
{
    public string ClassName { get; }
    public bool IsChartClass { get; }
    public List<AssetSet> AssetSets { get; }

    public AssetLoader(string className, bool isChartClass)
    {
        ClassName = className;
        IsChartClass = isChartClass;
        AssetSets = new List<AssetSet>();
    }

    public string ToGorgeCode()
    {
        if (!IsChartClass)
            throw new Exception("尝试将非谱面资源加载器转化为谱面代码");

        var sb = new StringBuilder();

        sb.AppendLine("@AudioStaff");
        sb.AppendLine($"class {ClassName}");
        sb.AppendLine("{");
        foreach (var assetSet in AssetSets)
        {
            sb.AppendLine(assetSet.ToGorgeCode(1));
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    public AssetLoader Clone()
    {
        var newAssetLoader = new AssetLoader(ClassName, IsChartClass);
        foreach (var assetSet in AssetSets)
            newAssetLoader.AssetSets.Add(assetSet.Clone());
        return newAssetLoader;
    }
}

/// <summary>
/// 资源组，对应一个Gorge方法
/// </summary>
public class AssetSet
{
    public string MethodName { get; }
    public List<Injector> Assets { get; }

    public AssetSet(string methodName)
    {
        MethodName = methodName;
        Assets = new List<Injector>();
    }

    public string ToGorgeCode(int indentation)
    {
        var sb = new StringBuilder();

        sb.AppendLine("@Asset", indentation);
        sb.AppendLine($"static Asset^[] {MethodName}()", indentation);
        sb.AppendLine("{", indentation);
        sb.AppendLine(
            $"return new Asset^[{Assets.Count}]{InjectorHardcodeGenerator.Generate("Asset^", Assets, false, indentation + 1)};",
            indentation + 1);
        sb.AppendLine("}", indentation);

        return sb.ToString();
    }

    public AssetSet Clone()
    {
        var newAssetSet = new AssetSet(MethodName);
        foreach (var asset in Assets)
            newAssetSet.Assets.Add((Injector)asset.Clone());
        return newAssetSet;
    }
}

/// <summary>
/// 资源文件
/// </summary>
public class AssetFile
{
    public string Path { get; }
    public byte[] Data { get; }
    public bool IsChartAsset { get; }

    public AssetFile(string path, byte[] data, bool isChartAsset)
    {
        Path = path;
        Data = data;
        IsChartAsset = isChartAsset;
    }

    public AssetFile Clone()
    {
        return new AssetFile(Path, (byte[])Data.Clone(), IsChartAsset);
    }
}

/// <summary>
/// 内存中的谱面包，包含源文件和资源文件。
/// </summary>
public class Package
{
    public List<AssetFile> AssetFiles { get; } = new();
    public List<SourceCodeFile> SourceCodeFiles { get; } = new();
}
