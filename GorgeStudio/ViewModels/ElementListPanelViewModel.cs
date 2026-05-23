using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Gorge.Native.Gorge;
using GorgeStudio.Models;
using GorgeStudio.Models.Chart;

namespace GorgeStudio.ViewModels;

public partial class ElementListPanelViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "Element List";

    [ObservableProperty]
    private string _description = "Elements in the current scene.";

    [ObservableProperty]
    private TreeNode? _selectedItem;

    /// <summary>
    /// TreeView 绑定的根节点集合。
    /// </summary>
    public ObservableCollection<TreeNode> RootNodes { get; } = new();

    /// <summary>
    /// 从 CompiledProject 构建 TreeView 数据树。
    /// 层级：Namespace → Class/Enum/Interface → Members
    /// </summary>
    public void LoadProject(CompiledProject project)
    {
        RootNodes.Clear();

        // 按命名空间分组构建树
        foreach (var (ns, classes) in project.ClassesByNamespace.OrderBy(kv => kv.Key))
        {
            var nsNode = CreateNamespaceNode(ns, classes, project.Enums, project.Interfaces);
            RootNodes.Add(nsNode);
        }

        // 无命名空间的顶层类型（归入 "(global)" 节点）
        var unnamespacedClasses = project.Classes
            .Where(c => string.IsNullOrEmpty(c.Namespace))
            .ToList();
        var unnamespacedEnums = project.Enums
            .Where(e => string.IsNullOrEmpty(e.Namespace))
            .ToList();
        var unnamespacedInterfaces = project.Interfaces
            .Where(i => string.IsNullOrEmpty(i.Namespace))
            .ToList();

        if (unnamespacedClasses.Count > 0 || unnamespacedEnums.Count > 0 || unnamespacedInterfaces.Count > 0)
        {
            RootNodes.Add(CreateNamespaceNode("(global)", unnamespacedClasses, unnamespacedEnums, unnamespacedInterfaces));
        }

        Description = project.Classes.Count > 0
            ? $"{project.Classes.Count} classes, {project.Enums.Count} enums, {project.Interfaces.Count} interfaces"
            : "No elements loaded.";
    }

    /// <summary>
    /// 从 SimulationScore 构建 Staff → Period → Element 层级树。
    /// 添加到现有 RootNodes 的末尾。
    /// </summary>
    public void LoadSimulationScore(SimulationScore score)
    {
        foreach (var staff in score.Stave)
        {
            var staffNode = CreateStaffNode(staff);
            RootNodes.Add(staffNode);
        }

        if (score.Stave.Count > 0)
            Description = $"{score.Stave.Count} staff(s), {score.Stave.Sum(s => s.Periods.Count())} period(s) loaded.";
    }

    private static TreeNode CreateStaffNode(IStaff staff)
    {
        var prefix = staff is ElementStaff ? "[ElementStaff]" : "[AudioStaff]";
        var node = new TreeNode
        {
            DisplayName = $"{prefix} {staff.DisplayName} ({staff.ClassName})",
            FullName = staff.ClassName,
            Category = TreeNodeCategory.Staff,
            Tag = staff,
            IsExpanded = true
        };

        foreach (var period in staff.Periods)
        {
            var periodNode = CreatePeriodNode(period);
            node.Children.Add(periodNode);
        }

        return node;
    }

    private static TreeNode CreatePeriodNode(IPeriod period)
    {
        var prefix = period is ElementPeriod ? "[Chart]" : "[Song]";
        var node = new TreeNode
        {
            DisplayName = $"{prefix} {period.MethodName} (offset={period.TimeOffset:F2})",
            FullName = period.MethodName,
            Category = TreeNodeCategory.Period,
            Tag = period,
            IsExpanded = true
        };

        if (period is ElementPeriod elementPeriod)
        {
            foreach (var element in elementPeriod.Elements)
            {
                var elementNode = CreateElementNode(element);
                node.Children.Add(elementNode);
            }
        }

        return node;
    }

    private static TreeNode CreateElementNode(Injector element)
    {
        var decl = element.InjectedClassDeclaration;
        var label = decl.Name;

        // Try to extract hitTime for display
        if (decl.TryGetInjectorFieldByName("hitTime", out var hitTimeField)
            && !element.GetInjectorFloatDefault(hitTimeField.Index))
        {
            var hitTime = element.GetInjectorFloat(hitTimeField.Index);
            label += $" @ {hitTime:F3}s";
        }

        return new TreeNode
        {
            DisplayName = label,
            FullName = decl.Name,
            Category = TreeNodeCategory.Element,
            Tag = element
        };
    }

    private static TreeNode CreateNamespaceNode(
        string ns,
        List<CompiledClassInfo> classes,
        IReadOnlyList<CompiledEnumInfo> enums,
        IReadOnlyList<CompiledInterfaceInfo> interfaces)
    {
        var node = new TreeNode
        {
            DisplayName = string.IsNullOrEmpty(ns) ? "(global)" : ns,
            FullName = ns,
            Category = TreeNodeCategory.Namespace,
            IsExpanded = true
        };

        // 类（谱面类在前）
        foreach (var cls in classes.OrderByDescending(c => c.IsChartCode).ThenBy(c => c.ClassName))
        {
            node.Children.Add(CreateClassNode(cls));
        }

        // 枚举
        foreach (var e in enums.Where(e => e.Namespace == ns))
        {
            node.Children.Add(CreateEnumNode(e));
        }

        // 接口
        foreach (var i in interfaces.Where(i => i.Namespace == ns))
        {
            node.Children.Add(CreateInterfaceNode(i));
        }

        return node;
    }

    private static TreeNode CreateClassNode(CompiledClassInfo cls)
    {
        var prefix = cls.IsChartCode ? "[Chart] " : "";
        var display = cls.SuperClassName != null
            ? $"{prefix}{cls.ClassName} : {cls.SuperClassName}"
            : $"{prefix}{cls.ClassName}";

        var node = new TreeNode
        {
            DisplayName = display,
            FullName = cls.FullName,
            Category = TreeNodeCategory.Class,
            Tag = cls,
            IsExpanded = cls.IsChartCode
        };

        // 注解
        foreach (var annotation in cls.Annotations)
        {
            node.Children.Add(new TreeNode
            {
                DisplayName = $"@{annotation.Name}",
                FullName = annotation.Name,
                Category = TreeNodeCategory.Annotation,
                Tag = annotation
            });
        }

        // 字段
        foreach (var field in cls.Fields)
        {
            node.Children.Add(new TreeNode
            {
                DisplayName = $"{field.Name} : {field.Type}",
                FullName = $"{cls.FullName}.{field.Name}",
                Category = TreeNodeCategory.Field,
                Tag = field
            });
        }

        // 注入字段
        foreach (var injectorField in cls.InjectorFields)
        {
            node.Children.Add(new TreeNode
            {
                DisplayName = $"[Inject] {injectorField.Name} : {injectorField.Type}",
                FullName = $"{cls.FullName}.{injectorField.Name}",
                Category = TreeNodeCategory.InjectorField,
                Tag = injectorField
            });
        }

        // 构造函数
        foreach (var ctor in cls.Constructors)
        {
            var paramList = string.Join(", ", ctor.Parameters.Select(p => $"{p.Name}: {p.Type}"));
            node.Children.Add(new TreeNode
            {
                DisplayName = $"ctor({paramList})",
                FullName = $"{cls.FullName}.ctor",
                Category = TreeNodeCategory.Constructor,
                Tag = ctor
            });
        }

        // 实例方法
        foreach (var method in cls.Methods)
        {
            var returnStr = method.ReturnType ?? "void";
            var paramList = string.Join(", ", method.Parameters.Select(p => $"{p.Name}: {p.Type}"));
            var annotationStr = method.Annotations.Count > 0
                ? string.Join(" ", method.Annotations.Select(a => $"@{a.Name}")) + " "
                : "";

            node.Children.Add(new TreeNode
            {
                DisplayName = $"{annotationStr}{method.Name}({paramList}) : {returnStr}",
                FullName = $"{cls.FullName}.{method.Name}",
                Category = TreeNodeCategory.Method,
                Tag = method
            });
        }

        // 静态方法
        foreach (var method in cls.StaticMethods)
        {
            var returnStr = method.ReturnType ?? "void";
            var paramList = string.Join(", ", method.Parameters.Select(p => $"{p.Name}: {p.Type}"));
            var annotationStr = method.Annotations.Count > 0
                ? string.Join(" ", method.Annotations.Select(a => $"@{a.Name}")) + " "
                : "";

            node.Children.Add(new TreeNode
            {
                DisplayName = $"static {annotationStr}{method.Name}({paramList}) : {returnStr}",
                FullName = $"{cls.FullName}.{method.Name}",
                Category = TreeNodeCategory.StaticMethod,
                Tag = method
            });
        }

        return node;
    }

    private static TreeNode CreateEnumNode(CompiledEnumInfo e)
    {
        var node = new TreeNode
        {
            DisplayName = e.FullName,
            FullName = e.FullName,
            Category = TreeNodeCategory.Enum,
            Tag = e
        };

        for (var i = 0; i < e.Values.Count; i++)
        {
            var display = e.DisplayNames.Count > i && !string.IsNullOrEmpty(e.DisplayNames[i])
                ? $"{e.Values[i]} = \"{e.DisplayNames[i]}\""
                : e.Values[i];

            node.Children.Add(new TreeNode
            {
                DisplayName = display,
                FullName = $"{e.FullName}.{e.Values[i]}",
                Category = TreeNodeCategory.Field
            });
        }

        return node;
    }

    private static TreeNode CreateInterfaceNode(CompiledInterfaceInfo i)
    {
        var node = new TreeNode
        {
            DisplayName = i.FullName,
            FullName = i.FullName,
            Category = TreeNodeCategory.Interface,
            Tag = i
        };

        foreach (var method in i.Methods)
        {
            var returnStr = method.ReturnType ?? "void";
            var paramList = string.Join(", ", method.Parameters.Select(p => $"{p.Name}: {p.Type}"));
            node.Children.Add(new TreeNode
            {
                DisplayName = $"{method.Name}({paramList}) : {returnStr}",
                FullName = $"{i.FullName}.{method.Name}",
                Category = TreeNodeCategory.Method,
                Tag = method
            });
        }

        return node;
    }
}
