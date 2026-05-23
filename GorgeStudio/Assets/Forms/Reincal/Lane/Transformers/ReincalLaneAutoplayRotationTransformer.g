using Gorge;
using GorgeFramework;
namespace Reincal;

class ReincalLaneAutoplayRotationTransformer :: ITransformer
{
    ReincalSliderLane lane;
    
    ReincalSliderLaneAutoMoveTarget lastTarget;
    ReincalSliderLaneAutoMoveTarget target;
    
    ReincalLaneAutoplayRotationTransformer(ReincalSliderLane lane)
    {
        this.lane = lane;
        lastTarget = new ReincalSliderLaneAutoMoveTarget(0, lane.rotation);
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        ObjectList<ReincalSliderLaneAutoMoveTarget> autoMoveTargets = lane.autoMoveTargets;
        
        // 如果没有运动目标
        if (target == null)
        {
            // 更新运动起点为当前点
            lastTarget = new ReincalSliderLaneAutoMoveTarget(now, lane.rotation);
            // 尝试取出一个运动目标
            if (autoMoveTargets.length > 0)
            {
                target = autoMoveTargets.Get(0);
                autoMoveTargets.RemoveAt(0);
            }
            // 如果没有运动目标，则直接结束
            else
            {
                return null;
            }
        }
        
        // 到此处必然有运动目标
        // 持续弹出队列，更新到恰当的运动目标
        while (target != null ? (target.time <= now) : false)
        {
            lastTarget = target;
            if (autoMoveTargets.length == 0)
            {
                target = null;
            }
            else
            {
                target = autoMoveTargets.Get(0);
                autoMoveTargets.RemoveAt(0);
            }
        }
        
        // 到此处必然有正确的lastTarget
        // 如果此时没有目标，则不移动
        if (target == null)
        {
            return null;
        }
        
        // 根据目标修改当前位置
        float process = Math.InverseLerp(lastTarget.time, target.time, now);
        lane.rotation = Math.Lerp(lastTarget.position, target.position, process);
        
        return null;
    }
}