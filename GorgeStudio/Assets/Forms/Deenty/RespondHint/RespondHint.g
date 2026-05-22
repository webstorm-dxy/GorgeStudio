using Gorge;
using GorgeFramework;
namespace Deenty;

class RespondHint : Element
{
    float startTime;
    
    float keepTime;
    
    VariableFloat respondHintSize;
    
    FunctionCurve respondHintProcessCurve;
    
    ColorArgb color;
    
    NineSliceSprite graphNode;
    
    RespondHint(VariableFloat respondHintSize, FunctionCurve respondHintProcessCurve, float keepTime, Vector2 respondPosition, float startTime, RespondResult respondResult)
    {
        this.startTime = startTime;
        this.keepTime = keepTime;
        this.respondHintSize = respondHintSize;
        this.respondHintProcessCurve = respondHintProcessCurve;
        
        switch (respondResult)
        {
            case RespondResult.Good:
                color = new ColorArgb(1, 0.6557, 1, 0.7134);
                break;
            case RespondResult.Perfect:
                color = new ColorArgb(1, 1, 1, 1);
                break;
            case RespondResult.BestPerfect:
                color = new ColorArgb(1, 1, 1, 0.7783);
                break;
            case RespondResult.Miss:
                color = new ColorArgb(1, 1, 1, 1);
                break;
        }
        
        ITransformer[] transformers = new ITransformer[1];
        transformers[0] = new RespondHintTransformer(this);
        
        // GorgeGraphics
        ImageAsset lineImage = (ImageAsset) Environment.GetAssetByName("image:Deenty/FormAsset/RespondHint");
        graphNode = new NineSliceSprite(lineImage.texture, new Vector2(169, 169), new Vector2(169, 169), new Vector2(0.085, 0.085));
        graphNode.position.x = respondPosition.x;
        graphNode.position.y = respondPosition.y;
        graphNode.position.z = -1;
        graphNode.rotation.z = 45;
        graphNode.size.x = 1;
        graphNode.size.y = 1;
        graphNode.color = color;
        
        nodes = new Node[1];
        nodes[0] = graphNode;
        
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