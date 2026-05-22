using Gorge;
using GorgeFramework;
namespace Reincal;

class NormalRespondHint : Element
{
    float startTime;
    float keepTime;
    
    Sprite respondHintCircle1;
    Sprite respondHintCircle2;
    Sprite respondHintCircle3;
    Sprite respondHintCircle4;
    Sprite respondHintCircle5;
    
    // 使用二次函数映射前的判定线半径
    float judgementLineUnmappedRadius;
    
    NormalRespondHint(Node laneNode, float baseSize, float startTime)
    {
        this.startTime = startTime;
        this.keepTime = 0.8;
        
        // GorgeGraphics
        ImageAsset respondHintImage1 = (ImageAsset) Environment.GetAssetByName("image:Reincal/FormAsset/RespondHint1");
        ImageAsset respondHintImage2 = (ImageAsset) Environment.GetAssetByName("image:Reincal/FormAsset/RespondHint2");
        ImageAsset respondHintImage3 = (ImageAsset) Environment.GetAssetByName("image:Reincal/FormAsset/RespondHint3");
        ImageAsset respondHintImage4 = (ImageAsset) Environment.GetAssetByName("image:Reincal/FormAsset/RespondHint4");
        ImageAsset respondHintImage5 = (ImageAsset) Environment.GetAssetByName("image:Reincal/FormAsset/RespondHint5");
        
        respondHintCircle1 = new Sprite(respondHintImage1.texture);
        respondHintCircle1.positionReference = laneNode;
        respondHintCircle1.rotationReference = laneNode;

        respondHintCircle2 = new Sprite(respondHintImage2.texture);
        respondHintCircle2.positionReference = laneNode;
        respondHintCircle2.rotationReference = laneNode;

        respondHintCircle3 = new Sprite(respondHintImage3.texture);
        respondHintCircle3.positionReference = laneNode;
        respondHintCircle3.rotationReference = laneNode;

        respondHintCircle4 = new Sprite(respondHintImage4.texture);
        respondHintCircle4.positionReference = laneNode;
        respondHintCircle4.rotationReference = laneNode;

        respondHintCircle5 = new Sprite(respondHintImage5.texture);
        respondHintCircle5.positionReference = laneNode;
        respondHintCircle5.rotationReference = laneNode;
        
        nodes = new Node[5];
        nodes[0] = respondHintCircle1;
        nodes[1] = respondHintCircle2;
        nodes[2] = respondHintCircle3;
        nodes[3] = respondHintCircle4;
        nodes[4] = respondHintCircle5;
        
        ITransformer[] transformers = new ITransformer[1];
        transformers[0] = new NormalRespondHintTransformer(this, baseSize);
        lateIndependentSimulator = new ElementSimulator(transformers);
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