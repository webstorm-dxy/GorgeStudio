using Gorge;
using GorgeFramework;
namespace Reincal;

[
    delegate<float:NormalLane^> display = string:(NormalLane^ laneInjector) ->
    {
        return "下落轨道 #"+laneInjector.^id;
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 0.2396693, g : 0.6370158, b : 0.8},
    delegate<ElementLine:NormalLane^> elementLine = ElementLine:(NormalLane^ lane) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[2];
        points[0] = new ElementLinePoint(lane.^generateTime, 0.5, 0.8);
        points[1] = new ElementLinePoint(lane.^generateTime + lane.^keepTime, 0.5, 0.8);
        return new ElementLine(new ColorArgb(0.3, 1, 1, 1), points);
    },
    string displayName = "下落轨道"
]
@EditableElement(type = "轨道", editUpdateMode = EditUpdateMode.RePlay)
class NormalLane : Note
{
    [
        string type = "基本",
        int order = 0,
        string displayName = "轨道编号",
        string information = "不小于0",
        delegate<bool:int> check = bool:(int id) -> { return true; }
    ]
    @Inject
    int id = ^id;

    [
        auto defaultValue = 0.0,
        string type = "生命周期",
        int order = 1000,
        string displayName = "生成时刻",
        string information = "单位秒",
        delegate<bool:float> check = bool:(float generateTime) -> { return true; }
    ]
    @Inject
    float generateTime = ^generateTime;
    
    [
        auto defaultValue = 10.0,
        string type = "生命周期",
        int order = 1001,
        string displayName = "保持时长",
        string information = "单位秒，>0",
        delegate<bool:float> check = bool:(float keepTime) -> { return keepTime > 0; }
    ]
    @Inject
    float keepTime = ^keepTime;

    [
        auto defaultValue = VariableFloat : {baseValue : 0.0},
        string type = "基本",
        int order = 1,
        string displayName = "位置",
        string information = "标准坐标系|横轴为以生成时刻为0点的时间，单位秒；纵轴为相对于角度，取基础值加曲线纵轴",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ position) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat position = new ^position();

    [
        auto defaultValue = 6.22,
        string type = "效果",
        int order = 101,
        string displayName = "运动轨道半径",
        string information = "判定点与圆心的距离",
        delegate<bool:float> check = bool:(float radius) -> { return true; }
    ]
    @Inject
    float radius = ^radius;

    [
        auto defaultValue = null,
        string type = "效果",
        int order = 102,
        string displayName = "轨道颜色",
        string information = "横轴为以生成时刻为0的时间，单位秒",
        delegate<bool:ColorCurve^> check = bool:(ColorCurve^ color) -> { return true; }
    ]
    @Inject<ColorCurve^>
    ColorCurve color = (^color == null) ? null : (new ^color());

    ReincalSliderLane sliderLane;

    Node graphNode;
    Sprite judgementNode;
    Sprite settingCircleNode;

    bool set;
    int touchSignalId;
    float nowPosition;

    int handlingSignalId;

