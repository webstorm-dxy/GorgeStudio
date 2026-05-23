using Gorge;
using GorgeFramework;
namespace BeeBoo;

class HoldBodyAnimationTransformer :: ITransformer
{
    Hold note;

    HoldBodyAnimationTransformer(Hold note)
    {
        this.note = note;
    }

    IAutomatonCommand[] Transform(float now)
    {
        string automatonState = note.automaton.GetState();
        if(automatonState == "Accepted" || automatonState == "Waiting" || automatonState == "Holding")
        {
            ColorArgb color = note.channel.nowColor;
            note.holdBody.color = new ColorArgb(color.a, color.r, color.g, color.b);
        }
        else
        {
            ColorArgb color = note.channel.nowColor;
            note.holdBody.color = new ColorArgb(color.a, 0.5, 0.5, 0.5);
        }

        float realStart = note.nowDrawStart;
        float realEnd = note.nowDrawEnd;
        if (automatonState == "Accepted")
        {
            realStart = -1;
            realEnd = -1;
        }
        else if (automatonState == "Holding")
        {
            realStart = note.localTime;
        }

        float holdEnd = note.holdTime;

        if((realStart < 0 && realEnd < 0) || (realStart > holdEnd && realEnd > holdEnd))
        {
            note.holdBody.points = null;
            return null;
        }

        if(realStart < 0)
        {
            realStart = 0;
        }
        else if(realStart > holdEnd)
        {
            realStart = holdEnd;
        }

        if(realEnd < 0)
        {
            realEnd = 0;
        }
        else if(realEnd > holdEnd)
        {
            realEnd = holdEnd;
        }

        Vector2[] curvePoints = new Vector2[note.pointCount];
        float step = (realEnd - realStart) / (note.pointCount - 1);
        float halfStep = step / 2;
        float t = realStart + note.trackerHitTime;

        for (int i = 0; i < note.pointCount; i = i + 1)
        {
            curvePoints[i] = note.tracker.GetPosition(t, halfStep);
            t = t + step;
        }

        note.holdBody.points = curvePoints;

        return null;
    }

}