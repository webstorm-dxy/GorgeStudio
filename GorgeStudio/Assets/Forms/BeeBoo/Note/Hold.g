using Gorge;
using GorgeFramework;
namespace BeeBoo;

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
    string displayName = "Hold"
]
@EditableElement(type = "Note", editUpdateMode = EditUpdateMode.ReInject)
class Hold : BeeBooNote
{
    [
        auto defaultValue = 1.0,
        string type = "基本",
        int order = 3,
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
        int order = 4,
        string displayName = "内部打击时刻",
        string information = "内部打击时刻列表",
        delegate<bool:float[]^> check = bool:(float[]^ innerNotes) -> { return true; }
    ]
    @Inject<float[]^>
    float[] innerNotes = (^innerNotes == null) ? (new float[0]) : (new (^innerNotes)[^innerNotes.length]);
   
    [
        auto defaultValue = LinearFunctionCurve : {k : 1.0, b : 0.0},
        string type = "基本",
        int order = 5,
        string displayName = "绘制起始点",
        string information = "标准坐标系|横轴为以释放时刻为0点的时间，单位秒；纵轴为距离轨道判定线的距离",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ drawStart) -> { return true; }
    ]
    @Inject<FunctionCurve^>
    FunctionCurve drawStart = new ^drawStart();

    [
        auto defaultValue = LinearFunctionCurve : {k : 1.0, b : 3.0},
        string type = "基本",
        int order = 6,
        string displayName = "绘制终止点",
        string information = "标准坐标系|横轴为以释放时刻为0点的时间，单位秒；纵轴为距离轨道判定线的距离",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ drawEnd) -> { return true; }
    ]
    @Inject<FunctionCurve^>
    FunctionCurve drawEnd = new ^drawEnd();

    [
        auto defaultValue = 100,
        string type = "效果",
        int order = 2005,
        string displayName = "绘制精度",
        string information = "采样点数，>1",
        delegate<bool:int> check = bool:(int pointCount) -> { return pointCount > 1; },
        float scaleMax = 1050.0,
        float scaleMin = 0.0
    ]
    @Inject
    int pointCount = ^pointCount;

    float nowDrawStart;
    float nowDrawEnd;

    CurveSprite holdBody;

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
        if (tracker == null)
        {
            return null;
        }
        
        if (isAutoPlay)
        {
            channel.EnqueueAutoTarget(hitTime, holdTime);
        }

        holdBody = new CurveSprite(null);
        holdBody.width = 0.4;
        holdBody.position.z = -2.7;

        nodes = new Node[4];
        nodes[0] = ringSprite;
        nodes[1] = circleSprite;
        nodes[2] = innerCircleSprite;
        nodes[3] = holdBody;

        HoldTsiga initializer = new HoldTsiga(this, isReverse, isAutoPlay);
        automaton = new SignalTsiga(this, initializer.timeStack, initializer.inputGraph, initializer.historyStack);
        
        ITransformer[] transformers = new ITransformer[4];
        transformers[0] = new NoteReinjectTransformer(this);
        transformers[1] = new NoteTimeVaryingVariableTransformer(this);
        transformers[2] = new HoldTimeVaryingVariableTransformer(this);
        transformers[3] = new HoldingHintTransformer(this);
        simulator = new ElementSimulator(transformers);

        ITransformer[] lateTransformers = new ITransformer[3];
        lateTransformers[0] = new NoteHeaderPositionTransformer(this);
        lateTransformers[1] = new NoteHeaderAnimationTransformer(this);
        lateTransformers[2] = new HoldBodyAnimationTransformer(this);
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

    @EditReInject
    void ReInjectHold(Hold^ newInjector)
    {
        ReInjectBeeBooNote(newInjector);
        holdTime = newInjector.^holdTime;
        innerNotes = (newInjector.^innerNotes == null) ? (new float[0]) : (new (newInjector.^innerNotes)[newInjector.^innerNotes.length]);
        drawStart = new (newInjector.^drawStart)();
        drawEnd = new (newInjector.^drawEnd)();
        pointCount = newInjector.^pointCount;
    }
    
    IAutomatonCommand[] DoRespond(string respondMode, float respondChartTime)
    {
        RespondResult respondResult;
        bool playEffect;
        
        int squareNumber;
        switch (respondMode)
        {
            case "BestPerfect":
                respondResult = RespondResult.BestPerfect;
                playEffect = true;
                squareNumber = 7;
                break;
            case "Perfect":
                respondResult = RespondResult.Perfect;
                playEffect = true;
                squareNumber = 4;
                break;
            case "Good":
                respondResult = RespondResult.Good;
                playEffect = true;
                squareNumber = 4;
                break;
            case "Miss":
                respondResult = RespondResult.Miss;
                playEffect = false;
                squareNumber = 0;
                break;
        }
        
        Environment.Scoring(respondResult);
        
        if (!playEffect)
        {
            return new IAutomatonCommand[0];
        }
        
        // Environment.PlayRespondEffect("RespondA");
        
        
        IAutomatonCommand[] commands = new IAutomatonCommand[squareNumber + 1];
        ColorArgb color = channel.nowColor;
        Vector3 globalRespondPosition = circleSprite.GlobalPosition();

        Word word = new Word(respondMode, new Vector2(globalRespondPosition.x, globalRespondPosition.y), respondChartTime, null, new ColorArgb(color.a,color.r,color.g,color.b));
        commands[squareNumber] = new DeriveElementCommand(word, false);
        
        for(int i = 0; i < squareNumber; i = i + 1)
        {
            Square square = new Square(new Vector2(globalRespondPosition.x, globalRespondPosition.y), respondChartTime, null, new ColorArgb(color.a,color.r,color.g,color.b), new ColorArgb(1,1,1,1));
            commands[i] = new DeriveElementCommand(square, false);
        }
        
        return commands;
    }
}