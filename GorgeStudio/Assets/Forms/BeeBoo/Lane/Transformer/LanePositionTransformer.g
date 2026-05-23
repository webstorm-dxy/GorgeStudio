using Gorge;
using GorgeFramework;
namespace BeeBoo;

class LanePositionTransformer :: ITransformer
{
    Lane lane;

    LanePositionTransformer(Lane lane)
    {
        this.lane = lane;
    }

    IAutomatonCommand[] Transform(float now)
    {
        lane.laneNode.position.x = lane.nowPositionX;
        lane.laneNode.position.y = lane.nowPositionY;
        lane.laneNode.rotation.z = lane.rotation;
        lane.laneNode.color.a = lane.nowAlpha;

        return null;
    }

}