using Gorge;
using GorgeFramework;
namespace DeentyStoryboard;

[
    delegate<float:StoryboardVideo^> display = string:(StoryboardVideo^ imageInjector) ->
    {
        return imageInjector.^startMoment + " [" + imageInjector.^keepTime + "] | " + imageInjector.^assetName + ":" +
               imageInjector.^positionX.^baseValue + "," + imageInjector.^positionY.^baseValue;
    },
    ColorArgb^ color = ColorArgb : {a : 1, r : 1.0, g : 1.0, b : 1.0},
    delegate<ElementLine:StoryboardVideo^> elementLine = ElementLine:(StoryboardVideo^ imageInjector) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[2];
        points[0] = new ElementLinePoint(imageInjector.^startMoment, 0.5, 0.8);
        points[1] = new ElementLinePoint(imageInjector.^startMoment + imageInjector.^keepTime, 0.5, 0.8);
        return new ElementLine(new ColorArgb(0.2, 1, 1, 1), points);
    }
]
@EditableElement(type = "视频", editUpdateMode = EditUpdateMode.ReInject)
class StoryboardVideo : StoryboardEntity
{
    [
        auto defaultValue = 1,
        string type = "效果",
        int order = 1002,
        string displayName = "播放倍速",
        string information = "播放倍速，>=0",
        delegate<bool:float> check = bool:(float speed) -> { return speed >= 0; }
    ]
    @Inject
    float speed = ^speed;
    
    VideoSprite graphNode;
    
    [
        delegate<float:StoryboardVideo^> time = float:(StoryboardVideo^ imageInjector) ->
        {
            return imageInjector.^startMoment;
        }
    ]
    @ForwardTimedGenerate
    [
        delegate<float:StoryboardVideo^> time = float:(StoryboardVideo^ imageInjector) ->
        {
            return imageInjector.^startMoment + imageInjector.^keepTime;
        }
    ]
    @BackwardTimedGenerate
    @EditGenerate
    StoryboardVideo()
    {
        ITransformer[] transformers = new ITransformer[3];
        transformers[0] = new VideoColorTransformer(this);
        transformers[1] = new VideoPositionTransformer(this);
        transformers[2] = new StoryboardVideoTransformer(this);
        
        // GorgeGraphics
        VideoAsset lineImage = (VideoAsset) Environment.GetAssetByName(assetName);
        graphNode = new VideoSprite(lineImage.GetAsset());
        
        nodes = new Node[1];
        nodes[0] = graphNode;
        
        mainNode = graphNode;
        
        simulator = new ElementSimulator(transformers);
    }
    
    @ForwardTimedDestroy
    float ForwardDestroyTime()
    {
        return startMoment + keepTime;
    }
    
    @BackwardTimedDestroy
    float BackwardDestroyTime()
    {
        return startMoment;
    }
    
    @EditReInject
    void ReInject(StoryboardVideo^ newInjector)
    {
        assetName = newInjector.^assetName;
        startMoment = newInjector.^startMoment;
        keepTime = newInjector.^keepTime;
        positionMode = newInjector.^positionMode;
        positionX = new (newInjector.^positionX)();
        positionY = new (newInjector.^positionY)();
        rotationZ = new (newInjector.^rotationZ)();
        scaleX = new (newInjector.^scaleX)();
        scaleY = new (newInjector.^scaleY)();
        positionZ = new (newInjector.^positionZ)();
        alpha = new (newInjector.^alpha)();
    }
    
    @EditTryGenerate
    static bool TryGenerate(StoryboardVideo^ newInjector, float chartTime)
    {
        float beginTime = newInjector.^startMoment;
        float destroyTime = newInjector.^startMoment + newInjector.^keepTime;
        return chartTime >= beginTime && chartTime <= destroyTime;
    }
    
    @PeriodModifier
    static void PeriodModifier(StoryboardVideo^ imageInjector, PeriodConfig periodConfig)
    {
        imageInjector.^startMoment = imageInjector.^startMoment + periodConfig.timeOffset;
    }
}