using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Gorge.GorgeLanguage.Objective;
using Gorge.Native.Gorge;
using GorgeStudio.AppCore.Models.Results;
using GorgeStudio.Models;
using GorgeStudio.Models.Chart;

namespace GorgeStudio.AppCore.Services;

public interface IPropertyInspectionService
{
    InspectResult Inspect(object? obj);
}

public sealed class PropertyInspectionService : IPropertyInspectionService
{
    public InspectResult Inspect(object? value)
    {
        if (value == null)
            return new InspectResult("Properties", "Select an object to view its properties.", new List<PropertyEntry>());

        switch (value)
        {
            case CompiledClassInfo cls:
                return InspectClass(cls);
            case CompiledEnumInfo e:
                return InspectEnum(e);
            case CompiledInterfaceInfo i:
                return InspectInterface(i);
            case FieldInfo field:
                return InspectField(field);
            case MethodInfo method:
                return InspectMethod(method);
            case ConstructorInfo ctor:
                return InspectConstructor(ctor);
            case InjectorFieldInfo injectorField:
                return InspectInjectorField(injectorField);
            case AnnotationInfo annotation:
                return InspectAnnotation(annotation);
            case IStaff staff:
                return InspectStaff(staff);
            case IPeriod period:
                return InspectPeriod(period);
            case Injector injector:
                return InspectInjector(injector);
            default:
                return new InspectResult("Unknown", $"Unknown object type: {value.GetType().Name}", new List<PropertyEntry>());
        }
    }

    private static InspectResult InspectClass(CompiledClassInfo cls)
    {
        var props = new List<PropertyEntry>
        {
            new("Full Name", cls.FullName),
            new("Class Name", cls.ClassName),
            new("Namespace", string.IsNullOrEmpty(cls.Namespace) ? "(global)" : cls.Namespace),
            new("Is Chart Code", cls.IsChartCode.ToString()),
            new("Is Native", cls.IsNative.ToString()),
            new("Inheritance Depth", cls.InheritanceDepth.ToString()),
        };
        if (cls.SuperClassName != null)
            props.Add(new PropertyEntry("Super Class", cls.SuperClassName));
        if (cls.InterfaceNames.Count > 0)
            props.Add(new PropertyEntry("Interfaces", string.Join(", ", cls.InterfaceNames)));
        if (cls.AnnotationNames.Count > 0)
            props.Add(new PropertyEntry("Annotations", string.Join(", ", cls.AnnotationNames)));
        props.Add(new PropertyEntry("Fields", cls.Fields.Count.ToString()));
        props.Add(new PropertyEntry("Methods", cls.Methods.Count.ToString()));
        props.Add(new PropertyEntry("Static Methods", cls.StaticMethods.Count.ToString()));
        props.Add(new PropertyEntry("Constructors", cls.Constructors.Count.ToString()));
        props.Add(new PropertyEntry("Injector Fields", cls.InjectorFields.Count.ToString()));

        return new InspectResult("Class", $"Class: {cls.FullName}", props);
    }

    private static InspectResult InspectEnum(CompiledEnumInfo e)
    {
        var props = new List<PropertyEntry>
        {
            new("Full Name", e.FullName),
            new("Namespace", string.IsNullOrEmpty(e.Namespace) ? "(global)" : e.Namespace),
            new("Is Native", e.IsNative.ToString()),
            new("Values", string.Join(", ", e.Values)),
        };
        if (e.DisplayNames.Count > 0)
            props.Add(new PropertyEntry("Display Names", string.Join(", ", e.DisplayNames)));

        return new InspectResult("Enum", $"Enum: {e.FullName}", props);
    }

    private static InspectResult InspectInterface(CompiledInterfaceInfo i)
    {
        var props = new List<PropertyEntry>
        {
            new("Full Name", i.FullName),
            new("Namespace", string.IsNullOrEmpty(i.Namespace) ? "(global)" : i.Namespace),
            new("Is Native", i.IsNative.ToString()),
            new("Methods", i.Methods.Count.ToString()),
        };

        return new InspectResult("Interface", $"Interface: {i.FullName}", props);
    }

    private static InspectResult InspectField(FieldInfo field)
    {
        var props = new List<PropertyEntry>
        {
            new("Name", field.Name),
            new("Type", field.Type),
            new("ID", field.Id.ToString()),
            new("Index", field.Index.ToString()),
        };
        if (field.Annotations.Count > 0)
            props.Add(new PropertyEntry("Annotations", string.Join(", ", field.Annotations.Select(a => a.Name))));

        return new InspectResult("Field", $"Field: {field.Name}", props);
    }

