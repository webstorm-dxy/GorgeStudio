using Gorge;
using GorgeFramework;
namespace Deenty;

class LanePositionTransformer :: ITransformer
{
    DeentyLane lane;
    
    LanePositionTransformer(DeentyLane lane)
    {
        this.lane = lane;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        lane.positionNode.position = new Vector3(
            lane.positionX.EvaluateAdd(now),
            lane.positionY.EvaluateAdd(now),
            lane.positionZ.EvaluateAdd(now)
        );
        
        lane.positionNode.rotation = new Vector3(
            0.0,
            0.0,
            lane.rotationZ.EvaluateAdd(now)
        );
        
        lane.positionNode.size = new Vector3(
            lane.scaleX.EvaluateAdd(now),
            lane.scaleY.EvaluateAdd(now),
            1.0
        );
        
        return null;
    }
}