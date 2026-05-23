using Gorge;
using GorgeFramework;
namespace Reincal;

[
    delegate<float:ReincalSliderTap^> display = string:(ReincalSliderTap^ noteInjector) ->
    {
        return noteInjector.^hitTime + "";
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 1, g : 0.6431372549019608, b : 0},
    delegate<ElementLine:ReincalSliderTap^> elementLine = ElementLine:(ReincalSliderTap^ noteInjector) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[1];
        points[0] = new ElementLinePoint(noteInjector.^hitTime, 0.3, 0.2);
        return new ElementLine(new ColorArgb(1, 1, 0.6431372549019608, 0), points);
    },
    string displayName = "下落Note"
]
@EditableElement(type = "Note", editUpdateMode = EditUpdateMode.RePlay)
class NormalNote : ReincalNote
{
    [
        string type = "基本",
        int order = 0,
        string displayName = "轨道编号",
        string information = "不小于0",
        delegate<bool:int> check = bool:(int id) -> { return true; }
    ]
    @Inject
    int laneId = ^laneId;

    [
        auto defaultValue = 1.5,
        string type = "效果",
        int order = 2002,
        string displayName = "大小",
        string information = "相对轨道，>=0",
        delegate<bool:float> check = bool:(float size) -> { return size >= 0; }
    ]
    @Inject
    float size = ^size;

    NormalLane lane;

    Sprite graphNode;

    NormalNote() : super()
    {
        lane = (NormalLane) Environment.FindAliveLane("Reincal.NormalLane", laneId);
        if (lane == null)
        {
            return null;
        }
    }

    IAutomatonCommand[] DoRespond(string respondMode, float respondChartTime)
    {
        RespondResult respondResult;
        bool playEffect;
        
        switch (respondMode)
        {
            case "BestPerfect":
                respondResult = RespondResult.BestPerfect;
                playEffect = true;
                break;
            case "Perfect":
                respondResult = RespondResult.Perfect;
                playEffect = true;
                break;
            case "Good":
                respondResult = RespondResult.Good;
                playEffect = true;
                break;
            case "Miss":
                respondResult = RespondResult.Miss;
                playEffect = false;
                break;
        }
        
        Environment.Scoring(respondResult);
        
        if (!playEffect)
        {
            return new IAutomatonCommand[0];
        }
        
        Environment.PlayRespondEffect("RespondA");
        
        ReincalSliderRespondHint respondHint;

        return null;
        
        // respondHint = new ReincalSliderRespondHint(position, judgementLineRadius, respondChartTime, lane.graphNode);
        
        // IAutomatonCommand[] commands = new IAutomatonCommand[1];
        // commands[0] = new DeriveElementCommand(respondHint, false);
        // return commands;
    }
}