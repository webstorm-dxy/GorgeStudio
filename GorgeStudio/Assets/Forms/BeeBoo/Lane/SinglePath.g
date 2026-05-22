using Gorge;
using GorgeFramework;
namespace BeeBoo;

[
    string displayName = "单路径",
    SinglePath^ defaultInjector = SinglePath : { laneSetId : 0, laneIndex : 0 }
]
@Editable 
class SinglePath : TrackerPath
{
    [
        string type = "基本",
        int order = 0,
        string displayName = "轨道组编号",
        string information = "不能为空",
        delegate<bool:int> check = bool:(int id) -> { return true; }
    ]
    @Inject
    int laneSetId = ^laneSetId;

    [
        string type = "基本",
        int order = 1,
        string displayName = "轨道索引",
        string information = "不能为空",
        delegate<bool:int> check = bool:(int index) -> { return true; }
    ]
    @Inject
    int laneIndex = ^laneIndex;

    [
        auto defaultValue = false,
        string type = "基本",
        int order = 2,
        string displayName = "反向运动",
        string information = "是否沿路径反向运动",
        delegate<bool:bool> check = bool:(bool isReverse) -> { return true; }
    ]
    @Inject
    bool isReverse = ^isReverse;

    [
        auto defaultValue = -1,
        string type = "生命周期",
        int order = 3,
        string displayName = "重写时间值",
        string information = "负数表示不覆盖",
        delegate<bool:float> check = bool:(float timeOverride) -> { return true; }
    ]
    @Inject
    float timeOverride = ^timeOverride;

    Lane lane;

    SinglePath() : super()
    {
        LaneSet laneSet = (LaneSet) Environment.FindAliveLane("BeeBoo.LaneSet", laneSetId);
        if(laneSet == null)
        {
            laneSet = (LaneSet) Environment.FindAliveLane("BeeBoo.FreeLaneSet", laneSetId);
            if(laneSet == null)
            {
                return null;
            }
        }

        lane = laneSet.GetLane(laneIndex);

        if (lane == null)
        {
            return null;
        }
    }

    float GetTime()
    {
        if(timeOverride >= 0)
        {
            return timeOverride;
        }

        return lane.time;
    }

    Vector2 GetPosition(float time)
    {
        float pathTime = GetTime();
        float progress;
        if(pathTime == 0)
        {
            progress = 0;
        }
        else
        {
            progress = time / pathTime;
            if(progress > 1)
            {
                progress = 1;
            }
            else if(progress < 0)
            {
                progress = 0;
            }
        }

        Vector2 position = lane.GetPosition(progress);

        return position;
    }

    void UpdateReference()
    {
        LaneSet laneSet = (LaneSet) Environment.FindAliveLane("BeeBoo.LaneSet", laneSetId);
        if(laneSet == null)
        {
            laneSet = (LaneSet) Environment.FindAliveLane("BeeBoo.FreeLaneSet", laneSetId);
        }

        lane = laneSet.GetLane(laneIndex);
    }
}