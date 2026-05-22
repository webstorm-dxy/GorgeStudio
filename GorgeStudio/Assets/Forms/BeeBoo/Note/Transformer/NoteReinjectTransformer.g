using Gorge;
using GorgeFramework;
namespace BeeBoo;

class NoteReinjectTransformer :: ITransformer
{
    BeeBooNote note;

    NoteReinjectTransformer(BeeBooNote note)
    {
        this.note = note;
    }

    IAutomatonCommand[] Transform(float now)
    {
        note.tracker = (Tracker) Environment.FindAliveLane("BeeBoo.Tracker", note.trackerId);
        note.trackerHitTime = note.hitTime - note.tracker.startTime;
        note.channel = (Channel) Environment.FindAliveLane("BeeBoo.Channel", note.channelId);
        return null;
    }

}