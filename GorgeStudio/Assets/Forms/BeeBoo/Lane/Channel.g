using Gorge;
using GorgeFramework;
namespace BeeBoo;

[
    delegate<float:LaneSet^> display = string:(LaneSet^ laneInjector) ->
    {
        return "触控通道 #" + laneInjector.^id;
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 0.2396693, g : 0.6370158, b : 0.8},
    delegate<ElementLine:LaneSet^> elementLine = ElementLine:(LaneSet^ lane) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[0];
        return new ElementLine(new ColorArgb(0.3, 1, 1, 1), points);
    },
    string displayName = "触控通道"
]
@EditableElement(type = "轨道", editUpdateMode = EditUpdateMode.ReInject)
class Channel : Element
{
    // 注入字段
    [
        string type = "基本",
        int order = 0,
        string displayName = "通道编号",
        string information = "不能为空",
        delegate<bool:int> check = bool:(int id) -> { return true; }
    ]
    @Inject
    int id = ^id;
    
    [
        auto defaultValue = VariableFloat : {baseValue : 0.0},
        string type = "基本",
        int order = 1,
        string displayName = "使能",
        string information = ">=0时信号有效，否则无效",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ enable) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat enable = new ^enable();

    [
        auto defaultValue = LerpColorCurve : {colorPoints : ColorArgb^ : { ColorArgb : { a : 1, r : 1, g : 1, b : 1}}},
        string type = "基本",
        int order = 100,
        string displayName = "通道颜色",
        string information = "通道颜色变化曲线",
        delegate<bool:VariableFloat^> check = bool:(ColorCurve^ channelColor) -> { return true; }
    ]
    @Inject<ColorCurve^>
    ColorCurve channelColor = new ^channelColor();
    
    // 时变字段
    float nowEnable;
    ColorArgb nowColor;
    
    // 存储字段
    bool set;
    ObjectList<ChannelAutoHitTarget> autoSetTargets = new ObjectList<ChannelAutoHitTarget>(){length : 0};

    @InitializeGenerate
    @EditGenerate
    Channel() : super()
    {
        ITransformer[] transformers = new ITransformer[3];
        transformers[0] = new ChannelTimeVaryingVariableTransformer(this);
        transformers[1] = new ChannelSignalTransformer(this);
        transformers[2] = new ChannelAutoplayTransformer(this);
        simulator = new ElementSimulator(transformers);
    }

    @EditTryGenerate
    static bool TryGenerate(Channel^ newInjector, float chartTime)
    {
        return true;
    }

    @EditReInject
    void ReInjectTracker(Channel^ newInjector)
    {
        id = newInjector.^id;
        enable = new (newInjector.^enable)();
        channelColor = new (newInjector.^channelColor)();
    }

    void EnqueueAutoTarget(float time, float holdTime)
    {
        autoSetTargets.Add(new ChannelAutoHitTarget(time, holdTime));
    }
}