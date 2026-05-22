using Gorge;
using GorgeFramework;
namespace BeeBoo;

class ChannelAutoHitTarget
{
    float time;
    float holdTime;
    
    ChannelAutoHitTarget(float time, float holdTime)
    {
        this.time = time;
        this.holdTime = holdTime;
    }
}