using System.Text;
using Gorge.Native.Gorge;
using GorgeStudio.Services.CodeGeneration;

namespace GorgeStudio.Models.Chart;

public class AudioPeriod : Period
{
    /// <summary>
    /// 音频资源注入器
    /// </summary>
    public Injector AudioInjector { get; private set; }

    public AudioPeriod(string methodName, Injector configInjector, Injector audioInjector) : base(methodName,
        configInjector)
    {
        UpdateAudio(audioInjector);
    }

    public void UpdateAudio(Injector audioInjector)
    {
        AudioInjector = audioInjector;
    }

    public override string ToGorgeCode(int indentation)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[", indentation);
        sb.AppendLine(
            $"GorgeFramework.PeriodConfig^ config = {InjectorHardcodeGenerator.Generate(ConfigInjector, indentation + 1)}",
            indentation + 1);
        sb.AppendLine("]", indentation);
        sb.AppendLine("@Song", indentation);
        sb.AppendLine($"static GorgeFramework.AudioAsset^ {MethodName}()", indentation);
        sb.AppendLine("{", indentation);
        sb.AppendLine(
            $"return {InjectorHardcodeGenerator.Generate(AudioInjector, indentation + 1)};",
            indentation + 1);
        sb.AppendLine("}", indentation);
        return sb.ToString();
    }

    public override Period Clone()
    {
        return new AudioPeriod(MethodName, (Injector)ConfigInjector.Clone(), (Injector)AudioInjector.Clone());
    }
}
