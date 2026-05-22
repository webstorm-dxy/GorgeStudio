using Gorge;
using GorgeFramework;
namespace Deenty;

/*
    天键缩圈变换器
*/
class SkySingleNoteLoopTransformer :: ITransformer
{
    DeentySkySingleNote note;
    
    SkySingleNoteLoopTransformer(DeentySkySingleNote note)
    {
        this.note = note;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float showMoment = note.respondMoment - note.respondTime;
        float curveTime = (now - showMoment) / note.respondTime;
        
        float startSize = 1 + note.loopSize;
        float endSize = 1;
        
        float newSize = (endSize - startSize) * note.loopSizeCurve.Evaluate(curveTime) + startSize;
        
        note.loopNode.size.x = newSize;
        note.loopNode.size.y = newSize;
        return null;
    }
}