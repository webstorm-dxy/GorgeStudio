using Gorge;
using GorgeFramework;
namespace BeeBoo;

class NoteHeaderAnimationTransformer :: ITransformer
{
    BeeBooNote note;

    NoteHeaderAnimationTransformer(BeeBooNote note)
    {
        this.note = note;
    }

    IAutomatonCommand[] Transform(float now)
    {
        string automatonState = note.automaton.GetState();
        if (automatonState == "Accepted" || automatonState == "Miss" || automatonState == "Timeout" || automatonState == "Denied" || automatonState == "Holding")
        {
            note.ringSprite.color.a = 0;
            note.circleSprite.color.a = 0;
            note.innerCircleSprite.color.a = 0;
            return null;
        }

        float nowTotalAlpha = note.nowTotalAlpha;

        note.ringSprite.size.x = note.nowOuterCircleSize;
        note.ringSprite.size.y = note.nowOuterCircleSize;
        note.ringSprite.color.a = nowTotalAlpha;
        note.circleSprite.size.x = note.nowOuterCircleSize;
        note.circleSprite.size.y = note.nowOuterCircleSize;
        ColorArgb color = note.channel.nowColor;
        note.circleSprite.color = new ColorArgb(color.a * nowTotalAlpha, color.r, color.g, color.b);
        note.innerCircleSprite.size.x = note.nowInnerCircleSize;
        note.innerCircleSprite.size.y = note.nowInnerCircleSize;
        note.innerCircleSprite.color.a = note.nowAlpha * nowTotalAlpha;
        return null;
    }

}