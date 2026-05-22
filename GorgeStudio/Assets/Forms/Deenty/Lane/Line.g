using Gorge;
using GorgeFramework;
namespace Deenty;

[
    delegate<float:Line^> display = string:(Line^ lineInjector) ->
    {
        return lineInjector.^name + " " + lineInjector.^positionX.^baseValue + "," + lineInjector.^positionY.^baseValue + " " +
               lineInjector.^rotationZ.^baseValue + " " + lineInjector.^scaleX.^baseValue + "," + lineInjector.^scaleY.^baseValue;
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 0.2396693, g : 0.6370158, b : 0.8}
]
@EditableElement(type = "轨道", editUpdateMode = EditUpdateMode.RePlay)
class Line : DeentyLane
{
    [
        auto defaultValue = "",
        string type = "基本",
        int order = 100,
        string displayName = "翻转目标",
        string information = "判定线名",
        delegate<bool:string> check = bool:(string flipHorizontalTarget) -> { return true; }
    ]
    @Inject
    string flipHorizontalTarget = ^flipHorizontalTarget;
    
    [
        auto defaultValue = LineLengthMode.ScreenY,
        string type = "效果",
        int order = 100,
        string displayName = "长度模式",
        string information = "长度模式",
        delegate<bool:LineLengthMode> check = bool:(LineLengthMode lengthMode) -> { return true; }
    ]
    @Inject
    LineLengthMode lengthMode = ^lengthMode;
    
    [
        auto defaultValue = VariableFloat : {baseValue : 1.0},
        string type = "效果",
        int order = 102,
        string displayName = "判定线长度",
        string information = "数字，意义取决于长度模式|横轴为谱面时间，单位秒；纵轴长度加值，实时长度为基值加上纵轴值",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ length) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat length = new ^length();
    
    [
        auto defaultValue = VariableFloat : {baseValue : 1.0},
        string type = "效果",
        int order = 2001,
        string displayName = "不透明度",
        string information = "数字，0-1之间|横轴为谱面时间，单位秒；纵轴为插值进度，0代表基值，1代表完全不透明，-1代表完全透明",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ alpha) -> { return alpha.^baseValue >= 0 && alpha.^baseValue <= 1; }
    ]
    @Inject<VariableFloat^>
    VariableFloat alpha = new ^alpha();
    
    [
        auto defaultValue = 1.5,
        string type = "判定",
        int order = 1101,
        string displayName = "判定区X尺寸",
        string information = "判定区尺寸在判定线两侧的宽度半长，标准坐标系",
        delegate<bool:float> check = bool:(float respondAreaXHalfInterval) -> { return true; },
        string convertFrom = "RespondAreaHalfAdditionInterval"
    ]
    @Inject
    float respondAreaXHalfInterval = ^respondAreaXHalfInterval;
    
    [
        auto defaultValue = 1.5,
        string type = "判定",
        int order = 1102,
        string displayName = "判定区Y尺寸",
        string information = "判定区尺寸比Note尺寸多出的半长，标准坐标系",
        delegate<bool:float> check = bool:(float respondAreaYHalfAdditionInterval) -> { return true; },
        string convertFrom = "RespondAreaHalfAdditionInterval"
    ]
    @Inject
    float respondAreaYHalfAdditionInterval = ^respondAreaYHalfAdditionInterval;
    
    NineSliceSprite lineGraphNode;
    
    @InitializeGenerate
    Line()
    {
        // GorgeGraphics
        ImageAsset lineImage = (ImageAsset) Environment.GetAssetByName("image:Deenty/FormAsset/Line");
        positionNode = new Node();
        lineGraphNode = new NineSliceSprite(lineImage.texture, new Vector2(169, 169), new Vector2(169, 169), new Vector2(0.1, 0.1));
        lineGraphNode.positionReference = positionNode;
        lineGraphNode.rotationReference = positionNode;
        lineGraphNode.size.x = 0.1;
        
        judgementNode = new Node();
        judgementNode.positionReference = positionNode;
        judgementNode.rotationReference = positionNode;
        
        nodes = new Node[3];
        nodes[0] = positionNode;
        nodes[1] = lineGraphNode;
        nodes[2] = judgementNode;
        
        ITransformer[] transformers = new ITransformer[3];
        transformers[0] = new LanePositionTransformer(this);
        transformers[1] = new LineLengthTransformer(this);
        transformers[2] = new LineColorTransformer(this);
        
        simulator = new ElementSimulator(transformers);
    }
}