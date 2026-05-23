using Gorge;
using GorgeFramework;
namespace Reincal;

class ReincalLaneRotationTransformer :: ITransformer
{
    ReincalSliderLane lane;
    
    ReincalLaneRotationTransformer(ReincalSliderLane lane)
    {
        this.lane = lane;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        lane.graphNode.rotation.z = lane.rotation * 180 / 3.1415926;
        return null;
    }
}