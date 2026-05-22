using Gorge;
using GorgeFramework;
namespace Deenty;

/*
    天键位置变换器
*/
class SkyNotePositionTransformer :: ITransformer
{
    DeentySkySingleNote note;
    
    SkyNotePositionTransformer(DeentySkySingleNote note)
    {
        this.note = note;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float respondProgress = 1 - (note.respondMoment - now) / note.respondTime;
        note.graphNode.position.x = note.positionX.EvaluateAdd(respondProgress);
        note.graphNode.position.y = note.positionY.EvaluateAdd(respondProgress);
        return null;
    }
}