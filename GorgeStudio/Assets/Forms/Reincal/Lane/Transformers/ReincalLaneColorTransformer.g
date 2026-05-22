using Gorge;
using GorgeFramework;
namespace Reincal;

class ReincalLaneColorTransformer :: ITransformer
{
    ReincalSliderLane lane;
    
    ReincalLaneColorTransformer(ReincalSliderLane lane)
    {
        this.lane = lane;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        if (lane.color == null)
        {
            lane.graphNode.color = new ColorArgb(1, 1, 1, 1);
            return null;
        }
        lane.graphNode.color = lane.color.Evaluate(now);
        return null;
    }
}