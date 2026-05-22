using Gorge;
using GorgeFramework;
namespace BeeBoo;

class TrackerPositionTransformer :: ITransformer
{
    Tracker tracker;

    TrackerPositionTransformer(Tracker tracker)
    {
        this.tracker = tracker;
    }

    IAutomatonCommand[] Transform(float now)
    {
        tracker.trackerSprite.color.a = tracker.nowAlpha;
        Vector2 position = tracker.GetPosition(tracker.localTime);

        if(position == null)
        {
            return null;
        }

        tracker.trackerSprite.position.x = position.x;
        tracker.trackerSprite.position.y = position.y;

        return null;
    }

}