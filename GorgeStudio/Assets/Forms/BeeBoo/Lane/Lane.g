using Gorge;
using GorgeFramework;
namespace BeeBoo;

[
    string displayName = "轨道"
]
@Editable
class Lane : Element
{
    // 注入字段
    [
        auto defaultValue = ConstantFunctionCurve : {value : 0.0},
        string type = "基本",
        int order = 0,
        string displayName = "轨道曲线",
        string information = "标准坐标系",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ endDistance) -> { return true; }
    ]
    @Inject<FunctionCurve^>
    FunctionCurve laneCurve = new ^laneCurve();
    
    [
        auto defaultValue = VariableFloat : {baseValue : -1},
        string type = "效果",
        int order = 100,
        string displayName = "曲线绘制起点",
        string information = "本地时间，轨道曲线坐标",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ drawStartPosition) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat drawStartPosition = new ^drawStartPosition();
    
    [
        auto defaultValue = VariableFloat : {baseValue : 1},
        string type = "效果",
        int order = 101,
        string displayName = "曲线绘制中点",
        string information = "本地时间，轨道曲线坐标",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ drawEndPosition) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat drawEndPosition = new ^drawEndPosition();
    
    [
        auto defaultValue = -1.0,
        string type = "基本",
        int order = 1,
        string displayName = "追踪起点",
        string information = "轨道曲线坐标",
        delegate<bool:float> check = bool:(float startPosition) -> { return true; }
    ]
    @Inject
    float startPosition = ^startPosition;
    
    [
        auto defaultValue = 1.0,
        string type = "基本",
        int order = 2,
        string displayName = "追踪终点",
        string information = "轨道曲线坐标",
        delegate<bool:float> check = bool:(float endPosition) -> { return true; }
    ]
    @Inject
    float endPosition = ^endPosition;
    
    [
        auto defaultValue = 1.0,
        string type = "基本",
        int order = 3,
        string displayName = "轨道时长",
        string information = "追踪器经过轨道所用的时长",
        delegate<bool:float> check = bool:(float time) -> { return true; }
    ]
    @Inject
    float time = ^time;

    [
        auto defaultValue = VariableFloat : {baseValue : 0},
        string type = "基本",
        int order = 4,
        string displayName = "位置X",
        string information = "标准坐标",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ positionX) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat positionX = new ^positionX();

    [
        auto defaultValue = VariableFloat : {baseValue : 0},
        string type = "基本",
        int order = 5,
        string displayName = "位置Y",
        string information = "标准坐标",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ positionY) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat positionY = new ^positionY();

    [
        auto defaultValue = 0.0,
        string type = "基本",
        int order = 6,
        string displayName = "角度",
        string information = "轨道角度",
        delegate<bool:float> check = bool:(float rotation) -> { return true; }
    ]
    @Inject
    float rotation = ^rotation;

    [
        auto defaultValue = VariableFloat : {baseValue : 1},
        string type = "基本",
        int order = 7,
        string displayName = "缩放倍数",
        string information = "轨道缩放倍数",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ scale) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat scale = new ^scale();

    [
        auto defaultValue = VariableFloat : {baseValue : 1},
        string type = "效果",
        int order = 102,
        string displayName = "不透明度",
        string information = "本地时间，轨道曲线坐标",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ alpha) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat alpha = new ^alpha();

    [
        auto defaultValue = 100,
        string type = "效果",
        int order = 103,
        string displayName = "绘制精度",
        string information = "绘制点数量",
        delegate<bool:int> check = bool:(int pointCount) -> { return pointCount >= 2; }
    ]
    @Inject
    int pointCount = ^pointCount;
    
    // 构造字段
    float startTime;
    float keepTime;
    
    // 时变字段
    float localTime;
    float nowDrawStartPosition;
    float nowDrawEndPosition;
    float nowAlpha;
    float nowScale;
    float nowPositionX;
    float nowPositionY;
    
    // 存储字段
    CurveSprite laneNode;
    
    Lane(float startTime, float keepTime, Node laneSetBase) : super()
    {
        this.startTime = startTime;
        this.keepTime = keepTime;
        laneNode = new CurveSprite(null);
        laneNode.position.z = -1.2;
        laneNode.rotation.z = rotation;
        
        laneNode.positionReference = laneSetBase;
        laneNode.rotationReference = laneSetBase;
        laneNode.sizeReference = laneSetBase;
        
        nodes = new Node[1];
        nodes[0] = laneNode;
        
        ITransformer[] transformers = new ITransformer[3];
        transformers[0] = new LaneTimeVaryingVariableTransformer(this);

        transformers[1] = new LaneCurveTransformer(this);
        transformers[2] = new LanePositionTransformer(this);
        simulator = new ElementSimulator(transformers);
        
        ITransformer[] lateTransformers = new ITransformer[0];
        
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

    Vector2 GetPosition(float progress)
    {
        float curveX = (endPosition - startPosition) * progress + startPosition;
        Vector3 globalPosition = laneNode.LocalPositionToGlobalPosition(new Vector3(curveX * nowScale, laneCurve.Evaluate(curveX) * nowScale, 0));
        return new Vector2(globalPosition.x, globalPosition.y);
    }
}