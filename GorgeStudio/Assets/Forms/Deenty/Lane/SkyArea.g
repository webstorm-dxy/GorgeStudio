using Gorge;
using GorgeFramework;
namespace Deenty;

[
    delegate<float:SkyArea^> display = string:(SkyArea^ skyAreaInjector) ->
    {
        return skyAreaInjector.^name + " " + skyAreaInjector.^positionX.^baseValue + "," + skyAreaInjector.^positionY.^baseValue + " " +
               skyAreaInjector.^rotationZ.^baseValue + " " + skyAreaInjector.^scaleX.^baseValue + "," + skyAreaInjector.^scaleY.^baseValue;
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 0.8, g : 0.6598039, b : 0.2392157}
]
@EditableElement(type = "轨道", editUpdateMode = EditUpdateMode.RePlay)
class SkyArea : DeentyLane
{
    [
        auto defaultValue = 1.5,
        string type = "判定",
        int order = 1101,
        string displayName = "判定区尺寸",
        string information = "判定区尺寸比Note尺寸多出的半长，标准坐标系",
        delegate<bool:float> check = bool:(float respondAreaHalfAdditionInterval) -> { return true; },
        string convertFrom = "RespondAreaXHalfInterval RespondAreaYHalfAdditionInterval"
    ]
    @Inject
    float respondAreaHalfAdditionInterval;
    
    @InitializeGenerate
    SkyArea()
    {
        positionNode = new Node();
        judgementNode = new Node();
        judgementNode.positionReference = positionNode;
        judgementNode.rotationReference = positionNode;
        
        nodes = new Node[2];
        nodes[0] = positionNode;
        nodes[1] = judgementNode;
        
        ITransformer[] transformers = new ITransformer[1];
        transformers[0] = new LanePositionTransformer(this);
        
        simulator = new ElementSimulator(transformers);
    }
}