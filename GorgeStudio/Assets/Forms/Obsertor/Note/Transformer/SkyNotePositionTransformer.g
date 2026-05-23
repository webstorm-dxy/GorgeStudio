using Gorge;
using GorgeFramework;
namespace Obsertor;

class SkyNotePositionTransformer :: ITransformer
{
    SkyNote note;
    
    SkyNotePositionTransformer(SkyNote note)
    {
        this.note = note;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float t = now - note.hitTime;
        float distance = note.distance.Evaluate(t);
        note.graphNode.position.z = distance;
        return null;
    }
}