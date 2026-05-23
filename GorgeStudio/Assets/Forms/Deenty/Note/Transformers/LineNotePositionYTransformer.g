using Gorge;
using GorgeFramework;
namespace Deenty;

/*
    线键平行位置变换器
*/
class LineNotePositionYTransformer :: ITransformer
{
    DeentyLineNote note;
    
    LineNotePositionYTransformer(DeentyLineNote note)
    {
        this.note = note;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float respondProgress = 1 - (note.respondMoment - now) / note.respondTime;
        note.graphNode.position.y = note.positionY.EvaluateAdd(respondProgress);
        return null;
    }
}