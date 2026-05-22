using Gorge;
using GorgeFramework;
namespace Reincal;

[
    delegate<float:ReincalSliderLane^> display = string:(ReincalSliderLane^ laneInjector) ->
    {
        return "滑动轨道";
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 0.2396693, g : 0.6370158, b : 0.8},
    string displayName = "滑动轨道"
]
@EditableElement(type = "轨道", editUpdateMode = EditUpdateMode.RePlay)
class ReincalSliderLane : Note
{
    string name = "SliderLane";
    
    float rotation = 0.0;

    int touchSignalId;

    int handlingSignalId;
    float baseRotation;
    float baseAngle;
    float nowAngle;

    [
        auto defaultValue = null,
        string type = "基本",
        int order = 1,
        string displayName = "圆心坐标",
        string information = "标准坐标系",
        delegate<bool:Vector2^> check = bool:(Vector2^ basePoint) -> { return true; }
    ]
    @Inject<Vector2^>
    Vector2 basePoint = ^basePoint == null ? new Vector2(0, 4.285714) : (new ^basePoint());
    
    [
        auto defaultValue = null,
        string type = "效果",
        int order = 2,
        string displayName = "轨道颜色",
        string information = "横轴为以生成时刻为0的时间，单位秒",
        delegate<bool:ColorCurve^> check = bool:(ColorCurve^ color) -> { return true; }
    ]
    @Inject<ColorCurve^>
    ColorCurve color = (^color == null) ? null : (new ^color());
    
    Sprite graphNode;
    
    @InitializeGenerate
    ReincalSliderLane(bool isAutoPlay, bool isReverse) : super()
    {
        ImageAsset lineImage = (ImageAsset) Environment.GetAssetByName("image:Reincal/FormAsset/SliderRing");
        graphNode = new Sprite(lineImage.texture);
        if (basePoint != null)
        {
            graphNode.position.x = basePoint.x;
            graphNode.position.y = basePoint.y;
        }
        else
        {
            graphNode.position.y = 4.285714;
        }
        graphNode.position.z = -2;
        graphNode.size.x = 19.14;
        graphNode.size.y = 19.14;
        graphNode.rotation.z = rotation;
        
        nodes = new Node[1];
        nodes[0] = graphNode;

        if(!isAutoPlay)
        {
            SliderLaneTsiga initializer = new SliderLaneTsiga(this, isReverse);
            automaton = new SignalTsiga(this, initializer.timeStack, initializer.inputGraph, initializer.historyStack);
        }
        
        ITransformer[] transformers;
        if(isAutoPlay)
        {
            transformers = new ITransformer[2];
            transformers[0] = new ReincalLaneAutoplayRotationTransformer(this);
            transformers[1] = new ReincalSliderLaneSignalTransformer(this);
        }
        else
        {
            transformers = new ITransformer[1];
            transformers[0] = new ReincalSliderLaneSignalTransformer(this);
        }
        
        ITransformer[] lateTransformers = new ITransformer[2];
        lateTransformers[0] = new ReincalLaneRotationTransformer(this);
        lateTransformers[1] = new ReincalLaneColorTransformer(this);
        
        simulator = new ElementSimulator(transformers);
        lateIndependentSimulator = new ElementSimulator(lateTransformers);
    }
    
    ObjectList<ReincalSliderLaneAutoMoveTarget> autoMoveTargets = new ObjectList<ReincalSliderLaneAutoMoveTarget>(){length : 0};
    
    void EnqueueAutoTarget(float time, float targetPosition)
    {
        autoMoveTargets.Add(new ReincalSliderLaneAutoMoveTarget(time, targetPosition));
    }

    float GetTouchAngle(TouchSignal signal)
    {
        Vector2 touchPosition = signal.position;
        Vector2 touchVector = new Vector2(touchPosition.x - basePoint.x, touchPosition.y - basePoint.y);
        return Vector2.Angle(touchVector) + 90;
    }

    bool IsInTouchArea(TouchSignal signal)
    {
        Vector2 touchPosition = signal.position;
        float touchAngle = GetTouchAngle(signal);
        float rotationAngle = rotation * 180 / 3.1415926;
        if(Math.Abs(touchAngle - rotationAngle) > 20)
        {
            return false;
        }
        float distance = Vector2.Distance(touchPosition, basePoint);
        // 7.2 ~ 10
        return Math.Abs(distance - 8.6) <= 1.4;
    }

}