using Gorge;
using GorgeFramework;
namespace Deenty;

/*
    天键大小变换器
*/
class SkyNoteSizeTransformer :: ITransformer
{
    DeentySkySingleNote note;
    
    SkyNoteSizeTransformer(DeentySkySingleNote note)
    {
        this.note = note;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float respondProgress = 1 - (note.respondMoment - now) / note.respondTime;
        float newSize = note.size.EvaluateAdd(respondProgress);
        note.graphNode.size.x = newSize;
        note.graphNode.size.y = newSize;
        return null;
    }
}