    [
        delegate<float:NormalLane^> time = float:(NormalLane^ laneInjector) ->
        {
            return laneInjector.^generateTime;
        }
    ]
    @ForwardTimedGenerate
    [
        delegate<float:NormalLane^> time = float:(NormalLane^ laneInjector) ->
        {
            return laneInjector.^generateTime + laneInjector.^keepTime;
        }
    ]
    @BackwardTimedGenerate
    @EditGenerate
    NormalLane(bool isAutoPlay, bool isReverse) : super()
    {
        sliderLane = (ReincalSliderLane) Environment.FindAliveLane("Reincal.ReincalSliderLane", "SliderLane");
        if (sliderLane == null)
        {
            return null;
        }
        Sprite sliderLaneBase = sliderLane.graphNode;

        ImageAsset lineImage = (ImageAsset) Environment.GetAssetByName("image:Reincal/FormAsset/JudgePoint");
        graphNode = new Node();
        graphNode.position.z = -2;
        graphNode.positionReference = sliderLaneBase;
        graphNode.rotationReference = sliderLaneBase;
        judgementNode = new Sprite(lineImage.texture);
        judgementNode.size.x = 0.6;
        judgementNode.size.y = 0.6;
        judgementNode.positionReference = graphNode;
        judgementNode.rotationReference = graphNode;

        ImageAsset settingCircleImage = (ImageAsset) Environment.GetAssetByName("image:Reincal/FormAsset/NormalLaneSettingCircle");
        settingCircleNode = new Sprite(settingCircleImage.texture);
        settingCircleNode.size.x = 1.6;
        settingCircleNode.size.y = 1.6;
        settingCircleNode.color.a = 0;
        settingCircleNode.positionReference = graphNode;
        settingCircleNode.rotationReference = graphNode;

        nodes = new Node[3];
        nodes[0] = graphNode;
        nodes[1] = judgementNode;
        nodes[2] = settingCircleNode;
        
        if(!isAutoPlay)
        {
            NormalLaneTsiga initializer = new NormalLaneTsiga(this, isReverse);
            automaton = new SignalTsiga(this, initializer.timeStack, initializer.inputGraph, initializer.historyStack);
        }

        ITransformer[] transformers;
        if(isAutoPlay)
        {
            transformers = new ITransformer[2];
            transformers[0] = new NormalLaneAutoplayTransformer(this);
            transformers[1] = new NormalLaneSignalTransformer(this);
        }
        else
        {
            transformers = new ITransformer[1];
            transformers[0] = new NormalLaneSignalTransformer(this);
        }

        simulator = new ElementSimulator(transformers);

        ITransformer[] lateTransformers = new ITransformer[3];
        lateTransformers[0] = new NormalLanePositionTransformer(this);
        lateTransformers[1] = new NormalLaneSettingCircleTransformer(this);
        lateTransformers[2] = new NormalLaneColorTransformer(this);

        lateIndependentSimulator = new ElementSimulator(lateTransformers);
    }

    @ForwardTimedDestroy
    float ForwardDestroyTime()
    {
        return generateTime + keepTime;
    }
    
    @BackwardTimedDestroy
    float BackwardDestroyTime()
    {
        return generateTime;
    }
    
    @EditTryGenerate
    static bool TryGenerate(NormalLane^ newInjector, float chartTime)
    {
        return chartTime >= generateTime && chartTime <= generateTime + keepTime;
    }
    
    @PeriodModifier
    static void PeriodModifier(NormalLane^ laneInjector, PeriodConfig periodConfig)
    {
        laneInjector.^generateTime = laneInjector.^generateTime + periodConfig.timeOffset;
    }

    ObjectList<NormalLaneAutoHitTarget> autoSetTargets = new ObjectList<NormalLaneAutoHitTarget>(){length : 0};
    
    void EnqueueAutoTarget(float time, float holdTime)
    {
        autoSetTargets.Add(new NormalLaneAutoHitTarget(time, holdTime));
    }

    float GetAimDistance(TouchSignal signal)
    {
        float rotation = sliderLane.rotation + nowPosition;
        float rotationAngle = rotation * 180 / 3.1415926;
        return Math.Abs(GetTouchAngle(signal) - rotationAngle);
    }

    float GetTouchAngle(TouchSignal signal)
    {
        Vector2 basePoint = sliderLane.basePoint;
        Vector2 touchPosition = signal.position;
        Vector2 touchVector = new Vector2(touchPosition.x - basePoint.x, touchPosition.y - basePoint.y);
        return Vector2.Angle(touchVector) + 90;
    }

    bool IsInTouchArea(TouchSignal signal)
    {
        Vector2 touchPosition = signal.position;
        float touchAngle = GetTouchAngle(signal);
        float rotation = sliderLane.rotation + nowPosition;
        float rotationAngle = rotation * 180 / 3.1415926;
        if(Math.Abs(touchAngle - rotationAngle) > 14)
        {
            return false;
        }
        float distance = Vector2.Distance(touchPosition, sliderLane.basePoint);
        // 5.2 ~ 7.2
        return Math.Abs(distance - 6.2) <= 1;
    }
}