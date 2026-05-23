using Gorge;
using GorgeFramework;
namespace Obsertor;

class LineNotePositionTransformer :: ITransformer
{
    LineNote note;
    
    LineNotePositionTransformer(LineNote note)
    {
        this.note = note;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float t = now - note.hitTime;
        float distance = note.distance.Evaluate(t);
        note.graphNode.position.z = distance;

        note.graphNode.ForceUpdate();
        return null;
    }
}