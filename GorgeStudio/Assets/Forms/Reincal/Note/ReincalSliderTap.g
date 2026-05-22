using Gorge;
using GorgeFramework;
namespace Reincal;

[
    delegate<float:ReincalSliderTap^> display = string:(ReincalSliderTap^ noteInjector) ->
    {
        return noteInjector.^hitTime + "";
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 1, g : 0.6431372549019608, b : 0},
    delegate<ElementLine:ReincalSliderTap^> elementLine = ElementLine:(ReincalSliderTap^ noteInjector) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[1];
        points[0] = new ElementLinePoint(noteInjector.^hitTime, 0.3, 0.2);
        return new ElementLine(new ColorArgb(1, 1, 0.6431372549019608, 0), points);
    },
    string displayName = "滑动Tap"
]
@EditableElement(type = "Note", editUpdateMode = EditUpdateMode.RePlay)
class ReincalSliderTap : SliderNote
{
    // 使用二次函数映射前的判定线半径
    float judgementLineUnmappedRadius;
    
    ReincalSliderLane lane;
    
    MeshedSprite graphNode;
    
    [
        delegate<float:ReincalSliderTap^> time = float:(ReincalSliderTap^ noteInjector) ->
        {
            return noteInjector.^hitTime - noteInjector.^leadTime;
        }
    ]
    @ForwardTimedGenerate
    [
        delegate<float:ReincalSliderTap^> time = float:(ReincalSliderTap^ noteInjector) ->
        {
            return noteInjector.^hitTime + noteInjector.^lagTime;
        }
    ]
    @BackwardTimedGenerate
    @EditGenerate
    ReincalSliderTap(bool isAutoPlay, bool isReverse) : super()
    {
        lane = (ReincalSliderLane) Environment.FindAliveLane("Reincal.ReincalSliderLane", "SliderLane");
        
        if (lane == null)
        {
            return null;
        }
        
        if (isAutoPlay && !isFake)
        {
            lane.EnqueueAutoTarget(hitTime, position);
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
        graphNode = new MeshedSprite(sliderTapImage.texture);
        graphNode.width = finalArcLength;
        graphNode.height = finalHeight;
        graphNode.horizontalSegments = 10;
        graphNode.verticalSegments = 1;
        graphNode.size.y = 1;
        graphNode.size.x = 1;
        graphNode.position.z = -1.1;
        graphNode.positionReference = lane.graphNode;
        
        // 计算映射前判定线距离
        judgementLineUnmappedRadius = Math.Sqrt(2 * judgementLineRadius * judgementLineRadius);
        AnnulusMeshTransformer meshTransformer = new AnnulusMeshTransformer();
        meshTransformer.yRadius = new QuadraticFunctionCurve(0.5 / judgementLineRadius, 0, 0);
        meshTransformer.xAngle = new LinearCurve(-1, -halfCenterAngleR + position + 4.71233889, 1, halfCenterAngleR + position + 4.71233889);
        
        graphNode.AddMeshTransformer(meshTransformer);
        
        nodes = new Node[1];
        nodes[0] = graphNode;
        
        if(!isFake)
        {
            ReincalSliderTapTsiga initializer = new ReincalSliderTapTsiga(this, isReverse);
            automaton = new SignalTsiga(this, initializer.timeStack, initializer.inputGraph, initializer.historyStack);
        }
        
        ITransformer[] lateTransformers = new ITransformer[2];
        lateTransformers[0] = new ReincalSliderTapPositionTransformer(this);
        lateTransformers[1] = new ReincalSliderTapColorTransformer(this);
        
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
    static bool TryGenerate(ReincalSliderTap^ newInjector, float chartTime)
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
        
        ReincalSliderRespondHint respondHint;
        
        respondHint = new ReincalSliderRespondHint(position, judgementLineRadius, respondChartTime, lane.graphNode);
        
        IAutomatonCommand[] commands = new IAutomatonCommand[1];
        commands[0] = new DeriveElementCommand(respondHint, false);
        return commands;
    }
}