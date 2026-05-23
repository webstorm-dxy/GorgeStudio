using Gorge;
using GorgeFramework;
namespace BeeBoo;

class ChannelSignalTransformer :: ITransformer
{
    Channel channel;
    
    ChannelSignalTransformer(Channel channel)
    {
        this.channel = channel;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        FloatSignal signal;
        if(channel.nowEnable < 0)
        {
            signal = null;
        }
        else
        {
            signal = new FloatSignal(channel.set ? 1 : 0);
        }
        IAutomatonCommand[] commands = new IAutomatonCommand[1];
        commands[0] = new AppendSignalCommand("Channel" + channel.id, 0, 0, signal);
        return commands;
    }
}