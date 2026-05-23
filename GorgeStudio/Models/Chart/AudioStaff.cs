using System;
using System.Text;
using Gorge.GorgeCompiler.Visitors;
using GorgeStudio.Services.CodeGeneration;

namespace GorgeStudio.Models.Chart;

public class AudioStaff : Staff<AudioPeriod>
{
    public AudioStaff(string className, bool isChartClass, string displayName) : base(className, isChartClass,
        displayName)
    {
    }

    public override string ToGorgeCode()
    {
        if (!IsChartClass)
            throw new Exception("尝试将非谱面谱表转化为谱面代码");

        var sb = new StringBuilder();

        sb.AppendLine("[");
        var displayLine = $"string displayName = {LiteralHelper.StringToStringLiteral(DisplayName)}";
        sb.AppendLine(displayLine, 1);
        sb.AppendLine("]");
        sb.AppendLine("@AudioStaff");
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

    protected override Staff<AudioPeriod> Clone()
    {
        var newStaff = new AudioStaff(ClassName, IsChartClass, DisplayName);
        foreach (var audioPeriod in Periods)
            newStaff.Periods.Add((AudioPeriod)audioPeriod.Clone());
        return newStaff;
    }
}
