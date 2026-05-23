using Gorge;
using GorgeFramework;
namespace Obsertor;

[
    delegate<float:Flick^> display = string:(Hold^ noteInjector) ->
    {
        return noteInjector.^hitTime + "";
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 0.5843137254901961, g : 0.2509803921568627, b : 0.7215686274509804},
    delegate<ElementLine:Hold^> elementLine = ElementLine:(Hold^ noteInjector) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[2];
        points[0] = new ElementLinePoint(noteInjector.^hitTime, 0.41666666, 0.16666666);
        points[1] = new ElementLinePoint(noteInjector.^hitTime + noteInjector.^holdTime, 0.41666666, 0.16666666);
        return new ElementLine(new ColorArgb(1,0.5843137254901961,0.2509803921568627,0.7215686274509804), points);
    },
    string displayName = "Hold"
]
@EditableElement(type = "Note", editUpdateMode = EditUpdateMode.RePlay)
class Hold : LineNote
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
        delegate<bool:HoldInnerNote^[]^> check = bool:(HoldInnerNote^[]^ innerNotes) -> { return true; }
    ]
    @Inject<HoldInnerNote^[]^>
    HoldInnerNote^[] innerNotes = (^innerNotes == null) ? null : (new (^innerNotes)[^innerNotes.length]);
    
    [
        auto defaultValue = LinearFunctionCurve : {k : -96.0, b : 0.0},
        string type = "基本",
        int order = 6,
        string displayName = "尾部距离曲线",
        string information = "标准坐标系|横轴为以释放时刻为0点的时间，单位秒；纵轴为距离轨道判定线的距离",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ endDistance) -> { return true; },
        float scaleMax = 11.5,
        float scaleMin = -11.5,
        string baseAt = "HoldEnd"
    ]
    @Inject<FunctionCurve^>
    FunctionCurve endDistance = new ^endDistance();
    
    [
        auto defaultValue = ConstantFunctionCurve : {value : 0.0},
        string type = "基本",
        int order = 7,
        string displayName = "Hold轨迹",
        string information = "标准坐标系|横轴以打击时刻为0点的时间，单位秒；纵轴为距离在轨道上的相对位置",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ holdLine) -> { return true; }
    ]
    @Inject<FunctionCurve^>
    FunctionCurve holdLine = new ^holdLine();
    
    [
        auto defaultValue = true,
        string type = "基本",
        int order = 8,
        string displayName = "轨迹截断",
        string information = "将尾部外的轨迹截断，否则压缩",
        delegate<bool:bool> check = bool:(bool truncation) -> { return true; }
    ]
    @Inject
    bool truncation = ^truncation;

    [
        auto defaultValue = 1,
        string type = "基本",
        int order = 9,
        string displayName = "标准轨迹长度",
        string information = "在轨迹截断模式下holdTime长度的轨迹对应的实际距离，不为0",
        delegate<bool:float> check = bool:(float holdLength) -> { return holdLength != 0; },
        float scaleMax = 20.0,
        float scaleMin = 0.0
    ]
    @Inject
    float holdLength = ^holdLength;

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

    HoldInnerNote[] innerNoteConfigs;
    
    LinearFunctionCurve holdLaneMapping;

    float nowPosition;

    float depth = 96;

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
            return noteInjector.^hitTime + noteInjector.^holdTime + noteInjector.^lagTime;
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
        
        if (innerNotes == null)
        {
            innerNoteConfigs = null;
        }
        else
        {
            innerNoteConfigs = new HoldInnerNote[innerNotes.length];
            for (int i = 0; i < innerNotes.length; i = i + 1)
            {
                if (innerNotes[i] == null)
                {
                    innerNoteConfigs[i] = null;
                }
                else
                {
                    innerNoteConfigs[i] = new innerNotes[i]();
                }
            }
        }
        
        Node sliderLaneBase = lane.lineGraphNode;

        ImageAsset lineImage = (ImageAsset) Environment.GetAssetByName("image:Obsertor/FormAsset/Hold200");
        graphNode = new MeshedSprite(lineImage.texture);
        graphNode.position.z = 0;
        graphNode.position.x = position;
        graphNode.width = 3;
        graphNode.height = depth;
        graphNode.centerY = -depth / 2;
        graphNode.size.x = 1;
        graphNode.size.y = 1;
        graphNode.positionReference = sliderLaneBase;
        graphNode.horizontalSegments = 10;
        graphNode.verticalSegments = pointCount;
        graphNode.rotation.x = 90;


        CurveMeshTransformer meshTransformer = new CurveMeshTransformer();
        meshTransformer.isHorizontal = true;
        holdLaneMapping = new LinearFunctionCurve(holdTime / holdLength, 0);

        meshTransformer.curve = new CompositeFunctionCurve(holdLine, holdLaneMapping);
        
        CurveWarpTransformer meshTransformer2 = new CurveWarpTransformer();
        CompositeFunctionCurve meshCurve = new CompositeFunctionCurve();
        meshCurve.innerFunctionCurve = new LinearFunctionCurve(1, position);
        meshCurve.outerFunctionCurve = lane.curve;
        CompositeFunctionCurve meshCurve2 = new CompositeFunctionCurve();
        meshCurve2.innerFunctionCurve = meshCurve;
        meshCurve2.outerFunctionCurve = new LinearFunctionCurve(-1, 0);
        
        meshTransformer2.curve = meshCurve2;
        meshTransformer2.curveValueAxis = Axis.Z;
        
        graphNode.AddMeshTransformer(meshTransformer);
        graphNode.AddMeshTransformer(meshTransformer2);
        
        nodes = new Node[1];
        nodes[0] = graphNode;
        
        if (isAutoPlay)
        {
            HoldAutoplayTsiga initializer = new HoldAutoplayTsiga(this, isReverse);
            automaton = new SignalTsiga(this, initializer.timeStack, initializer.inputGraph, initializer.historyStack);
        }
        else
        {
            HoldTsiga initializer = new HoldTsiga(this, isReverse);
            automaton = new SignalTsiga(this, initializer.timeStack, initializer.inputGraph, initializer.historyStack);
        }

        ITransformer[] lateTransformers = new ITransformer[2];
        lateTransformers[0] = new LineHoldPositionTransformer(this);
        lateTransformers[1] = new LineNoteColorTransformer(this);
        
        lateIndependentSimulator = new ElementSimulator(lateTransformers);
    }
    
    @ForwardTimedDestroy
    float ForwardDestroyTime()
    {
        return hitTime + holdTime + lagTime;
    }
    
    @BackwardTimedDestroy
    float BackwardDestroyTime()
    {
        return hitTime - leadTime;
    }
    
    @EditTryGenerate
    static bool TryGenerate(Hold^ newInjector, float chartTime)
    {
        return chartTime >= (newInjector.^hitTime - newInjector.^leadTime) && chartTime <= (newInjector.^hitTime + newInjector.^holdTime + newInjector.^lagTime);
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
        
        RespondHint respondHint;

        ColorArgb color = new ColorArgb(1,0.2588235294117647,0.5176470588235294,0.9490196078431373);

        float t = respondChartTime - hitTime;

        float x = position + holdLine.Evaluate(t);

        respondHint = new RespondHint(new Vector2(x,lane.curve.Evaluate(x)), respondChartTime, null, color, new ColorArgb(1,1,1,1));
        
        IAutomatonCommand[] commands = new IAutomatonCommand[1];
        commands[0] = new DeriveElementCommand(respondHint, false);
        return commands;
    }

    float GetAimDistance(TouchSignal signal)
    {
        return Vector2.Distance(signal.position, new Vector2(nowPosition, lane.curve.Evaluate(nowPosition)));
    }
}