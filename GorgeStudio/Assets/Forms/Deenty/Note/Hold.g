using Gorge;
using GorgeFramework;
namespace Deenty;

[
    delegate<float:Hold^> display = string:(Hold^ holdInjector) ->
    {
        return holdInjector.^respondMoment + " [" + holdInjector.^respondTime + "] | " + holdInjector.^laneName + ":" +
               holdInjector.^positionY.^baseValue + " | " + holdInjector.^holdTime + " " + holdInjector.^respondQuantity;
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 0.8, g : 0.2392157, b : 0.6224183},
    delegate<ElementLine:Hold^> elementLine = ElementLine:(Hold^ holdInjector) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[2];
        points[0] = new ElementLinePoint(holdInjector.^respondMoment, 0.5, 0.2);
        points[1] = new ElementLinePoint(holdInjector.^respondMoment + holdInjector.^holdTime, 0.5, 0.2);
        return new ElementLine(new ColorArgb(1, 0.8, 0.447059, 0.682353), points);
    }
]
@EditableElement(type = "Note", editUpdateMode = EditUpdateMode.ReInject)
class Hold : DeentyLineNote
{
    [
        auto defaultValue = 1.0,
        string type = "基本",
        int order = 200,
        string displayName = "保持时间",
        string information = "单位秒，>0",
        delegate<bool:float> check = bool:(float holdTime) -> { return holdTime > 0; }
    ]
    @Inject
    float holdTime = ^holdTime;
    
    [
        auto defaultValue = 2,
        string type = "基本",
        int order = 201,
        string displayName = "判定点数",
        string information = "个数，>=2",
        delegate<bool:int> check = bool:(int respondQuantity) -> { return respondQuantity >= 2; }
    ]
    @Inject
    int respondQuantity = ^respondQuantity;
    
    [
        auto defaultValue = LinearCurve : {timeStart : 0.0, valueStart : 0.0, timeEnd : 2.0, valueEnd : 2.0},
        string type = "效果",
        int order = 202,
        string displayName = "头进度曲线",
        string information = "横轴为响应进度，0代表出现时刻，1代表响应时刻；纵轴为运动进度，0代表轨道末端，1代表判定线",
        delegate<bool:FunctionCurve> check = bool:(FunctionCurve positionStartXCurve) -> { return true; },
        string convertFrom = "PositionXCurve LoopSizeCurve"
    ]
    @Inject<FunctionCurve^>
    FunctionCurve positionStartXCurve = new ^positionStartXCurve();
    
    [
        auto defaultValue = LinearCurve : {timeStart : 0.0, valueStart : 0.0, timeEnd : 2.0, valueEnd : 2.0},
        string type = "效果",
        int order = 203,
        string displayName = "尾进度曲线",
        string information = "横轴为响应进度，0代表出现时刻，1代表响应时刻；纵轴为运动进度，0代表轨道末端，1代表判定线",
        delegate<bool:FunctionCurve> check = bool:(FunctionCurve positionStartXCurve) -> { return true; },
        string convertFrom = "PositionXCurve LoopSizeCurve"
    ]
    @Inject<FunctionCurve^>
    FunctionCurve positionEndXCurve = new ^positionEndXCurve();
    
