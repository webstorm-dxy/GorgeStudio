using Gorge;
using GorgeFramework;
namespace Deenty;

/*
    Note叠放次序变换器
*/
class NotePositionZTransformer :: ITransformer
{
    DeentyNote note;
    
    NotePositionZTransformer(DeentyNote note)
    {
        this.note = note;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float respondProgress = 1 - (note.respondMoment - now) / note.respondTime;
        note.graphNode.position.z = note.positionZ.EvaluateAdd(respondProgress);
        
        return null;
    }
}