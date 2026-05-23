using Gorge;
using GorgeFramework;
namespace BeeBoo;

class HoldTimeVaryingVariableTransformer :: ITransformer
{
    Hold hold;

    HoldTimeVaryingVariableTransformer(Hold hold)
    {
        this.hold = hold;
    }

    IAutomatonCommand[] Transform(float now)
    {
        hold.nowDrawStart = hold.drawStart.Evaluate(hold.localTime);
        hold.nowDrawEnd = hold.drawEnd.Evaluate(hold.localTime);
        return null;
    }

}