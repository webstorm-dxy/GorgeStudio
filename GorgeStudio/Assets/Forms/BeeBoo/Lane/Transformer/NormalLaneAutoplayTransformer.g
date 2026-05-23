using Gorge;
using GorgeFramework;
namespace BeeBoo;

class ChannelAutoplayTransformer :: ITransformer
{
    Channel channel;
    
    ChannelAutoHitTarget target;
    
    ChannelAutoplayTransformer(Channel channel) 
    {
        this.channel = channel;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        ObjectList<ChannelAutoHitTarget> autoSetTargets = channel.autoSetTargets;

        // 如果没有运动目标
        if (target == null)
        {
            // 尝试取出一个运动目标
            if (autoSetTargets.length > 0)
            {
                target = autoSetTargets.Get(0);
                autoSetTargets.RemoveAt(0);
            }
            // 如果没有运动目标，则直接结束
            else
            {
                channel.set = false;
                return null;
            }
        }
        
        // 到此处必然有运动目标
        // 持续弹出队列，更新到恰当的置位目标
        while (target != null ? (target.time + target.holdTime <= now) : false)
        {
            if (autoSetTargets.length == 0)
            {
                target = null;
            }
            else
            {
                target = autoSetTargets.Get(0);
                autoSetTargets.RemoveAt(0);
            }
        }

        // 如果此时没有目标，则设为非置位
        if (target == null)
        {
            channel.set = false;
            return null;
        }

        // 超时设为非置位
        if((target.time + target.holdTime) < now || now < target.time)
        {
            channel.set = false;
            return null;
        }

        // 有目标且未超时的情况，设置为置位
        channel.set = true;
        return null;
    }
}