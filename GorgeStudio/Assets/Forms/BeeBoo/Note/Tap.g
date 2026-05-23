using Gorge;
using GorgeFramework;
namespace BeeBoo;

[
    delegate<float:Tap^> display = string:(Tap^ noteInjector) ->
    {
        return noteInjector.^hitTime + "";
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 0.843, g : 0.953, b : 1},
    delegate<ElementLine:Tap^> elementLine = ElementLine:(Tap^ noteInjector) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[1];
        points[0] = new ElementLinePoint(noteInjector.^hitTime, 0.91666666, 0.16666666);
        return new ElementLine(new ColorArgb(1,0.843,0.953,1), points);
    },
    string displayName = "Tap"
]
@EditableElement(type = "Note", editUpdateMode = EditUpdateMode.ReInject)
class Tap : BeeBooNote
{
    [
        delegate<float:Tap^> time = float:(Tap^ noteInjector) ->
        {
            return noteInjector.^hitTime - noteInjector.^leadTime;
        }
    ]
    @ForwardTimedGenerate
    [
        delegate<float:Tap^> time = float:(Tap^ noteInjector) ->
        {
            return noteInjector.^hitTime + noteInjector.^lagTime;
        }
    ]
    @BackwardTimedGenerate
    @EditGenerate
    Tap(bool isAutoPlay, bool isReverse) : super()
    {
        if (tracker == null)
        {
            return null;
        }
        
        if (isAutoPlay)
        {
            channel.EnqueueAutoTarget(hitTime, 0.05);
        }

        nodes = new Node[3];
        nodes[0] = ringSprite;
        nodes[1] = circleSprite;
        nodes[2] = innerCircleSprite;

        TapTsiga initializer = new TapTsiga(this, isReverse, isAutoPlay);
        automaton = new SignalTsiga(this, initializer.timeStack, initializer.inputGraph, initializer.historyStack);
        
        ITransformer[] transformers = new ITransformer[2];
        transformers[0] = new NoteReinjectTransformer(this);
        transformers[1] = new NoteTimeVaryingVariableTransformer(this);
        simulator = new ElementSimulator(transformers);

        ITransformer[] lateTransformers = new ITransformer[2];
        lateTransformers[0] = new NoteHeaderPositionTransformer(this);
        lateTransformers[1] = new NoteHeaderAnimationTransformer(this);
        lateIndependentSimulator = new ElementSimulator(lateTransformers);
    }
    
    @ForwardTimedDestroy
    float ForwardDestroyTime()
    {
        return hitTime + lagTime;
    }
    
    @BackwardTimedDestroy
    float BackwardDestroyTime()
    {
        return hitTime - leadTime;
    }
    
    @EditTryGenerate
    static bool TryGenerate(Tap^ newInjector, float chartTime)
    {
        return chartTime >= (newInjector.^hitTime - newInjector.^leadTime) && chartTime <= (newInjector.^hitTime + newInjector.^lagTime);
    }

    @EditReInject
    void ReInjectTap(Tap^ newInjector)
    {
        ReInjectBeeBooNote(newInjector);
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