using Gorge;
using GorgeFramework;
namespace Deenty;

/*
    线键长度变换器
*/
class LineNoteLengthTransformer :: ITransformer
{
    DeentyLineNote note;
    
    LineNoteLengthTransformer(DeentyLineNote note)
    {
        this.note = note;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float respondProgress = 1 - (note.respondMoment - now) / note.respondTime;
        note.graphNode.size.y = note.length.EvaluateAdd(respondProgress);
        return null;
    }
}