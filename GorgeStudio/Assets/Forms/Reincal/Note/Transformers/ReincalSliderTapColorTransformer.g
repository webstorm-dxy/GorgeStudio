using Gorge;
using GorgeFramework;
namespace Reincal;

class ReincalSliderTapColorTransformer :: ITransformer
{
    ReincalSliderTap note;
    
    ReincalSliderTapColorTransformer(ReincalSliderTap note)
    {
        this.note = note;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float t = now - note.hitTime;
        float distance = note.distance.Evaluate(t);
        if (distance < 0)
        {
            note.graphNode.color = new ColorArgb(0, 1, 1, 1);
            return null;
        }
        
        // 与圆心的距离
        float centerDistance = note.judgementLineUnmappedRadius - distance;
        if (centerDistance < 0)
        {
            note.graphNode.color = new ColorArgb(0, 1, 1, 1);
            return null;
        }
        
        if (note.color == null)
        {
            note.graphNode.color = new ColorArgb(1, 1, 1, 1);
            return null;
        }
        note.graphNode.color = note.color.Evaluate(t);
        return null;
    }
}