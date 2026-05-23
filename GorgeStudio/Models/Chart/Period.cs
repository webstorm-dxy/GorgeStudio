using System.Linq;
using Gorge.Native.Gorge;

namespace GorgeStudio.Models.Chart;

public interface IPeriod
{
    string MethodName { get; set; }
    Injector ConfigInjector { get; }

    /// <summary>
    /// 乐段起点时间偏移，直接从 ConfigInjector 读取。
    /// </summary>
    float TimeOffset { get; }

    /// <summary>
    /// 乐段最小显示长度，直接从 ConfigInjector 读取。
    /// </summary>
    float MinLength { get; }

    void UpdateConfig(Injector injector);
    string ToGorgeCode(int indentation);
    IPeriod Clone();
}

public abstract class Period : IPeriod
{
    public string MethodName { get; set; }
    public Injector ConfigInjector { get; private set; }

    public float TimeOffset
    {
        get
        {
            if (ConfigInjector.InjectedClassDeclaration.TryGetInjectorFieldByName("timeOffset", out var field)
                && !ConfigInjector.GetInjectorFloatDefault(field.Index))
                return ConfigInjector.GetInjectorFloat(field.Index);
            return 0f;
        }
    }

    public float MinLength
    {
        get
        {
            if (ConfigInjector.InjectedClassDeclaration.TryGetInjectorFieldByName("minLength", out var field)
                && !ConfigInjector.GetInjectorFloatDefault(field.Index))
                return ConfigInjector.GetInjectorFloat(field.Index);
            return 10f;
        }
    }

    protected Period(string methodName, Injector configInjector)
    {
        MethodName = methodName;
        ConfigInjector = configInjector;
    }

    public void UpdateConfig(Injector injector)
    {
        ConfigInjector = injector;
    }

    public abstract string ToGorgeCode(int indentation);

    IPeriod IPeriod.Clone() => Clone();
    public abstract Period Clone();
}