    private static InspectResult InspectMethod(MethodInfo method)
    {
        var props = new List<PropertyEntry>
        {
            new("Name", method.Name),
            new("Return Type", method.ReturnType ?? "void"),
            new("ID", method.Id.ToString()),
        };
        if (method.Parameters.Count > 0)
        {
            for (var i = 0; i < method.Parameters.Count; i++)
            {
                var p = method.Parameters[i];
                props.Add(new PropertyEntry($"  Parameter[{i}]", $"{p.Name}: {p.Type} (id={p.Id})"));
            }
        }
        if (method.Annotations.Count > 0)
            props.Add(new PropertyEntry("Annotations", string.Join(", ", method.Annotations.Select(a => a.Name))));

        return new InspectResult("Method", $"Method: {method.Name}", props);
    }

    private static InspectResult InspectConstructor(ConstructorInfo ctor)
    {
        var props = new List<PropertyEntry> { new("ID", ctor.Id.ToString()) };
        if (ctor.Parameters.Count > 0)
        {
            for (var i = 0; i < ctor.Parameters.Count; i++)
            {
                var p = ctor.Parameters[i];
                props.Add(new PropertyEntry($"  Parameter[{i}]", $"{p.Name}: {p.Type} (id={p.Id})"));
            }
        }
        if (ctor.Annotations.Count > 0)
            props.Add(new PropertyEntry("Annotations", string.Join(", ", ctor.Annotations.Select(a => a.Name))));

        return new InspectResult("Constructor", "Constructor", props);
    }

    private static InspectResult InspectAnnotation(AnnotationInfo annotation)
    {
        var props = new List<PropertyEntry>
        {
            new("Name", annotation.Name),
        };
        if (annotation.GenericType != null)
            props.Add(new PropertyEntry("Generic Type", annotation.GenericType));
        foreach (var (key, value) in annotation.Parameters)
        {
            var displayValue = value switch
            {
                string s => $"\"{s}\"",
                null => "null",
                _ => value.ToString() ?? "null"
            };
            props.Add(new PropertyEntry($"  {key}", displayValue));
        }

        return new InspectResult("Annotation", $"Annotation: @{annotation.Name}", props);
    }

    private static InspectResult InspectInjectorField(InjectorFieldInfo field)
    {
        var props = new List<PropertyEntry>
        {
            new("Name", field.Name),
            new("Type", field.Type),
            new("ID", field.Id.ToString()),
            new("Index", field.Index.ToString()),
        };
        if (field.DefaultValueIndex.HasValue)
            props.Add(new PropertyEntry("Default Value Index", field.DefaultValueIndex.Value.ToString()));

        return new InspectResult("Injector Field", $"Injector Field: {field.Name}", props);
    }

    private static InspectResult InspectStaff(IStaff staff)
    {
        var props = new List<PropertyEntry>
        {
            new("Class Name", staff.ClassName),
            new("Display Name", staff.DisplayName),
            new("Is Chart Class", staff.IsChartClass.ToString()),
        };
        if (staff is ElementStaff elementStaff)
            props.Add(new PropertyEntry("Form Name", elementStaff.FormName));
        props.Add(new PropertyEntry("Periods", staff.Periods.Count().ToString()));

        return new InspectResult("Staff", $"Staff: {staff.DisplayName}", props);
    }

    private static InspectResult InspectPeriod(IPeriod period)
    {
        var props = new List<PropertyEntry>
        {
            new("Method Name", period.MethodName),
            new("Time Offset (s)", period.TimeOffset.ToString("0.###", CultureInfo.InvariantCulture)),
            new("Min Length (s)", period.MinLength.ToString("0.###", CultureInfo.InvariantCulture)),
        };
        if (period is ElementPeriod elementPeriod)
        {
            props.Add(new PropertyEntry("Form Name", elementPeriod.FormName));
            props.Add(new PropertyEntry("Elements", elementPeriod.Elements.Count.ToString()));
        }

        return new InspectResult("Period", $"Period: {period.MethodName}", props);
    }

    private static InspectResult InspectInjector(Injector injector)
    {
        var decl = injector.InjectedClassDeclaration;
        var props = new List<PropertyEntry> { new("Type", decl.Name) };

        foreach (var field in decl.InjectorFields)
        {
            var valueStr = GetInjectorFieldValueByIndex(injector, field.Index, field.Type.BasicType);
            props.Add(new PropertyEntry($"  {field.Name}", valueStr));
        }

        return new InspectResult("Element", $"Element: {decl.Name}", props);
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
}
