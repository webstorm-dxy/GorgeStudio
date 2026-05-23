using Gorge;
using GorgeFramework;
namespace Obsertor;

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
@EditableElement(type = "Note", editUpdateMode = EditUpdateMode.RePlay)
class Tap : LineNote
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
        
        // if (isAutoPlay)
        // {
        //     lane.EnqueueAutoTarget(hitTime, 0.05);
        // }
        
        Node sliderLaneBase = lane.lineGraphNode;
        
        ImageAsset lineImage = (ImageAsset) Environment.GetAssetByName("image:Obsertor/FormAsset/Tap200");
        graphNode = new MeshedSprite(lineImage.texture);
        graphNode.position.z = 0;
        graphNode.position.x = position;
        graphNode.width = 3;
        graphNode.height = 0.6;
        graphNode.size.x = 1;
        graphNode.size.y = 1;
        graphNode.positionReference = sliderLaneBase;
        graphNode.horizontalSegments = 10;
        graphNode.verticalSegments = 1;
        
        CurveWarpTransformer meshTransformer = new CurveWarpTransformer();
        CompositeFunctionCurve meshCurve = new CompositeFunctionCurve();
        meshCurve.innerFunctionCurve = new LinearFunctionCurve(1, position);
        meshCurve.outerFunctionCurve = lane.curve;
        
        meshTransformer.curve = meshCurve;
        
        graphNode.AddMeshTransformer(meshTransformer);

        nodes = new Node[1];
        nodes[0] = graphNode;
        
        if (isAutoPlay)
        {
            AutoplayTsiga initializer = new AutoplayTsiga(this, isReverse);
            automaton = new SignalTsiga(this, initializer.timeStack, initializer.inputGraph, initializer.historyStack);
        }
        else
        {
            TapTsiga initializer = new TapTsiga(this, isReverse);
            automaton = new SignalTsiga(this, initializer.timeStack, initializer.inputGraph, initializer.historyStack);
        }
        
        ITransformer[] lateTransformers = new ITransformer[2];
        lateTransformers[0] = new LineNotePositionTransformer(this);
        lateTransformers[1] = new LineNoteColorTransformer(this);
        
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
        
        RespondHint respondHint;

        respondHint = new RespondHint(new Vector2(position,lane.curve.Evaluate(position)), respondChartTime, null, new ColorArgb(1,0.843,0.953,1), new ColorArgb(1,1,1,1));
        
        IAutomatonCommand[] commands = new IAutomatonCommand[1];
        commands[0] = new DeriveElementCommand(respondHint, false);
        return commands;
    }
}