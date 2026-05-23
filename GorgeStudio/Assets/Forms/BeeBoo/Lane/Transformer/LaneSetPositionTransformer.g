using Gorge;
using GorgeFramework;
namespace BeeBoo;

class LaneSetPositionTransformer :: ITransformer
{
    LaneSet laneSet;

    LaneSetPositionTransformer(LaneSet laneSet)
    {
        this.laneSet = laneSet;
    }

    IAutomatonCommand[] Transform(float now)
    {
        laneSet.baseNode.position.x = laneSet.nowPositionX;
        laneSet.baseNode.position.y = laneSet.nowPositionY;
        laneSet.baseNode.rotation.z = laneSet.nowRotation;

        return null;
    }

}