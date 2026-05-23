using Gorge;
using GorgeFramework;
namespace BeeBoo;

class TrackerTimeVaryingVariableTransformer :: ITransformer
{
    Tracker tracker;

    TrackerTimeVaryingVariableTransformer(Tracker tracker)
    {
        this.tracker = tracker;
    }

    IAutomatonCommand[] Transform(float now)
    {
        float t = now - tracker.startTime;
        tracker.localTime = t;
        tracker.nowAlpha = tracker.alpha.EvaluateAdd(t);
        return null;
    }

}