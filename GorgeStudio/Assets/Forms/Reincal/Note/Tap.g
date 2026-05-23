using Gorge;
using GorgeFramework;
namespace Reincal;

[
    delegate<float:Tap^> display = string:(Tap^ noteInjector) ->
    {
        return noteInjector.^hitTime + "";
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 1, g : 0.1294117647058824, b : 0.1294117647058824},
    delegate<ElementLine:Tap^> elementLine = ElementLine:(Tap^ noteInjector) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[1];
        points[0] = new ElementLinePoint(noteInjector.^hitTime, 0.9, 0.2);
        return new ElementLine(new ColorArgb(1, 1, 0.1294117647058824, 0.1294117647058824), points);
    },
    string displayName = "下落Tap"
]
@EditableElement(type = "Note", editUpdateMode = EditUpdateMode.RePlay)
class Tap : NormalNote
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
        if (lane == null)
        {
            return null;
        }

        if (isAutoPlay)
        {
            lane.EnqueueAutoTarget(hitTime, 0.05);
        }
        
        Node sliderLaneBase = lane.graphNode;
        
        ImageAsset lineImage = (ImageAsset) Environment.GetAssetByName("image:Reincal/FormAsset/Tap");
        graphNode = new Sprite(lineImage.texture);
        graphNode.position.z = -2;
        graphNode.size.x = size;
        graphNode.size.y = size;
        graphNode.positionReference = sliderLaneBase;
        graphNode.rotationReference = sliderLaneBase;
        graphNode.color = new ColorArgb(1, 1, 1, 1);
        
        nodes = new Node[1];
        nodes[0] = graphNode;
        
        TapTsiga initializer = new TapTsiga(this, isReverse);
        automaton = new SignalTsiga(this, initializer.timeStack, initializer.inputGraph, initializer.historyStack);

        ITransformer[] lateTransformers = new ITransformer[2];
        lateTransformers[0] = new SinglePositionTransformer(this);
        lateTransformers[1] = new NormalSingleColorTransformer(this);
        
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