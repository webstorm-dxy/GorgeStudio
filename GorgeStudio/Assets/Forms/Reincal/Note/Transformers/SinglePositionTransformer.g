using Gorge;
using GorgeFramework;
namespace Reincal;

class SinglePositionTransformer :: ITransformer
{
    NormalNote note;

    SinglePositionTransformer(NormalNote note)
    {
        this.note = note;
    }

    IAutomatonCommand[] Transform(float now)
    {
        float t = now - note.hitTime;
        float distance = note.distance.Evaluate(t);
        if(distance<0)
        {
            distance = 0;
        }
        Sprite graphNode = note.graphNode;
        graphNode.position.y = distance;
        
        float radius = note.lane.radius;
        float size = distance > radius ? 0 : ((radius - distance) * note.size / radius);
        graphNode.size.x = size;
        graphNode.size.y = size; 

        return null;
    }
    
}