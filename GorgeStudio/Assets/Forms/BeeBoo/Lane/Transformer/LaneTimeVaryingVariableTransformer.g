using Gorge;
using GorgeFramework;
namespace BeeBoo;

class LaneTimeVaryingVariableTransformer :: ITransformer
{

    Lane lane;

    LaneTimeVaryingVariableTransformer(Lane lane)
    {
        this.lane = lane;
    }

    IAutomatonCommand[] Transform(float now)
    {
        float t = now - lane.startTime;
        lane.localTime = t;
        lane.nowDrawStartPosition = lane.drawStartPosition.EvaluateAdd(t);
        lane.nowDrawEndPosition = lane.drawEndPosition.EvaluateAdd(t);
        lane.nowAlpha = lane.alpha.EvaluateAdd(t);
        lane.nowScale = lane.scale.EvaluateAdd(t);
        lane.nowPositionX = lane.positionX.EvaluateAdd(t);
        lane.nowPositionY = lane.positionY.EvaluateAdd(t);
        
        return null;
    }

}