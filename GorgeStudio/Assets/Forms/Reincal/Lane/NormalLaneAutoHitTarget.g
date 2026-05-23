using Gorge;
using GorgeFramework;
namespace Reincal;

class NormalLaneAutoHitTarget
{
    float time;
    float holdTime;
    
    NormalLaneAutoHitTarget(float time, float holdTime)
    {
        this.time = time;
        this.holdTime = holdTime;
    }
}