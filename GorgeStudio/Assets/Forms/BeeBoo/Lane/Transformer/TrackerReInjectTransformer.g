using Gorge;
using GorgeFramework;
namespace BeeBoo;

class TrackerReInjectTransformer :: ITransformer
{
    Tracker tracker;

    TrackerReInjectTransformer(Tracker tracker)
    {
        this.tracker = tracker;
    }

    IAutomatonCommand[] Transform(float now)
    {
        for (int i = 0; i < tracker.loopPathConfigs.length; i = i + 1)
        {
            tracker.loopPathConfigs[i].UpdateReference();
        }

        return null;
    }

}