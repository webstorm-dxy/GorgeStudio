using Gorge;
using GorgeFramework;
namespace Reincal;

class ReincalSliderRespondHintPositionTransformer :: ITransformer
{
    ReincalSliderRespondHint note;
    
    ReincalSliderRespondHintPositionTransformer(ReincalSliderRespondHint note)
    {
        this.note = note;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float t = now - note.startTime;
        float distance = t * note.moveSpeed;
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