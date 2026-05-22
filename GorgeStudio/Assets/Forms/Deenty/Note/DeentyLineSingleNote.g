using Gorge;
using GorgeFramework;
namespace Deenty;

class DeentyLineSingleNote : DeentyLineNote
{
    [
        auto defaultValue = LinearCurve : {timeStart : 0.0, valueStart : 0.0, timeEnd : 2.0, valueEnd : 2.0},
        string type = "效果",
        int order = 200,
        string displayName = "运动曲线",
        string information = "横轴为响应进度，0代表出现时刻，1代表响应时刻；纵轴为运动进度，0代表轨道末端，1代表判定线",
        delegate<bool:FunctionCurve> check = bool:(FunctionCurve positionXCurve) -> { return true; }
    ]
    @Inject<FunctionCurve^>
    FunctionCurve positionXCurve = new ^positionXCurve();
    
    DeentyLineSingleNote()
    {
    }
    
    string DisplayString()
    {
        return respondMoment + " [" + respondTime + "] | " + laneName + ":" + positionY.baseValue;
    }
    
    void ReInjectDeentyLineSingleNote(DeentyLineSingleNote^ newInjector)
    {
        ReInjectDeentyLineNote(newInjector);
        positionXCurve = new (newInjector.^positionXCurve)();
    }
    
    static float InjectorGenerateTime(DeentyLineSingleNote^ noteInjector)
    {
        Line line = (Line) Environment.FindAliveLane("Deenty.Line", noteInjector.^laneName);
        if (line == null)
        {
            return Math.FloatPositiveInfinity();
        }
        float noteBeginTime = noteInjector.^respondMoment - noteInjector.^respondTime;
        float judgementBeginTime = line.MissRespondStartMoment(noteInjector.^respondMoment);
        return Math.Min(noteBeginTime, judgementBeginTime);
    }
    
    static float InjectorDestroyTime(DeentyLineSingleNote^ noteInjector)
    {
        Line line = (Line) Environment.FindAliveLane("Deenty.Line", noteInjector.^laneName);
        if (line == null)
        {
            return Math.FloatNegativeInfinity();
        }
        float noteDestroyTime = noteInjector.^respondMoment + noteInjector.^stayTime;
        float judgementEndTime = line.MissRespondEndMoment(noteInjector.^respondMoment);
        return Math.Max(noteDestroyTime, judgementEndTime);
    }
    
    float GenerateTime()
    {
        Line line = (Line) Environment.FindAliveLane("Deenty.Line", laneName);
        if (line == null)
        {
            return Math.FloatNegativeInfinity();
        }
        float noteGenerateTime = respondMoment - respondTime;
        float judgementBeginTime = line.MissRespondStartMoment(respondMoment);
        return Math.Min(noteGenerateTime, judgementBeginTime);
    }
    
    float DestroyTime()
    {
        Line line = (Line) Environment.FindAliveLane("Deenty.Line", laneName);
        if (line == null)
        {
            return Math.FloatPositiveInfinity();
        }
        float noteDestroyTime = respondMoment + stayTime;
        float judgementEndTime = line.MissRespondEndMoment(respondMoment);
        return Math.Max(noteDestroyTime, judgementEndTime);
    }
}