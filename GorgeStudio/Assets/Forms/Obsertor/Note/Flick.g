using Gorge;
using GorgeFramework;
namespace Obsertor;

[
    delegate<float:Flick^> display = string:(Flick^ noteInjector) ->
    {
        return noteInjector.^hitTime + "";
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 0.984, g : 0.29, b : 0.208},
    delegate<ElementLine:Flick^> elementLine = ElementLine:(Flick^ noteInjector) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[1];
        points[0] = new ElementLinePoint(noteInjector.^hitTime, 0.5833333333333333, 0.16666666);
        return new ElementLine(new ColorArgb(1,0.984,0.29,0.208), points);
    },
    string displayName = "Flick"
]
@EditableElement(type = "Note", editUpdateMode = EditUpdateMode.RePlay)
class Flick : LineNote
{
    [
        auto defaultValue = true,
        string type = "基本",
        int order = 9,
        string displayName = "向上滑动",
        string information = "是否向上滑动，否则向下",
        delegate<bool:bool> check = bool:(bool isUp) -> { return true; }
    ]
    @Inject
    bool isUp = ^isUp;

    [
        auto defaultValue = false,
        string type = "基本",
        int order = 10,
        string displayName = "绿色",
        string information = "是否向上滑动，否则向下",
        delegate<bool:bool> check = bool:(bool isGreen) -> { return true; }
    ]
    @Inject
    bool isGreen = ^isGreen;

    // 滑动基点，标准坐标
    Vector2 flickBase;

    [
        delegate<float:Flick^> time = float:(Flick^ noteInjector) ->
        {
            return noteInjector.^hitTime - noteInjector.^leadTime;
        }
    ]
    @ForwardTimedGenerate
    [
        delegate<float:Flick^> time = float:(Flick^ noteInjector) ->
        {
            return noteInjector.^hitTime + noteInjector.^lagTime;
        }
    ]
    @BackwardTimedGenerate
    @EditGenerate
    Flick(bool isAutoPlay, bool isReverse) : super()
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
        
        if(isUp)
        {
            if(isGreen)
            {
                ImageAsset lineImage = (ImageAsset) Environment.GetAssetByName("image:Obsertor/FormAsset/FlickUpGreen200");
                graphNode = new MeshedSprite(lineImage.texture);
                graphNode.position.z = 0;
                graphNode.position.x = position;
                graphNode.width = 3;
                graphNode.height = 1.56;
                graphNode.size.x = 1;
                graphNode.size.y = 1;
                graphNode.positionReference = sliderLaneBase;
                graphNode.horizontalSegments = 10;
                graphNode.verticalSegments = 1;
                graphNode.centerY = 0.29;
            }
            else
            {
                ImageAsset lineImage = (ImageAsset) Environment.GetAssetByName("image:Obsertor/FormAsset/FlickUpRed200");
                graphNode = new MeshedSprite(lineImage.texture);
                graphNode.position.z = 0;
                graphNode.position.x = position;
                graphNode.width = 3;
                graphNode.height = 1.56;
                graphNode.size.x = 1;
                graphNode.size.y = 1;
                graphNode.positionReference = sliderLaneBase;
                graphNode.horizontalSegments = 10;
                graphNode.verticalSegments = 1;
                graphNode.centerY = 0.29;
            }
        }
        else
        {
            if(isGreen)
            {
                ImageAsset lineImage = (ImageAsset) Environment.GetAssetByName("image:Obsertor/FormAsset/FlickDownGreen200");
                graphNode = new MeshedSprite(lineImage.texture);
                graphNode.position.z = 0;
                graphNode.position.x = position;
                graphNode.width = 3;
                graphNode.height = 1.56;
                graphNode.size.x = 1;
                graphNode.size.y = 1;
                graphNode.positionReference = sliderLaneBase;
                graphNode.horizontalSegments = 10;
                graphNode.verticalSegments = 1;
                graphNode.centerY = -0.29;
            }
            else
            {
                ImageAsset lineImage = (ImageAsset) Environment.GetAssetByName("image:Obsertor/FormAsset/FlickDownRed200");
                graphNode = new MeshedSprite(lineImage.texture);
                graphNode.position.z = 0;
                graphNode.position.x = position;
                graphNode.width = 3;
                graphNode.height = 1.56;
                graphNode.size.x = 1;
                graphNode.size.y = 1;
                graphNode.positionReference = sliderLaneBase;
                graphNode.horizontalSegments = 10;
                graphNode.verticalSegments = 1;
                graphNode.centerY = -0.29;
            }
        }
        
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
            FlickTsiga initializer = new FlickTsiga(this, isReverse);
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
    static bool TryGenerate(Flick^ newInjector, float chartTime)
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

        ColorArgb color;

        if(!isGreen)
        {
            color = new ColorArgb(1,0.984,0.29,0.208);
        }
        else
        {
            color = new ColorArgb(1,0.255,0.71,0.549);
        }

        respondHint = new RespondHint(new Vector2(position,lane.curve.Evaluate(position)), respondChartTime, null, color, new ColorArgb(1,1,1,1));
        
        IAutomatonCommand[] commands = new IAutomatonCommand[1];
        commands[0] = new DeriveElementCommand(respondHint, false);
        return commands;
    }

    // 计算滑动距离
    float GetFlickDistance(TouchSignal signal)
    {
        if (flickBase == null)
        {
            return 0;
        }

        if((isUp && signal.position.y > flickBase.y) || (!isUp && signal.position.y < flickBase.y))
        {
            return Vector2.Distance(signal.position, flickBase);
        }
        
        return 0;
    }
}