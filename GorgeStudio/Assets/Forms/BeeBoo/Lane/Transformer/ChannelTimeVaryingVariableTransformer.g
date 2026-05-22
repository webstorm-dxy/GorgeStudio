using Gorge;
using GorgeFramework;
namespace BeeBoo;

class ChannelTimeVaryingVariableTransformer :: ITransformer
{
    Channel channel;

    ChannelTimeVaryingVariableTransformer(Channel channel)
    {
        this.channel = channel;
    }

    IAutomatonCommand[] Transform(float now)
    {
        channel.nowEnable = channel.enable.EvaluateAdd(now);
        channel.nowColor = channel.channelColor.Evaluate(now);
        return null;
    }

}