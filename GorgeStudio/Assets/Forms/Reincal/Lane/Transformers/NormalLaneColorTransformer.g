using Gorge;
using GorgeFramework;
namespace Reincal;

class NormalLaneColorTransformer :: ITransformer
{
    NormalLane lane;

    NormalLaneColorTransformer(NormalLane lane)
    {
        this.lane = lane;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        if (lane.color == null)
        {
            lane.judgementNode.color = new ColorArgb(1, 1, 1, 1);
            return null;
        }
        lane.judgementNode.color = lane.color.Evaluate(now);
        return null;
    }
}