using Gorge;
using GorgeFramework;
namespace BeeBoo;

class LaneSetTimeVaryingVariableTransformer :: ITransformer
{

    LaneSet laneSet;

    LaneSetTimeVaryingVariableTransformer(LaneSet laneSet)
    {
        this.laneSet = laneSet;
    }

    IAutomatonCommand[] Transform(float now)
    {
        float t = now - laneSet.startTime;
        laneSet.localTime = t;
        laneSet.nowRotation = laneSet.rotation.EvaluateAdd(t);
        laneSet.nowPositionX = laneSet.positionX.EvaluateAdd(t);
        laneSet.nowPositionY = laneSet.positionY.EvaluateAdd(t);
        
        return null;
    }

}