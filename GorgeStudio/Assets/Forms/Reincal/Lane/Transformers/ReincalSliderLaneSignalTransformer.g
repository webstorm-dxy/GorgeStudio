using Gorge;
using GorgeFramework;
namespace Reincal;

class ReincalSliderLaneSignalTransformer :: ITransformer
{
    ReincalSliderLane lane;
    
    ReincalSliderLaneSignalTransformer(ReincalSliderLane lane)
    {
        this.lane = lane;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        //lane.tsigaChecked = false;
        IAutomatonCommand[] commands = new IAutomatonCommand[1];
        commands[0] = new AppendSignalCommand("SliderLaneRotation", 0, 0, new FloatSignal(lane.rotation));
        return commands;
    }
}