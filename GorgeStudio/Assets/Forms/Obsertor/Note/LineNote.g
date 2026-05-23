using Gorge;
using GorgeFramework;
namespace Obsertor;

class LineNote : ObsertorNote
{

    [
        auto defaultValue = 0,
        string type = "基本",
        int order = 0,
        string displayName = "轨道编号",
        string information = "不小于0",
        delegate<bool:int> check = bool:(int laneId) -> { return true; }
    ]
    @Inject
    int laneId = ^laneId;

    [
        auto defaultValue = 0,
        string type = "基本",
        int order = 2,
        string displayName = "位置",
        string information = "角度|横轴为以打击时刻为0点的时间，单位秒；纵轴为相对于角度，取基础值加曲线纵轴",
        delegate<bool:float> check = bool:(float position) -> { return true; }
    ]
    @Inject<float>
    float position = ^position;

    LineLane lane;

    MeshedSprite graphNode;
    
    LineNote() : super()
    {
        lane = (LineLane) Environment.FindAliveLane("Obsertor.LineLane", laneId);
        if (lane == null)
        {
            return null;
        }
    }

    float GetAimDistance(TouchSignal signal)
    {
        return Vector2.Distance(signal.position, new Vector2(position, lane.curve.Evaluate(position)));
    }
}