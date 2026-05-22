using Gorge;
using GorgeFramework;
namespace Deenty;

class DeentyLane : Element
{
    [
        string type = "基本",
        int order = 0,
        string displayName = "轨道名",
        string information = "不能为空",
        delegate<bool:string> check = bool:(string name) -> { return name != ""; }
    ]
    @Inject
    string name = ^name;
    
    [
        auto defaultValue = VariableFloat : {baseValue : 0.0},
        string type = "基本",
        int order = 1,
        string displayName = "X坐标",
        string information = "标准坐标系|横轴为谱面时间，单位秒；纵轴为坐标加值，实时坐标为基值加上纵轴值",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ positionX) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat positionX = new ^positionX();
    
    [
        auto defaultValue = VariableFloat : {baseValue : 0.0},
        string type = "基本",
        int order = 2,
        string displayName = "Y坐标",
        string information = "标准坐标系|横轴为谱面时间，单位秒；纵轴为坐标加值，实时坐标为基值加上纵轴值",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ positionY) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat positionY = new ^positionY();
    
    [
        auto defaultValue = VariableFloat : {baseValue : 0.0},
        string type = "基本",
        int order = 3,
        string displayName = "角度",
        string information = "标准坐标系|横轴为谱面时间，单位秒；纵轴为角度加值，实时角度为基值加上纵轴值",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ rotationZ) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat rotationZ = new ^rotationZ();
    
    [
        auto defaultValue = VariableFloat : {baseValue : 1.0},
        string type = "基本",
        int order = 4,
        string displayName = "横向比例",
        string information = "标准坐标系|横轴为谱面时间，单位秒；纵轴为比例加值，实时比例为基值加上纵轴值",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ scaleX) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat scaleX = new ^scaleX();
    
    [
        auto defaultValue = VariableFloat : {baseValue : 1.0},
        string type = "基本",
        int order = 5,
        string displayName = "纵向比例",
        string information = "标准坐标系|横轴为谱面时间，单位秒；纵轴为比例加值，实时比例为基值加上纵轴值",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ scaleY) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat scaleY = new ^scaleY();
    
    [
        auto defaultValue = VariableFloat : {baseValue : -2.0},
        string type = "基本",
        int order = 2000,
        string displayName = "叠放顺序",
        string information = "数字，值小者覆盖值大者，超出+-10则不显示|横轴为谱面时间，单位秒；纵轴为叠放顺序加值，实时叠放顺序为基值加上纵轴值",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ positionZ) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat positionZ = new ^positionZ();
    
    [
        auto defaultValue = 0.03,
        string type = "判定",
        int order = 1001,
        string displayName = "大P区间",
        string information = "区间半长，单位秒，>0",
        delegate<bool:float> check = bool:(float bestPerfectTimeHalfInterval) -> { return bestPerfectTimeHalfInterval > 0; }
    ]
    @Inject
    float bestPerfectTimeHalfInterval = ^bestPerfectTimeHalfInterval;
    
    [
        auto defaultValue = 0.095,
        string type = "判定",
        int order = 1002,
        string displayName = "小P区间",
        string information = "区间半长，单位秒，>0",
        delegate<bool:float> check = bool:(float perfectTimeHalfInterval) -> { return perfectTimeHalfInterval > 0; }
    ]
    @Inject
    float perfectTimeHalfInterval = ^perfectTimeHalfInterval;
    
    [
        auto defaultValue = 0.3,
        string type = "判定",
        int order = 1003,
        string displayName = "Good区间",
        string information = "区间半长，单位秒，>0",
        delegate<bool:float> check = bool:(float goodTimeHalfInterval) -> { return goodTimeHalfInterval > 0; }
    ]
    @Inject
    float goodTimeHalfInterval = ^goodTimeHalfInterval;
    
    [
        auto defaultValue = 0.5,
        string type = "判定",
        int order = 1004,
        string displayName = "Miss区间",
        string information = "区间半长，单位秒，>0",
        delegate<bool:float> check = bool:(float missTimeHalfInterval) -> { return missTimeHalfInterval > 0; }
    ]
    @Inject
    float missTimeHalfInterval = ^missTimeHalfInterval;
    
    Node positionNode;
    Node judgementNode;
    
    DeentyLane()
    {
    }
    
    string DescriptorDisplayString()
    {
        return name + " " + positionX.baseValue + "," + positionY.baseValue + " " + rotationZ.baseValue + " " + scaleX.baseValue + "," + scaleY.baseValue;
    }
    
    float MissRespondStartMoment(float noteRespondMoment)
    {
        return noteRespondMoment - missTimeHalfInterval;
    }
    
    float GoodRespondStartMoment(float noteRespondMoment)
    {
        return noteRespondMoment - goodTimeHalfInterval;
    }
    
    float PerfectRespondStartMoment(float noteRespondMoment)
    {
        return noteRespondMoment - perfectTimeHalfInterval;
    }
    
    float BestPerfectRespondStartMoment(float noteRespondMoment)
    {
        return noteRespondMoment - bestPerfectTimeHalfInterval;
    }
    
    float BestPerfectRespondEndMoment(float noteRespondMoment)
    {
        return noteRespondMoment + bestPerfectTimeHalfInterval;
    }
    
    float PerfectRespondEndMoment(float noteRespondMoment)
    {
        return noteRespondMoment + perfectTimeHalfInterval;
    }
    
    float GoodRespondEndMoment(float noteRespondMoment)
    {
        return noteRespondMoment + goodTimeHalfInterval;
    }
    
    float MissRespondEndMoment(float noteRespondMoment)
    {
        return noteRespondMoment + missTimeHalfInterval;
    }
}