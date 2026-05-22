using Gorge;
using GorgeFramework;
namespace Obsertor;

[
    delegate<float:LineLane^> display = string:(LineLane^ laneInjector) ->
    {
        return laneInjector.^id + "";
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 0.2396693, g : 0.6370158, b : 0.8},
    // delegate<ElementLine:LineLane^> elementLine = ElementLine:(LineLane^ lane) ->
    // {
    //     ElementLinePoint[] points = new ElementLinePoint[2];
    //     points[0] = new ElementLinePoint(lane.^generateTime, 0.5, 0.8);
    //     points[1] = new ElementLinePoint(lane.^generateTime + lane.^keepTime, 0.5, 0.8);
    //     return new ElementLine(new ColorArgb(0.3, 1, 1, 1), points);
    // },
    string displayName = "轨道"
]
@EditableElement(type = "轨道", editUpdateMode = EditUpdateMode.RePlay)
class LineLane : Element
{
    [
        string type = "基本",
        int order = 0,
        string displayName = "轨道编号",
        string information = "不能为空",
        delegate<bool:int> check = bool:(int id) -> { return true; }
    ]
    @Inject
    int id = ^id;
    
    [
        auto defaultValue = ConstantFunctionCurve : {value : 1.2},
        string type = "基本",
        int order = 5,
        string displayName = "判定线动画进度",
        string information = "进度坐标系|横轴为以打击时刻为0点的时间，单位秒；纵轴为距离轨道判定线的距离",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ angle) -> { return true; },
        float scaleMax = 5.5,
        float scaleMin = -0.5
    ]
    @Inject<FunctionCurve^>
    FunctionCurve tangent = new ^tangent();
    
    CubicHermiteSpline curve;

    CurveSprite lineGraphNode;
    
    float now;
    
    @InitializeGenerate
    LineLane()
    {
        lineGraphNode = new CurveSprite(null);
        lineGraphNode.size.x = 1;
        lineGraphNode.position.z = 0;

        curve = new CubicHermiteSpline();
        curve.startPoint = new Vector2(-8,0);
        curve.startTangent = 0;
        curve.startWeight = 0.3;
        curve.endPoint = new Vector2(8,0);
        curve.endTangent = 0;
        curve.endWeight = 0.3;

        nodes = new Node[1];
        nodes[0] = lineGraphNode;

        ITransformer[] lateTransformers = new ITransformer[1];
        lateTransformers[0] = new LineLaneTangentTransformer(this);

        // simulator = new ElementSimulator(transformers);
        lateIndependentSimulator = new ElementSimulator(lateTransformers);
    }
}