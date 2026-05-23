using Gorge;
using GorgeFramework;
namespace DeentyStoryboard;

[
    delegate<float:Image^> display = string:(Image^ imageInjector) ->
    {
        return imageInjector.^startMoment + " [" + imageInjector.^keepTime + "] | " + imageInjector.^assetName + ":" +
               imageInjector.^positionX.^baseValue + "," + imageInjector.^positionY.^baseValue;
    },
    ColorArgb^ color = ColorArgb : {a : 1, r : 1.0, g : 1.0, b : 1.0},
    delegate<ElementLine:Image^> elementLine = ElementLine:(Image^ imageInjector) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[2];
        points[0] = new ElementLinePoint(imageInjector.^startMoment, 0.5, 0.8);
        points[1] = new ElementLinePoint(imageInjector.^startMoment + imageInjector.^keepTime, 0.5, 0.8);
        return new ElementLine(new ColorArgb(0.2, 1, 1, 1), points);
    }
]
@EditableElement(type = "图片", editUpdateMode = EditUpdateMode.ReInject)
class Image : StoryboardEntity
{
    Sprite graphNode;
    
    [
        delegate<float:Image^> time = float:(Image^ imageInjector) ->
        {
            return imageInjector.^startMoment;
        }
    ]
    @ForwardTimedGenerate
    [
        delegate<float:Image^> time = float:(Image^ imageInjector) ->
        {
            return imageInjector.^startMoment + imageInjector.^keepTime;
        }
    ]
    @BackwardTimedGenerate
    @EditGenerate
    Image()
    {
        ITransformer[] transformers = new ITransformer[3];
        transformers[0] = new EntityColorTransformer(this);
        transformers[1] = new EntityPositionTransformer(this);
        transformers[2] = new EntityTextureTransformer(this);
        
        // GorgeGraphics
        ImageAsset lineImage = (ImageAsset) Environment.GetAssetByName(assetName);
        graphNode = new Sprite(lineImage.texture);
        
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
    void ReInject(Image^ newInjector)
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
    static bool TryGenerate(Image^ newInjector, float chartTime)
    {
        float beginTime = newInjector.^startMoment;
        float destroyTime = newInjector.^startMoment + newInjector.^keepTime;
        return chartTime >= beginTime && chartTime <= destroyTime;
    }
    
    @PeriodModifier
    static void PeriodModifier(Image^ imageInjector, PeriodConfig periodConfig)
    {
        imageInjector.^startMoment = imageInjector.^startMoment + periodConfig.timeOffset;
    }
}