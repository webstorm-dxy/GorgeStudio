using Gorge;
using GorgeFramework;
namespace BeeBoo;

class NoteTimeVaryingVariableTransformer :: ITransformer
{
    BeeBooNote note;

    NoteTimeVaryingVariableTransformer(BeeBooNote note)
    {
        this.note = note;
    }

    IAutomatonCommand[] Transform(float now)
    {
        float t = now - note.hitTime;
        note.localTime = t;
        note.nowOuterCircleSize = note.outerCircleSize.EvaluateAdd(t);
        note.nowInnerCircleSize = note.innerCircleSize.EvaluateAdd(t);
        note.nowAlpha = note.alpha.EvaluateAdd(t);
        note.nowTotalAlpha = note.totalAlpha.EvaluateAdd(t);
        return null;
    }

}