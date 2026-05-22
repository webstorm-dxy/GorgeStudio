using Gorge;
using GorgeFramework;
namespace Reincal;

class ReincalSliderTapPositionTransformer :: ITransformer
{
    ReincalSliderTap note;
    
    ReincalSliderTapPositionTransformer(ReincalSliderTap note)
    {
        this.note = note;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float t = now - note.hitTime;
        float distance = note.distance.Evaluate(t);
        // 与圆心的距离
        float centerDistance = note.judgementLineUnmappedRadius - distance;
        if (centerDistance < 0)
        {
            centerDistance = 0;
        }
        
        note.graphNode.centerY = -centerDistance;
        
        return null;
    }
}