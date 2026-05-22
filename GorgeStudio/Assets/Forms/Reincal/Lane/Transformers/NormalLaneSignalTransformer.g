using Gorge;
using GorgeFramework;
namespace Reincal;

class NormalLaneSignalTransformer :: ITransformer
{
    NormalLane lane;
    
    NormalLaneSignalTransformer(NormalLane lane)
    {
        this.lane = lane;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        IAutomatonCommand[] commands = new IAutomatonCommand[1];
        commands[0] = new AppendSignalCommand("NormalLane" + lane.id, 0, 0, new FloatSignal(lane.set ? 1 : 0));
        return commands;
    }
}