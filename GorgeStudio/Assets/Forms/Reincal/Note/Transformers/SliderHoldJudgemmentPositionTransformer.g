using Gorge;
using GorgeFramework;
namespace Reincal;

class SliderHoldJudgementPositionTransformer :: ITransformer
{
    ReincalSliderHold note;

    SliderHoldJudgementPositionTransformer(ReincalSliderHold note)
    {
        this.note = note;
    }

    IAutomatonCommand[] Transform(float now)
    {
        note.judgementPosition = note.position + note.sliderLine.Evaluate(now - note.hitTime);
        return null;
    }
    
}