using Gorge;
using GorgeFramework;
namespace Reincal;

[
    delegate<float:ReincalSliderHold^> display = string:(ReincalSliderHold^ noteInjector) ->
    {
        return noteInjector.^hitTime + "";
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 0.2941176470588235, g : 0.3607843137254902, b : 0.7686274509803922},
    delegate<ElementLine:ReincalSliderHold^> elementLine = ElementLine:(ReincalSliderHold^ noteInjector) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[2];
        points[0] = new ElementLinePoint(noteInjector.^hitTime, 0.1, 0.2);
        points[1] = new ElementLinePoint(noteInjector.^hitTime + noteInjector.^holdTime, 0.1, 0.2);
        return new ElementLine(new ColorArgb(1, 0.2941176470588235, 0.3607843137254902, 0.7686274509803922), points);
    },
    string displayName = "滑动Hold"
]
@EditableElement(type = "Note", editUpdateMode = EditUpdateMode.RePlay)
class ReincalSliderHold : SliderNote
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
        auto defaultValue = ConstantFunctionCurve : {value : 0.0},
        string type = "基本",
        int order = 7,
        string displayName = "Slider轨迹",
        string information = "标准坐标系|横轴以打击时刻为0点的时间，单位秒；纵轴为距离在轨道上的角度",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ holdLine) -> { return true; }
    ]
    @Inject<FunctionCurve^>
    FunctionCurve sliderLine = new ^sliderLine();
    
    [
        auto defaultValue = 100,
        string type = "效果",
        int order = 2005,
        string displayName = "绘制精度",
        string information = "采样段数，>1",
        delegate<bool:int> check = bool:(int pointCount) -> { return pointCount > 1; }
    ]
    @Inject
    int pointCount = ^pointCount;
    
    [
        auto defaultValue = 100,
        string type = "效果",
        int order = 2006,
        string displayName = "Autoplay移动精度",
        string information = "移动段数，>1",
        delegate<bool:int> check = bool:(int autoplayTargetCount) -> { return autoplayTargetCount > 1; }
    ]
    @Inject
    int autoplayTargetCount = ^autoplayTargetCount;
    
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
    
    // 使用二次函数映射前的判定线半径
    float judgementLineUnmappedRadius;
    
    ReincalSliderLane lane;
    
    MeshedSprite headGraphNode;
    MeshedSprite tailGraphNode;
    
    MeshedSprite holdGraphNode;
    
    LinearFunctionCurve holdLaneMapping;

    float judgementPosition;
    
    [
        delegate<float:ReincalSliderHold^> time = float:(ReincalSliderHold^ noteInjector) ->
        {
            return noteInjector.^hitTime - noteInjector.^leadTime;
        }
    ]
    @ForwardTimedGenerate
    [
        delegate<float:ReincalSliderHold^> time = float:(ReincalSliderHold^ noteInjector) ->
        {
            return noteInjector.^hitTime + noteInjector.^lagTime + noteInjector.^holdTime;
        }
    ]
    @BackwardTimedGenerate
    @EditGenerate
    ReincalSliderHold(bool isAutoPlay, bool isReverse) : super()
    {
        lane = (ReincalSliderLane) Environment.FindAliveLane("Reincal.ReincalSliderLane", "SliderLane");
        
        if (lane == null)
        {
            return null;
        }

        judgementPosition = position;

        if (isAutoPlay)
        {
            float step = holdTime / autoplayTargetCount;
            for (float time = hitTime; time <= hitTime + holdTime; time = time + step)
            {
                lane.EnqueueAutoTarget(time, position + sliderLine.Evaluate(time - hitTime));
            }
        }
        
        // 计算网格变形器参数
        // Note落在判定线上时的弧长
        float finalArcLength = 2;
        // Note落在判定线上的径向高度
        float finalHeight = 0.4;
        
        // 计算弧长圆心角
        float centerAngleR = finalArcLength / judgementLineRadius;
        float halfCenterAngleR = centerAngleR / 2;
        
        // GorgeGraphics
        ImageAsset sliderTapImage = (ImageAsset) Environment.GetAssetByName("image:Reincal/FormAsset/SliderTap");
        headGraphNode = new MeshedSprite(sliderTapImage.texture);
        headGraphNode.width = finalArcLength;
        headGraphNode.height = finalHeight;
        headGraphNode.horizontalSegments = 10;
        headGraphNode.verticalSegments = 1;
        headGraphNode.size.y = 1;
        headGraphNode.size.x = 1;
        headGraphNode.position.z = -1.1;
        headGraphNode.positionReference = lane.graphNode;
        
        // 计算映射前判定线距离
        judgementLineUnmappedRadius = Math.Sqrt(2 * judgementLineRadius * judgementLineRadius);
        AnnulusMeshTransformer meshTransformer = new AnnulusMeshTransformer();
        meshTransformer.yRadius = new QuadraticFunctionCurve(0.5 / judgementLineRadius, 0, 0);
        meshTransformer.xAngle = new LinearCurve(-1, -halfCenterAngleR + position + 4.71233889, 1, halfCenterAngleR + position + 4.71233889);
        
        headGraphNode.AddMeshTransformer(meshTransformer);
        
        ImageAsset sliderHoldImage = (ImageAsset) Environment.GetAssetByName("image:Reincal/FormAsset/SliderHold");
        holdGraphNode = new MeshedSprite(sliderHoldImage.texture);
        holdGraphNode.width = finalArcLength - 0.4;
        holdGraphNode.height = judgementLineUnmappedRadius;
        holdGraphNode.centerY = -judgementLineUnmappedRadius / 2;
        holdGraphNode.horizontalSegments = 10;
        holdGraphNode.verticalSegments = pointCount;
        holdGraphNode.size.y = 1;
        holdGraphNode.size.x = 1;
        holdGraphNode.position.z = -1.09;
        holdGraphNode.positionReference = lane.graphNode;
        
        CurveMeshTransformer meshTransformer2 = new CurveMeshTransformer();
        meshTransformer2.isHorizontal = true;
        holdLaneMapping = new LinearFunctionCurve(holdTime / holdLength, 0);
        
        MultiplicationFunctionCurve sliderMapping = new MultiplicationFunctionCurve(sliderLine, new ConstantFunctionCurve(1 / halfCenterAngleR));
        meshTransformer2.curve = new CompositeFunctionCurve(sliderMapping, holdLaneMapping);
        
        AnnulusMeshTransformer meshTransformer3 = new AnnulusMeshTransformer();
        meshTransformer3.yRadius = new QuadraticFunctionCurve(0.5 / judgementLineRadius, 0, 0);
        //meshTransformer3.xAngle = new LinearCurve(-1, -halfCenterAngleR + position + 4.71233889, 1, halfCenterAngleR + position + 4.71233889);
        meshTransformer3.xAngle = new LinearFunctionCurve(halfCenterAngleR, position + 4.71233889);
        
        holdGraphNode.AddMeshTransformer(meshTransformer2);
        holdGraphNode.AddMeshTransformer(meshTransformer3);
        
        tailGraphNode = new MeshedSprite(sliderTapImage.texture);
        tailGraphNode.width = finalArcLength;
        tailGraphNode.height = finalHeight;
        tailGraphNode.horizontalSegments = 10;
        tailGraphNode.verticalSegments = 1;
        tailGraphNode.size.y = 1;
        tailGraphNode.size.x = 1;
        tailGraphNode.position.z = -1.1;
        tailGraphNode.positionReference = lane.graphNode;
        
        // 计算映射前判定线距离
        AnnulusMeshTransformer meshTransformer4 = new AnnulusMeshTransformer();
        meshTransformer4.yRadius = new QuadraticFunctionCurve(0.5 / judgementLineRadius, 0, 0);
        float finalPosition = sliderLine.Evaluate(holdTime);
        meshTransformer4.xAngle = new LinearCurve(-1, -halfCenterAngleR + position + finalPosition + 4.71233889,
            1, halfCenterAngleR + position + finalPosition + 4.71233889);
        
        tailGraphNode.AddMeshTransformer(meshTransformer4);
        
        nodes = new Node[3];
        nodes[0] = headGraphNode;
        nodes[1] = holdGraphNode;
        nodes[2] = tailGraphNode;

        ReincalSliderHoldTsiga initializer = new ReincalSliderHoldTsiga(this, isReverse);
        automaton = new SignalTsiga(this, initializer.timeStack, initializer.inputGraph, initializer.historyStack);
        
        ITransformer[] transformers = new ITransformer[1];
        transformers[0] = new SliderHoldJudgementPositionTransformer(this);
        simulator = new ElementSimulator(transformers);

        ITransformer[] lateTransformers = new ITransformer[1];
        lateTransformers[0] = new ReincalSliderHoldPositionTransformer(this);
        
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
    static bool TryGenerate(ReincalSliderHold^ newInjector, float chartTime)
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
        
        ReincalSliderRespondHint respondHint;
        
        respondHint = new ReincalSliderRespondHint(judgementPosition, judgementLineRadius, respondChartTime, lane.graphNode);
        
        IAutomatonCommand[] commands = new IAutomatonCommand[1];
        commands[0] = new DeriveElementCommand(respondHint, false);
        return commands;
    }
}