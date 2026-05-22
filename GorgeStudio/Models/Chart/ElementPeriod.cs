using System.Collections.Generic;
using System.Text;
using Gorge.Native.Gorge;
using GorgeStudio.Services.CodeGeneration;

namespace GorgeStudio.Models.Chart;

public class ElementPeriod : Period
{
    public string FormName { get; }

    public readonly List<Injector> Elements;

    public ElementPeriod(string formName, string methodName, Injector configInjector) : base(methodName, configInjector)
    {
        Elements = new List<Injector>();
        FormName = formName;
    }

    public override string ToGorgeCode(int indentation)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[", indentation);
        sb.AppendLine(
            $"GorgeFramework.PeriodConfig^ config = {InjectorHardcodeGenerator.Generate(ConfigInjector, indentation + 1)}",
            indentation + 1);
        sb.AppendLine("]", indentation);
        sb.AppendLine("@Chart", indentation);
        sb.AppendLine($"static GorgeFramework.Element^[] {MethodName}()", indentation);
        sb.AppendLine("{", indentation);
        sb.AppendLine(
            $"return new GorgeFramework.Element^[{Elements.Count}]{InjectorHardcodeGenerator.Generate("GorgeFramework.Element^", Elements, false, indentation + 1)};",
            indentation + 1);
        sb.AppendLine("}", indentation);
        return sb.ToString();
    }

    public override Period Clone()
    {
        var elementPeriod = new ElementPeriod(FormName, MethodName, (Injector)ConfigInjector.Clone());
        foreach (var element in Elements)
            elementPeriod.Elements.Add((Injector)element.Clone());
        return elementPeriod;
    }
}
