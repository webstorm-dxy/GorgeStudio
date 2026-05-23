using Gorge;
using GorgeFramework;
namespace BeeBoo;

[
    delegate<float:LaneSet^> display = string:(LaneSet^ laneInjector) ->
    {
        return "轨道组 #" + laneInjector.^id;
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 0.2396693, g : 0.6370158, b : 0.8},
    delegate<ElementLine:LaneSet^> elementLine = ElementLine:(LaneSet^ lane) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[2];
        points[0] = new ElementLinePoint(lane.^startTime, 0.5, 0.8);
        points[1] = new ElementLinePoint(lane.^startTime + lane.^keepTime, 0.5, 0.8);
        return new ElementLine(new ColorArgb(0.3, 1, 1, 1), points);
    },
    string displayName = "轨道组"
]
@EditableElement(type = "轨道", editUpdateMode = EditUpdateMode.RePlay)
class LaneSet : Element
{
    // 注入字段

    [
        string type = "基本",
        int order = 0,
        string displayName = "轨道组编号",
        string information = "不能为空",
        delegate<bool:int> check = bool:(int id) -> { return true; }
    ]
    @Inject
    int id = ^id;

    [
        auto defaultValue = 0.0,
        string type = "生命周期",
        int order = 1000,
        string displayName = "生成时刻",
        string information = "单位秒，乐段相对时间",
        delegate<bool:float> check = bool:(float generateTime) -> { return true; },
        string timePointName = "GenerateTime",
        string timePointEarlyAnchor = null,
        string timePointLateAnchor = null
    ]
    @Inject
    float startTime = ^startTime;

    [
        auto defaultValue = 10,
        string type = "生命周期",
        int order = 1001,
        string displayName = "保持时长",
        string information = "单位秒，>0",
        delegate<bool:float> check = bool:(float keepTime) -> { return keepTime > 0; },
        string timePointName = "DestroyTime",
        string timePointEarlyAnchor = "GenerateTime",
        string timePointLateAnchor = null
    ]
    @Inject
    float keepTime = ^keepTime;

    [
        auto defaultValue = VariableFloat : {baseValue : 0},
        string type = "基本",
        int order = 1,
        string displayName = "位置X",
        string information = "标准坐标",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ positionX) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat positionX = new ^positionX();

    [
        auto defaultValue = VariableFloat : {baseValue : 0},
        string type = "基本",
        int order = 2,
        string displayName = "位置Y",
        string information = "标准坐标",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ positionY) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat positionY = new ^positionY();

    [
        auto defaultValue = VariableFloat : {baseValue : 0},
        string type = "基本",
        int order = 3,
        string displayName = "旋转角度",
        string information = "本地时间，旋转角度，角度制",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ rotation) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat rotation = new ^rotation();

    // 时变字段
    float localTime;
    float nowPositionX;
    float nowPositionY;
    float nowRotation;

    // 存储字段
    Lane[] lanes;

    Node baseNode;

    LaneSet() : super()
    {
        baseNode = new Node();
    }

    Lane GetLane(int laneIndex)
    {
        if(laneIndex >= lanes.length || laneIndex < 0)
        {
            return null;
        }

        return lanes[laneIndex];
    }

    @ForwardTimedDestroy
    float ForwardDestroyTime()
    {
        return startTime + keepTime;
    }
    
    @BackwardTimedDestroy
    float BackwardDestroyTime()
    {
        return startTime;
    }
    
    @EditTryGenerate
    static bool TryGenerate(LaneSet^ newInjector, float chartTime)
    {
        return chartTime >= newInjector.^startTime && chartTime <= newInjector.^startTime + newInjector.^keepTime;
    }

    @PeriodModifier
    static void PeriodModifier(LaneSet^ noteInjector, PeriodConfig periodConfig)
    {
        noteInjector.^startTime = noteInjector.^startTime + periodConfig.timeOffset;
    }

    void ReInjectLaneSet(LaneSet^ newInjector)
    {
        id = newInjector.^id;
        startTime = newInjector.^startTime;
        keepTime = newInjector.^keepTime;
        positionX = new (newInjector.^positionX)();
        positionY = new (newInjector.^positionY)();
        rotation = new (newInjector.^rotation)();
    }
}