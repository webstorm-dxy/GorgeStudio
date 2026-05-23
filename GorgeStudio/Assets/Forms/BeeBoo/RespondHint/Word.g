using Gorge;
using GorgeFramework;
namespace BeeBoo;

class Word : Element
{
    float startTime;
    
    float keepTime;
    
    FunctionCurve respondHintProcessCurve;
    
    ColorArgb color;
    
    Sprite graphNode;
    
    Vector2 respondPosition;
    
    Word(string respondMode, Vector2 respondPosition, float startTime, Node reference, ColorArgb color1)
    {
        int squareCount = 7;
        this.startTime = startTime;
        this.keepTime = 0.8;
        this.respondPosition = respondPosition;
        this.respondHintProcessCurve = new CubicHermiteSpline()
        {
            startPoint : Vector2 : {x : 0.0, y : 0.0},
            startTangent : 0.0,
            startWeight : 0.0,
            endPoint : Vector2 : {x : 1.0, y : 1.0},
            endTangent : 0.1,
            endWeight : 0.8,
        };
        color = color1;

        ImageAsset lineImage;

        switch (respondMode)
        {
            case "BestPerfect":
            case "Perfect":
                lineImage = (ImageAsset) Environment.GetAssetByName("image:BeeBoo/FormAsset/Perfect");
                break;
            case "Good":
                lineImage = (ImageAsset) Environment.GetAssetByName("image:BeeBoo/FormAsset/Great");
                break;
            case "Miss":
                lineImage = (ImageAsset) Environment.GetAssetByName("image:BeeBoo/FormAsset/Miss");
                break;
        }
        
        // GorgeGraphics
        graphNode = new Sprite(lineImage.texture);
        graphNode.position.x = respondPosition.x;
        graphNode.position.y = respondPosition.y;
        graphNode.position.z = -1.5;
        graphNode.size.x = 1.1;
        graphNode.size.y = 0.25;
        graphNode.color = color1;
        graphNode.positionReference = reference;
        
        nodes = new Node[1];
        nodes[0] = graphNode;
        
        ITransformer[] transformers = new ITransformer[1];
        transformers[0] = new WordTransformer(this);
        simulator = new ElementSimulator(transformers);
    }
    
    @ForwardTimedDestroy
    float ForwardDestroyTime()
    {
        return startTime + keepTime;
    }
    
    @BackwardTimedDestroy
    float BackwardDestroyTime()
    {
        return startTime;
    }
}