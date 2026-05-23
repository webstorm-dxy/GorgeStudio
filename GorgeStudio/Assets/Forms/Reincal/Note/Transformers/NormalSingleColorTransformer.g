using Gorge;
using GorgeFramework;
namespace Reincal;

class NormalSingleColorTransformer :: ITransformer
{
    NormalNote note;
    
    NormalSingleColorTransformer(NormalNote note)
    {
        this.note = note;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float t = now - note.hitTime;
        float distance = note.distance.Evaluate(t);
        
        // 与圆心的距离
        float radius = note.lane.radius;
        float centerDistance = radius - distance;
        if (centerDistance < 0)
        {
            note.graphNode.color = new ColorArgb(0, 1, 1, 1);
            return null;
        }

        if(centerDistance > radius)
        {
            float alphaReduce = -distance / radius * 20;
            note.graphNode.color.a = 1 - alphaReduce;
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