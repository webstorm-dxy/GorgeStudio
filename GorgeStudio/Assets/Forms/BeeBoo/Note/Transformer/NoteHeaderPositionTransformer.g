using Gorge;
using GorgeFramework;
namespace BeeBoo;

class NoteHeaderPositionTransformer :: ITransformer
{
    BeeBooNote note;
    
    NoteHeaderPositionTransformer(BeeBooNote note)
    {
        this.note = note;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        Vector2 position = note.tracker.GetPosition(note.trackerHitTime);
        if (position != null)
        {
            note.ringSprite.position.x = position.x;
            note.ringSprite.position.y = position.y;
            note.circleSprite.position.x = position.x;
            note.circleSprite.position.y = position.y;
            note.innerCircleSprite.position.x = position.x;
            note.innerCircleSprite.position.y = position.y;
        }
        return null;
    }
}