using Gorge;
using GorgeFramework;
namespace Deenty;

/*
    线键下落变换器
*/
class LineSingleNotePositionXTransformer :: ITransformer
{
    DeentyLineSingleNote note;
    
    LineSingleNotePositionXTransformer(DeentyLineSingleNote note)
    {
        this.note = note;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float curveTime = (now - (note.respondMoment - note.respondTime)) / note.respondTime;
        note.graphNode.position.x = (1 - note.positionXCurve.Evaluate(curveTime)) * note.laneLength;
        return null;
    }
}