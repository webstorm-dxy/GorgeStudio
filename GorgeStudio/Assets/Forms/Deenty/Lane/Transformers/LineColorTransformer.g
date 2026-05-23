using Gorge;
using GorgeFramework;
namespace Deenty;

class LineColorTransformer :: ITransformer
{
    Line lane;
    
    LineColorTransformer(Line lane)
    {
        this.lane = lane;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        lane.lineGraphNode.color.a = lane.alpha.EvaluateDoubleLerp(now, 0.0, 1.0);
        return null;
    }
}