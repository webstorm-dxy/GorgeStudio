using Gorge;
using GorgeFramework;
namespace Obsertor;

[
    delegate<float:SkyCatch^> display = string:(SkyCatch^ noteInjector) ->
    {
        return noteInjector.^hitTime + "";
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 0.7, g : 0.7, b : 0.684},
    delegate<ElementLine:SkyCatch^> elementLine = ElementLine:(SkyCatch^ noteInjector) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[1];
        points[0] = new ElementLinePoint(noteInjector.^hitTime, 0.0833333333333333, 0.16666666);
        return new ElementLine(new ColorArgb(1,0.7,0.7,0.684), points);
    },
    string displayName = "SkyCatch"
]
@EditableElement(type = "Note", editUpdateMode = EditUpdateMode.RePlay)
class SkyCatch : SkyNote
{
    [
        delegate<float:SkyCatch^> time = float:(SkyCatch^ noteInjector) ->
        {
            return noteInjector.^hitTime - noteInjector.^leadTime;
        }
    ]
    @ForwardTimedGenerate
    [
        delegate<float:SkyCatch^> time = float:(SkyCatch^ noteInjector) ->
        {
            return noteInjector.^hitTime + noteInjector.^lagTime;
        }
    ]
    @BackwardTimedGenerate
    @EditGenerate
    SkyCatch(bool isAutoPlay, bool isReverse) : super()
    {
        ImageAsset lineImage = (ImageAsset) Environment.GetAssetByName("image:Obsertor/FormAsset/SkyCatch200");
        indicatorNode = new Sprite(lineImage.texture);
        indicatorNode.size.x = 1.8;
        indicatorNode.size.y = 1.8;
        indicatorNode.position.x = position.x;
        indicatorNode.position.y = position.y;
        graphNode = new Sprite(lineImage.texture);
        graphNode.size.x = 1.8;
        graphNode.size.y = 1.8;
        graphNode.position.x = position.x;
        graphNode.position.y = position.y;

        nodes = new Node[2];
        nodes[0] = indicatorNode;
        nodes[1] = graphNode;

        if (isAutoPlay)
        {
            AutoplayTsiga initializer = new AutoplayTsiga(this, isReverse);
            automaton = new SignalTsiga(this, initializer.timeStack, initializer.inputGraph, initializer.historyStack);
        }
        else
        {
            CatchTsiga initializer = new CatchTsiga(this, isReverse);
            automaton = new SignalTsiga(this, initializer.timeStack, initializer.inputGraph, initializer.historyStack);
        }

        ITransformer[] lateTransformers = new ITransformer[2];
        lateTransformers[0] = new SkyNotePositionTransformer(this);
        lateTransformers[1] = new SkyNoteColorTransformer(this);

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
    static bool TryGenerate(SkyCatch^ newInjector, float chartTime)
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

        respondHint = new RespondHint(position, respondChartTime, null,new ColorArgb(1,1,1,0.584), new ColorArgb(1,1,1,1));
        
        IAutomatonCommand[] commands = new IAutomatonCommand[1];
        commands[0] = new DeriveElementCommand(respondHint, false);
        return commands;
    }
}