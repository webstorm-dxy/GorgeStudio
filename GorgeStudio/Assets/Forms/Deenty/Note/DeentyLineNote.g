using Gorge;
using GorgeFramework;
namespace Deenty;

class DeentyLineNote : DeentyNote
{
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
        auto defaultValue = VariableFloat : {baseValue : 1.5},
        string type = "基本",
        int order = 103,
        string displayName = "长度",
        string information = "标准坐标系|横轴为响应进度，0代表出现时刻，1代表响应时刻；纵轴为长度加值，实时长度为基值加上纵轴值",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ length) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat length = new ^length();
    
    [
        auto defaultValue = 6.0,
        string type = "效果",
        int order = 104,
        string displayName = "轨道长度",
        string information = "标准坐标系",
        delegate<bool:float> check = bool:(float laneLength) -> { return true; }
    ]
    @Inject
    float laneLength = ^laneLength;
    
    DeentyLineNote()
    {
        laneType = "Deenty.Line";
        respondArea = new LineRespondArea(this);
    }
    
    Vector2 NoteAreaRespondPosition()
    {
        return new Vector2(0.0, positionY.baseValue);
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
    
    void ReInjectDeentyLineNote(DeentyLineNote^ newInjector)
    {
        ReInjectNote(newInjector);
        positionY = new (newInjector.^positionY)();
        length = new (newInjector.^length)();
        laneLength = newInjector.^laneLength;
    }
}