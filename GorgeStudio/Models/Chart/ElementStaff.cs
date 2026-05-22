using System;
using System.Text;
using Gorge.GorgeCompiler.Visitors;
using GorgeStudio.Services.CodeGeneration;

namespace GorgeStudio.Models.Chart;

public class ElementStaff : Staff<ElementPeriod>
{
    public string FormName { get; }

    public ElementStaff(string className, bool isChartClass, string displayName, string formName) : base(className,
        isChartClass, displayName)
    {
        FormName = formName;
    }

    public override string ToGorgeCode()
    {
        if (!IsChartClass)
            throw new Exception("尝试将非谱面谱表转化为谱面代码");

        var sb = new StringBuilder();

        sb.AppendLine("[");
        var formLine = $"string form = {LiteralHelper.StringToStringLiteral(FormName)},";
        var displayLine = $"string displayName = {LiteralHelper.StringToStringLiteral(DisplayName)}";
        sb.AppendLine(formLine, 1);
        sb.AppendLine(displayLine, 1);
        sb.AppendLine("]");
        sb.AppendLine("@ElementStaff");
        sb.AppendLine($"class {ClassName}");
        sb.AppendLine("{");
        foreach (var period in Periods)
        {
            sb.AppendLine(period.ToGorgeCode(1));
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    protected override Staff<ElementPeriod> Clone()
    {
        var newStaff = new ElementStaff(ClassName, IsChartClass, DisplayName, FormName);
        foreach (var elementPeriod in Periods)
            newStaff.Periods.Add((ElementPeriod)elementPeriod.Clone());
        return newStaff;
    }
}
