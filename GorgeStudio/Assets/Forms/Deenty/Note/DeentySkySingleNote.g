using Gorge;
using GorgeFramework;
namespace Deenty;

class DeentySkySingleNote : DeentyNote
{
    [
        auto defaultValue = VariableFloat : {baseValue : 0.0},
        string type = "基本",
        int order = 101,
        string displayName = "X坐标",
        string information = "标准坐标系|横轴为响应进度，0代表出现时刻，1代表响应时刻；纵轴为坐标加值，实时坐标为基值加上纵轴值",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ positionX) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat positionX = new ^positionX();
    
    [
        auto defaultValue = VariableFloat : {baseValue : 0.0},
        string type = "基本",
        int order = 102,
        string displayName = "Y坐标",
        string information = "标准坐标系|横轴为响应进度，0代表出现时刻，1代表响应时刻；纵轴为坐标加值，实时坐标为基值加上纵轴值",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ positionY) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat positionY = new ^positionY();
    
    [
        auto defaultValue = VariableFloat : {baseValue : 1.0},
        string type = "基本",
        int order = 103,
        string displayName = "尺寸",
        string information = "边长，标准坐标系|横轴为响应进度，0代表出现时刻，1代表响应时刻；纵轴为尺寸加值，实时尺寸为基值加上纵轴值",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ size) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat size = new ^size();
    
    [
        auto defaultValue = 1.5,
        string type = "效果",
        int order = 104,
        string displayName = "外圈尺寸",
        string information = "比核心边长更宽的长度，标准坐标系",
        delegate<bool:float> check = bool:(float respondMoment) -> { return true; }
    ]
    @Inject
    float loopSize = ^loopSize;
    
    [
        auto defaultValue = LinearCurve : {timeStart : 0.0, valueStart : 0.0, timeEnd : 1.1, valueEnd : 1.1},
        string type = "响应",
        int order = 200,
        string displayName = "缩圈曲线",
        string information = "横轴为响应进度，0代表出现时刻，1代表响应时刻；纵轴为缩圈进度，0代表判定圈为原尺寸，1代表判定圈为核心尺寸",
        delegate<bool:FunctionCurve> check = bool:(FunctionCurve respondHintProcessCurve) -> { return true; },
        string convertFrom = "PositionXCurve PositionStartXCurve"
    ]
    @Inject<FunctionCurve^>
    FunctionCurve loopSizeCurve = new ^loopSizeCurve();
    NineSliceSprite loopNode;
    
    DeentySkySingleNote()
    {
        laneType = "Deenty.SkyArea";
        respondArea = new SkyRespondArea(this);
    }
    
    string DisplayString()
    {
        return respondMoment + " [" + respondTime + "] | " + laneName + ":" + positionX.baseValue + "," + positionY.baseValue;
    }
    
    Vector2 NoteAreaRespondPosition()
    {
        return new Vector2(positionX.baseValue, positionY.baseValue);
    }
    
    IAutomatonCommand[] DoRespond(string respondMode, float respondChartTime)
    {
        RespondResult respondResult;
        bool playRespondEffect;
        
        switch (respondMode)
        {
            case "BestPerfect":
                respondResult = RespondResult.BestPerfect;
                playRespondEffect = true;
                break;
            case "MutedBestPerfect":
                respondResult = RespondResult.BestPerfect;
                playRespondEffect = false;
                break;
            case "Perfect":
                respondResult = RespondResult.Perfect;
                playRespondEffect = true;
                break;
            case "Good":
                respondResult = RespondResult.Good;
                playRespondEffect = true;
                break;
            case "Miss":
                respondResult = RespondResult.Miss;
                playRespondEffect = false;
                break;
        }
        
        // 计分
        Environment.Scoring(respondResult);
        
        if (playRespondEffect)
        {
            // 播放响应音效
            Environment.PlayRespondEffect("NormalRespond");
            
            DeentyLane lane = (DeentyLane) Environment.FindAliveLane(laneType, laneName);
            Vector3 respondPosition = lane.positionNode.LocalPositionToGlobalPosition(NoteAreaRespondPosition().ToVector3());
            
            RespondHint respondHint = new RespondHint(respondHintSize, respondHintProcessCurve, respondHintKeepTime, respondPosition.ToVector2(), respondChartTime, respondResult);
            IAutomatonCommand[] commands = new IAutomatonCommand[1];
            commands[0] = new DeriveElementCommand(respondHint, false);
            return commands;
        }
        
        return new IAutomatonCommand[0];
    }
    
    void ReInjectDeentySkyNote(DeentySkySingleNote^ newInjector)
    {
        ReInjectNote(newInjector);
        positionX = new (newInjector.^positionX)();
        positionY = new (newInjector.^positionY)();
        size = new (newInjector.^size)();
        loopSize = newInjector.^loopSize;
        loopSizeCurve = new (newInjector.^loopSizeCurve)();
    }
    
    static float InjectorGenerateTime(DeentySkySingleNote^ noteInjector)
    {
        SkyArea skyArea = (SkyArea) Environment.FindAliveLane("Deenty.SkyArea", noteInjector.^laneName);
        if (skyArea == null)
        {
            return Math.FloatPositiveInfinity();
        }
        float noteBeginTime = noteInjector.^respondMoment - noteInjector.^respondTime;
        float judgementBeginTime = skyArea.MissRespondStartMoment(noteInjector.^respondMoment);
        return Math.Min(noteBeginTime, judgementBeginTime);
    }
    
    static float InjectorDestroyTime(DeentySkySingleNote^ noteInjector)
    {
        SkyArea skyArea = (SkyArea) Environment.FindAliveLane("Deenty.SkyArea", noteInjector.^laneName);
        if (skyArea == null)
        {
            return Math.FloatNegativeInfinity();
        }
        float noteDestroyTime = noteInjector.^respondMoment + noteInjector.^stayTime;
        float judgementEndTime = skyArea.MissRespondEndMoment(noteInjector.^respondMoment);
        return Math.Max(noteDestroyTime, judgementEndTime);
    }
    
    float GenerateTime()
    {
        SkyArea skyArea = (SkyArea) Environment.FindAliveLane("Deenty.SkyArea", laneName);
        if (skyArea == null)
        {
            return Math.FloatNegativeInfinity();
        }
        float noteGenerateTime = respondMoment - respondTime;
        float judgementBeginTime = skyArea.MissRespondStartMoment(respondMoment);
        return Math.Min(noteGenerateTime, judgementBeginTime);
    }
    
    float DestroyTime()
    {
        SkyArea skyArea = (SkyArea) Environment.FindAliveLane("Deenty.Line", laneName);
        if (skyArea == null)
        {
            return Math.FloatPositiveInfinity();
        }
        float noteDestroyTime = respondMoment + stayTime;
        float judgementEndTime = skyArea.MissRespondEndMoment(respondMoment);
        return Math.Max(noteDestroyTime, judgementEndTime);
    }
}