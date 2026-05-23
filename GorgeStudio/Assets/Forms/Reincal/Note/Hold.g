using Gorge;
using GorgeFramework;
namespace Reincal;

[
    delegate<float:Hold^> display = string:(Hold^ noteInjector) ->
    {
        return noteInjector.^hitTime + "";
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 0.2666666666666667, g : 0.807843137254902, b : 0.9647058823529412},
    delegate<ElementLine:Hold^> elementLine = ElementLine:(Hold^ noteInjector) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[2];
        points[0] = new ElementLinePoint(noteInjector.^hitTime, 0.5, 0.2);
        points[1] = new ElementLinePoint(noteInjector.^hitTime + noteInjector.^holdTime, 0.5, 0.2);
        return new ElementLine(new ColorArgb(1, 0.2666666666666667, 0.807843137254902, 0.9647058823529412), points);
    },
    string displayName = "下落Hold"
]
@EditableElement(type = "Note", editUpdateMode = EditUpdateMode.RePlay)
class Hold : NormalNote
{

    [
        auto defaultValue = 1.0,
        string type = "基本",
        int order = 4,
        string displayName = "保持时间",
        string information = "单位秒，>0",
        delegate<bool:float> check = bool:(float holdTime) -> { return holdTime > 0; },
        string timePointName = "HoldEnd",
        string timePointEarlyAnchor = "HitTime",
        string timePointLateAnchor = null,
        string timePointRegion = "RhythmDuration"
    ]
    @Inject
    float holdTime = ^holdTime;

    [
        auto defaultValue = null,
        string type = "基本",
        int order = 5,
        string displayName = "内部打击时刻",
        string information = "内部打击时刻列表",
        delegate<bool:float[]^> check = bool:(float[]^ innerNotes) -> { return true; }
    ]
    @Inject<float[]^>
    float[] innerNotes = (^innerNotes == null) ? (new float[0]) : (new (^innerNotes)[^innerNotes.length]);
    
    [
        auto defaultValue = LinearFunctionCurve : {k : -8.0, b : 0.0},
        string type = "基本",
        int order = 6,
        string displayName = "尾部距离曲线",
        string information = "标准坐标系|横轴为以释放时刻为0点的时间，单位秒；纵轴为距离轨道判定线的距离",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ endDistance) -> { return true; }
    ]
    @Inject<FunctionCurve^>
    FunctionCurve endDistance = new ^endDistance();

    [
        auto defaultValue = 1,
        string type = "基本",
        int order = 9,
        string displayName = "标准轨迹长度",
        string information = "在轨迹截断模式下holdTime长度的轨迹对应的实际距离，不为0",
        delegate<bool:float> check = bool:(float holdLength) -> { return holdLength != 0; }
    ]
    @Inject
    float holdLength = ^holdLength;

    Sprite tailGraphNode;
    
    MeshedSprite holdGraphNode;

    [
        delegate<float:Hold^> time = float:(Hold^ noteInjector) ->
        {
            return noteInjector.^hitTime - noteInjector.^leadTime;
        }
    ]
    @ForwardTimedGenerate
    [
        delegate<float:Hold^> time = float:(Hold^ noteInjector) ->
        {
            return noteInjector.^hitTime + noteInjector.^lagTime + noteInjector.^holdTime;
        }
    ]
    @BackwardTimedGenerate
    @EditGenerate
    Hold(bool isAutoPlay, bool isReverse) : super()
    {
        if (lane == null)
        {
            return null;
        }

        if (isAutoPlay)
        {
            lane.EnqueueAutoTarget(hitTime, holdTime);
        }
        
        Node sliderLaneBase = lane.graphNode;
        
        ImageAsset headImage = (ImageAsset) Environment.GetAssetByName("image:Reincal/FormAsset/HoldHead");
        graphNode = new Sprite(headImage.texture);
        graphNode.position.z = -2;
        graphNode.size.x = size;
        graphNode.size.y = size;
        graphNode.positionReference = sliderLaneBase;
        graphNode.rotationReference = sliderLaneBase;
        graphNode.color = new ColorArgb(1, 1, 1, 1);

        ImageAsset holdImage = (ImageAsset) Environment.GetAssetByName("image:Reincal/FormAsset/SliderHold");
        holdGraphNode = new MeshedSprite(holdImage.texture);
        holdGraphNode.position.y = lane.radius;
        holdGraphNode.positionReference = sliderLaneBase;
        holdGraphNode.rotationReference = sliderLaneBase;

        // Note落在判定线上时的弧长
        float finalArcLength = size * 1.5;
        
        // 计算弧长圆心角
        float centerAngleR = finalArcLength / lane.radius;
        float halfCenterAngleR = centerAngleR / 2;
        AnnulusMeshTransformer meshTransformer = new AnnulusMeshTransformer();
        meshTransformer.yRadius = new LinearFunctionCurve(1.0, 0);
        // meshTransformer.xAngle = new LinearFunctionCurve(halfCenterAngleR, 4.71233889);
        meshTransformer.xAngle = new LinearFunctionCurve(halfCenterAngleR, 4.71233889);
        
        holdGraphNode.AddMeshTransformer(meshTransformer);

        tailGraphNode = new Sprite(headImage.texture);
        tailGraphNode.position.z = -2;
        tailGraphNode.size.x = size;
        tailGraphNode.size.y = size;
        tailGraphNode.rotation.z = 180;
        tailGraphNode.positionReference = sliderLaneBase;
        tailGraphNode.rotationReference = sliderLaneBase;
        tailGraphNode.color = new ColorArgb(1, 1, 1, 1);

        nodes = new Node[3];
        nodes[0] = graphNode;
        nodes[1] = holdGraphNode;
        nodes[2] = tailGraphNode;
        
        HoldTsiga initializer = new HoldTsiga(this, isReverse);
        automaton = new SignalTsiga(this, initializer.timeStack, initializer.inputGraph, initializer.historyStack);

        ITransformer[] lateTransformers = new ITransformer[1];
        lateTransformers[0] = new NormalHoldPositionTransformer(this);
        
        lateIndependentSimulator = new ElementSimulator(lateTransformers);
    }
    
    @ForwardTimedDestroy
    float ForwardDestroyTime()
    {
        return hitTime + lagTime + holdTime;
    }
    
    @BackwardTimedDestroy
    float BackwardDestroyTime()
    {
        return hitTime - leadTime;
    }
    
    @EditTryGenerate
    static bool TryGenerate(Hold^ newInjector, float chartTime)
    {
        return chartTime >= (newInjector.^hitTime - newInjector.^leadTime) && chartTime <= (newInjector.^hitTime + newInjector.^lagTime + newInjector.^holdTime);
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
        
        NormalRespondHint respondHint;
        
        respondHint = new NormalRespondHint(lane.graphNode, size, respondChartTime);
        
        IAutomatonCommand[] commands = new IAutomatonCommand[1];
        commands[0] = new DeriveElementCommand(respondHint, false);
        return commands;
    }
}