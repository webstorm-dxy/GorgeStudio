using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Gorge.GorgeLanguage.Objective;
using Gorge.Native.Gorge;
using GorgeStudio.Models;
using GorgeStudio.Models.Chart;

namespace GorgeStudio.ViewModels;

public partial class PropertiesPanelViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "Properties";

    [ObservableProperty]
    private string _description = "Select an object to view its properties.";

    [ObservableProperty]
    private object? _selectedObject;

    /// <summary>
    /// 当前选中对象的属性条目列表，绑定到属性面板的列表控件。
    /// </summary>
    public ObservableCollection<PropertyEntry> Properties { get; } = new();

    private SimulationScore? _simulationScore;

    /// <summary>
    /// 设置当前谱面文档，用于显示和编辑 Injector 值。
    /// </summary>
    public void SetChartDocument(SimulationScore? score)
    {
        _simulationScore = score;
        if (SelectedObject != null)
            OnSelectedObjectChanged(SelectedObject);
    }

    public void RefreshSelectedObject()
    {
        OnSelectedObjectChanged(SelectedObject);
    }

    partial void OnSelectedObjectChanged(object? value)
    {
        Properties.Clear();

        if (value == null)
        {
            Description = "Select an object to view its properties.";
            return;
        }

        switch (value)
        {
            case CompiledClassInfo cls:
                PopulateClass(cls);
                break;
            case CompiledEnumInfo e:
                PopulateEnum(e);
                break;
            case CompiledInterfaceInfo i:
                PopulateInterface(i);
                break;
            case FieldInfo field:
                PopulateField(field);
                break;
            case MethodInfo method:
                PopulateMethod(method);
                break;
            case ConstructorInfo ctor:
                PopulateConstructor(ctor);
                break;
            case InjectorFieldInfo injectorField:
                PopulateInjectorField(injectorField);
                break;
            case AnnotationInfo annotation:
                PopulateAnnotation(annotation);
                break;
            case IStaff staff:
                PopulateStaff(staff);
                break;
            case IPeriod period:
                PopulatePeriod(period);
                break;
            case Injector injector:
                PopulateInjector(injector);
                break;
            default:
                Description = $"Unknown object type: {value.GetType().Name}";
                break;
        }
    }

    private void PopulateClass(CompiledClassInfo cls)
    {
        Description = $"Class: {cls.FullName}";
        Add("Full Name", cls.FullName);
        Add("Class Name", cls.ClassName);
        Add("Namespace", string.IsNullOrEmpty(cls.Namespace) ? "(global)" : cls.Namespace);
        Add("Is Chart Code", cls.IsChartCode.ToString());
        Add("Is Native", cls.IsNative.ToString());
        Add("Inheritance Depth", cls.InheritanceDepth.ToString());
        if (cls.SuperClassName != null)
            Add("Super Class", cls.SuperClassName);
        if (cls.InterfaceNames.Count > 0)
            Add("Interfaces", string.Join(", ", cls.InterfaceNames));
        if (cls.AnnotationNames.Count > 0)
            Add("Annotations", string.Join(", ", cls.AnnotationNames));
        Add("Fields", cls.Fields.Count.ToString());
        Add("Methods", cls.Methods.Count.ToString());
        Add("Static Methods", cls.StaticMethods.Count.ToString());
        Add("Constructors", cls.Constructors.Count.ToString());
        Add("Injector Fields", cls.InjectorFields.Count.ToString());
    }

    private static string GetInjectorFieldValue(Injector injector, InjectorFieldInfo field)
    {
        var index = field.Index;
        try
        {
            return field.Type switch
            {
                "int" => injector.GetInjectorIntDefault(index)
                    ? "(default)" : injector.GetInjectorInt(index).ToString(),
                "float" => injector.GetInjectorFloatDefault(index)
                    ? "(default)" : injector.GetInjectorFloat(index).ToString(CultureInfo.InvariantCulture),
                "bool" => injector.GetInjectorBoolDefault(index)
                    ? "(default)" : injector.GetInjectorBool(index).ToString(),
                "string" => injector.GetInjectorStringDefault(index)
                    ? "(default)" : $"\"{injector.GetInjectorString(index)}\"",
                _ => injector.GetInjectorObjectDefault(index)
                    ? "(default)" : injector.GetInjectorObject(index)?.ToString() ?? "null"
            };
        }
        catch
        {
            return "(error)";
        }
    }

    private void PopulateEnum(CompiledEnumInfo e)
    {
        Description = $"Enum: {e.FullName}";
        Add("Full Name", e.FullName);
        Add("Namespace", string.IsNullOrEmpty(e.Namespace) ? "(global)" : e.Namespace);
        Add("Is Native", e.IsNative.ToString());
        Add("Values", string.Join(", ", e.Values));
        if (e.DisplayNames.Count > 0)
            Add("Display Names", string.Join(", ", e.DisplayNames));
    }

    private void PopulateInterface(CompiledInterfaceInfo i)
    {
        Description = $"Interface: {i.FullName}";
        Add("Full Name", i.FullName);
        Add("Namespace", string.IsNullOrEmpty(i.Namespace) ? "(global)" : i.Namespace);
        Add("Is Native", i.IsNative.ToString());
        Add("Methods", i.Methods.Count.ToString());
    }

    private void PopulateField(FieldInfo field)
    {
        Description = $"Field: {field.Name}";
        Add("Name", field.Name);
        Add("Type", field.Type);
        Add("ID", field.Id.ToString());
        Add("Index", field.Index.ToString());
        if (field.Annotations.Count > 0)
            Add("Annotations", string.Join(", ", field.Annotations.Select(a => a.Name)));
    }

    private void PopulateMethod(MethodInfo method)
    {
        Description = $"Method: {method.Name}";
        Add("Name", method.Name);
        Add("Return Type", method.ReturnType ?? "void");
        Add("ID", method.Id.ToString());
        if (method.Parameters.Count > 0)
        {
            for (var i = 0; i < method.Parameters.Count; i++)
            {
                var p = method.Parameters[i];
                Add($"  Parameter[{i}]", $"{p.Name}: {p.Type} (id={p.Id})");
            }
        }
        if (method.Annotations.Count > 0)
            Add("Annotations", string.Join(", ", method.Annotations.Select(a => a.Name)));
    }

    private void PopulateConstructor(ConstructorInfo ctor)
    {
        Description = "Constructor";
        Add("ID", ctor.Id.ToString());
        if (ctor.Parameters.Count > 0)
        {
            for (var i = 0; i < ctor.Parameters.Count; i++)
            {
                var p = ctor.Parameters[i];
                Add($"  Parameter[{i}]", $"{p.Name}: {p.Type} (id={p.Id})");
            }
        }
        if (ctor.Annotations.Count > 0)
            Add("Annotations", string.Join(", ", ctor.Annotations.Select(a => a.Name)));
    }

    private void PopulateInjectorField(InjectorFieldInfo field)
    {
        Description = $"Injector Field: {field.Name}";
        Add("Name", field.Name);
        Add("Type", field.Type);
        Add("ID", field.Id.ToString());
        Add("Index", field.Index.ToString());
        if (field.DefaultValueIndex.HasValue)
            Add("Default Value Index", field.DefaultValueIndex.Value.ToString());
    }

    private void PopulateStaff(IStaff staff)
    {
        Description = $"Staff: {staff.DisplayName}";
        Add("Class Name", staff.ClassName);
        Add("Display Name", staff.DisplayName);
        Add("Is Chart Class", staff.IsChartClass.ToString());
        if (staff is ElementStaff elementStaff)
            Add("Form Name", elementStaff.FormName);
        Add("Periods", staff.Periods.Count().ToString());
    }

    private void PopulatePeriod(IPeriod period)
    {
        Description = $"Period: {period.MethodName}";
        Add("Method Name", period.MethodName);
        Add("Time Offset (s)", period.TimeOffset.ToString("0.###", CultureInfo.InvariantCulture));
        Add("Min Length (s)", period.MinLength.ToString("0.###", CultureInfo.InvariantCulture));

        if (period is ElementPeriod elementPeriod)
        {
            Add("Form Name", elementPeriod.FormName);
            Add("Elements", elementPeriod.Elements.Count.ToString());
        }
    }

    private void PopulateInjector(Injector injector)
    {
        var decl = injector.InjectedClassDeclaration;
        Description = $"Element: {decl.Name}";
        Add("Type", decl.Name);

        foreach (var field in decl.InjectorFields)
        {
            var valueStr = GetInjectorFieldValueByIndex(injector, field.Index, field.Type.BasicType);
            Add($"  {field.Name}", valueStr);
        }
    }

    private static string GetInjectorFieldValueByIndex(Injector injector, int index, BasicType basicType)
    {
        try
        {
            return basicType switch
            {
                BasicType.Int or BasicType.Enum => injector.GetInjectorIntDefault(index)
                    ? "(default)" : injector.GetInjectorInt(index).ToString(),
                BasicType.Float => injector.GetInjectorFloatDefault(index)
                    ? "(default)" : injector.GetInjectorFloat(index).ToString(CultureInfo.InvariantCulture),
                BasicType.Bool => injector.GetInjectorBoolDefault(index)
                    ? "(default)" : injector.GetInjectorBool(index).ToString(),
                BasicType.String => injector.GetInjectorStringDefault(index)
                    ? "(default)" : $"\"{injector.GetInjectorString(index)}\"",
                _ => injector.GetInjectorObjectDefault(index)
                    ? "(default)" : injector.GetInjectorObject(index)?.ToString() ?? "null"
            };
        }
        catch
        {
            return "(error)";
        }
    }

    private void PopulateAnnotation(AnnotationInfo annotation)
    {
        Description = $"Annotation: @{annotation.Name}";
        Add("Name", annotation.Name);
        if (annotation.GenericType != null)
            Add("Generic Type", annotation.GenericType);
        foreach (var (key, value) in annotation.Parameters)
        {
            var displayValue = value switch
            {
                string s => $"\"{s}\"",
                null => "null",
                _ => value.ToString() ?? "null"
            };
            Add($"  {key}", displayValue);
        }
    }

    private void Add(string name, string value)
    {
        Properties.Add(new PropertyEntry(name, value));
    }
}

/// <summary>
/// 属性面板中单个属性的名称-值对。
/// </summary>
public class PropertyEntry
{
    public string Name { get; }
    public string Value { get; }

    public PropertyEntry(string name, string value)
    {
        Name = name;
        Value = value;
    }
}
