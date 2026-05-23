using Gorge;
using GorgeFramework;
namespace Deenty;

interface IRespondArea
{
    bool IsInRespondArea(TouchSignal touch);
    
    float BestDistance(TouchSignal touch);
}