    [
        delegate<float:Hold^> time = float:(Hold^ noteInjector) ->
        {
            return Hold.InjectorGenerateTime(noteInjector);
        }
    ]
    @ForwardTimedGenerate
    [
        delegate<float:Hold^> time = float:(Hold^ noteInjector) ->
        {
            return Hold.InjectorDestroyTime(noteInjector);
        }
    ]
    @BackwardTimedGenerate
    @EditGenerate
    Hold(bool isAutoPlay, bool isReverse) : super()
    {
        ITransformer[] transformers = new ITransformer[7];
        transformers[0] = new LineNotePositionYTransformer(this);
        transformers[1] = new LineStripNotePositionXTransformer(this);
        transformers[2] = new NotePositionZTransformer(this);
        transformers[3] = new LineNoteLengthTransformer(this);
        transformers[4] = new NoteLaneTransformer(this){laneType : "Line"};
        transformers[5] = new NoteColorTransformer(this){type : "Hold"};
        transformers[6] = new NoteChordTransformer(this){type : "Hold"};
        
        // GorgeGraphics
        ImageAsset lineImage = (ImageAsset) Environment.GetAssetByName("image:Deenty/FormAsset/Hold");
        graphNode = new NineSliceSprite(lineImage.texture, new Vector2(169, 169), new Vector2(169, 169), new Vector2(0.2, 0.2));
        graphNode.size.x = 0.2;
        
        nodes = new Node[1];
        nodes[0] = graphNode;
        
        if (isAutoPlay)
        {
            HoldAuto initializer = new HoldAuto(this, isReverse);
            automaton = new SignalTsiga(this, initializer.timeStack, initializer.inputGraph, initializer.historyStack);
        }
        else
        {
            HoldNormal initializer = new HoldNormal(this, isReverse);
            automaton = new SignalTsiga(this, initializer.timeStack, initializer.inputGraph, initializer.historyStack);
        }
        
        simulator = new ElementSimulator(transformers);
    }
    
    float HideMoment()
    {
        return respondMoment + holdTime + stayTime;
    }
    
    string DisplayString()
    {
        return respondMoment + " [" + respondTime + "] | " + laneName + ":" + positionY.baseValue + " | " + holdTime + " " + respondQuantity;
    }
    
    float AutoPlayHoldTime()
    {
        return holdTime;
    }
    
    @ForwardTimedDestroy
    float ForwardDestroyTime()
    {
        Line line = (Line) Environment.FindAliveLane("Deenty.Line", laneName);
        if (line == null)
        {
            return Math.FloatPositiveInfinity();
        }
        float noteDestroyTime = respondMoment + stayTime + holdTime;
        float judgementEndTime = line.MissRespondEndMoment(respondMoment);
        return Math.Max(noteDestroyTime, judgementEndTime);
    }
    
    @BackwardTimedDestroy
    float BackwardDestroyTime()
    {
        Line line = (Line) Environment.FindAliveLane("Deenty.Line", laneName);
        if (line == null)
        {
            return Math.FloatNegativeInfinity();
        }
        float noteGenerateTime = respondMoment - respondTime;
        float judgementBeginTime = line.MissRespondStartMoment(respondMoment);
        return Math.Min(noteGenerateTime, judgementBeginTime);
    }
    
    @EditReInject
    void ReInjectHold(Hold^ newInjector)
    {
        ReInjectDeentyLineNote(newInjector);
        holdTime = newInjector.^holdTime;
        respondQuantity = newInjector.^respondQuantity;
        positionStartXCurve = new (newInjector.^positionStartXCurve)();
        positionEndXCurve = new (newInjector.^positionEndXCurve)();
    }
    
    @EditTryGenerate
    static bool TryGenerate(Hold^ newInjector, float chartTime)
    {
        float beginTime = Hold.InjectorGenerateTime(newInjector);
        float destroyTime = Hold.InjectorDestroyTime(newInjector);
        return chartTime >= beginTime && chartTime <= destroyTime;
    }
    
    static float InjectorGenerateTime(Hold^ noteInjector)
    {
        Line line = (Line) Environment.FindAliveLane("Deenty.Line", noteInjector.^laneName);
        if (line == null)
        {
            return Math.FloatPositiveInfinity();
        }
        float noteBeginTime = noteInjector.^respondMoment - noteInjector.^respondTime + noteInjector.^holdTime;
        float judgementBeginTime = line.MissRespondStartMoment(noteInjector.^respondMoment);
        return Math.Min(noteBeginTime, judgementBeginTime);
    }
    
    static float InjectorDestroyTime(Hold^ noteInjector)
    {
        Line line = (Line) Environment.FindAliveLane("Deenty.Line", noteInjector.^laneName);
        if (line == null)
        {
            return Math.FloatNegativeInfinity();
        }
        float noteDestroyTime = noteInjector.^respondMoment + noteInjector.^stayTime;
        float judgementEndTime = line.MissRespondEndMoment(noteInjector.^respondMoment);
        return Math.Max(noteDestroyTime, judgementEndTime);
    }
}