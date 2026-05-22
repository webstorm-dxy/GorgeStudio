using Gorge;
using GorgeFramework;
namespace BeeBoo;

[
    delegate<float:Tracker^> display = string:(InputArea^ laneInjector) ->
    {
        return "" + laneInjector.^startTime;
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 0.2396693, g : 0.6370158, b : 0.8},
    delegate<ElementLine:InputArea^> elementLine = ElementLine:(InputArea^ lane) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[2];
        points[0] = new ElementLinePoint(lane.^startTime, 0.5, 0.8);
        points[1] = new ElementLinePoint(lane.^startTime + lane.^keepTime, 0.5, 0.8);
        return new ElementLine(new ColorArgb(0.3, 1, 1, 1), points);
    },
    string displayName = "输入区"
]
@EditableElement(type = "输入区", editUpdateMode = EditUpdateMode.ReInject)
class InputArea : Note
{
    // 注入字段
    [
        auto defaultValue = 0,
        string type = "基本",
        int order = 1,
        string displayName = "触控通道编号",
        string information = "",
        delegate<bool:int> check = bool:(int channelId) -> { return true; }
    ]
    @Inject
    int channelId = ^channelId;

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
        int order = 2,
        string displayName = "水平位置",
        string information = "标准坐标",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ positionX) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat positionX = new ^positionX();

    [
        auto defaultValue = VariableFloat : {baseValue : 0},
        string type = "基本",
        int order = 3,
        string displayName = "垂直位置",
        string information = "标准坐标",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ positionY) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat positionY = new ^positionY();

    [
        auto defaultValue = VariableFloat : {baseValue : 1},
        string type = "基本",
        int order = 4,
        string displayName = "宽度",
        string information = "标准坐标",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ width) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat width = new ^width();

    [
        auto defaultValue = VariableFloat : {baseValue : 1},
        string type = "基本",
        int order = 5,
        string displayName = "高度",
        string information = "标准坐标",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ height) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat height = new ^height();

    [
        auto defaultValue = VariableFloat : {baseValue : 45},
        string type = "基本",
        int order = 6,
        string displayName = "角度",
        string information = "标准坐标",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ rotation) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat rotation = new ^rotation();

    [
        auto defaultValue = VariableFloat : {baseValue : 1.0},
        string type = "效果",
        int order = 2000,
        string displayName = "不透明度",
        string information = "标注坐标",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ alpha) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat alpha = new ^alpha();

    // 时变字段
    float localTime;
    float nowPositionX;
    float nowPositionY;
    float nowWidth;
    float nowHeight;
    float nowRotation;
    float nowAlpha;

    // 存储字段
    Channel channel;
    NineSliceSprite sprite;

    [
        delegate<float:Tracker^> time = float:(Tracker^ laneInjector) ->
        {
            return laneInjector.^startTime;
        }
    ]
    @ForwardTimedGenerate
    [
        delegate<float:Tracker^> time = float:(Tracker^ laneInjector) ->
        {
            return laneInjector.^startTime + laneInjector.^keepTime;
        }
    ]
    @BackwardTimedGenerate
    @EditGenerate
    InputArea() : super()
    {
        channel = (Channel) Environment.FindAliveLane("BeeBoo.Channel", channelId);
        if (channel == null)
        {
            return null;
        }

        ImageAsset innerCircleImage = (ImageAsset) Environment.GetAssetByName("image:BeeBoo/FormAsset/InputArea");
        sprite = new NineSliceSprite(innerCircleImage.texture, new Vector2(99, 99), new Vector2(99, 99), new Vector2(0.1166666666666667, 0.1166666666666667));
        sprite.position.z = -1.1;

        nodes = new Node[1];
        nodes[0] = sprite;

        ITransformer[] transformers = new ITransformer[1];
        transformers[0] = new InputAreaTimeVaryingVariableTransformer(this);
        simulator = new ElementSimulator(transformers);

        ITransformer[] lateTransformers = new ITransformer[1];
        lateTransformers[0] = new InputAreaPositionTransformer(this);
        lateIndependentSimulator = new ElementSimulator(lateTransformers);
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
    static bool TryGenerate(InputArea^ newInjector, float chartTime)
    {
        return chartTime >= newInjector.^startTime && chartTime <= newInjector.^startTime + newInjector.^keepTime;
    }

    @PeriodModifier
    static void PeriodModifier(InputArea^ noteInjector, PeriodConfig periodConfig)
    {
        noteInjector.^startTime = noteInjector.^startTime + periodConfig.timeOffset;
    }

    @EditReInject
    void ReInjectTracker(InputArea^ newInjector)
    {
        channelId = newInjector.^channelId;
        startTime = newInjector.^startTime;
        keepTime = newInjector.^keepTime;
        positionX = new (newInjector.^positionX)();
        positionY = new (newInjector.^positionY)();
        width = new (newInjector.^width)();
        height = new (newInjector.^height)();
        rotation = new (newInjector.^rotation)();
        alpha = new (newInjector.^alpha)();
    }
}