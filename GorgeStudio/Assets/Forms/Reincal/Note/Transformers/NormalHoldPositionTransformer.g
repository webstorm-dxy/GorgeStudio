using Gorge;
using GorgeFramework;
namespace Reincal;

class NormalHoldPositionTransformer :: ITransformer
{
    Hold note;
    
    NormalHoldPositionTransformer(Hold note)
    {
        this.note = note;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float t = now - note.hitTime;
        float radius = note.lane.radius;

        // 计算当前颜色
        ColorArgb color;
        if (note.color == null)
        {
            color = new ColorArgb(1, 1, 1, 1);
        }
        else
        {
            color = note.color.Evaluate(t);
        }

        // 头尾位置计算
        float headDistanceRaw = note.distance.Evaluate(t);
        float headDistance = radius - headDistanceRaw;
        if (headDistance < 0)
        {
            headDistance = 0;
        }
        float tailDistanceRaw = note.endDistance.Evaluate(t - note.holdTime);
        float tailDistance = radius - tailDistanceRaw;
        if (tailDistance < 0)
        {
            tailDistance = 0;
        }
        float bodyStartDistance = headDistanceRaw;
        if (bodyStartDistance > radius)
        {
            bodyStartDistance = radius;
        }
        else if (bodyStartDistance < 0)
        {
            bodyStartDistance = 0;
        }
        float bodyEndDistance = tailDistanceRaw;
        if (bodyEndDistance > radius)
        {
            bodyEndDistance = radius;
        }
        else if (bodyEndDistance < 0)
        {
            bodyEndDistance = 0;
        }

        Sprite headNode = note.graphNode;
        MeshedSprite holdNode = note.holdGraphNode;
        Sprite tailNode = note.tailGraphNode;

        // 头
        // 设置头颜色
        if (headDistance < 0)
        {
            headNode.color = new ColorArgb(0, 1, 1, 1);
        }
        else
        {
            headNode.color = color;
        }

        if(headDistance > radius)
        {
            headDistance = radius;
        }

        // 设置头位置
        headNode.position.y = radius - headDistance;

        // 设置头大小
        float headSize = headDistance > radius ? 0 : (headDistance * note.size / radius);
        headNode.size.x = headSize;
        headNode.size.y = headSize; 

        // 身
        // 设置身大小
        float bodyHeight = bodyEndDistance - bodyStartDistance;
        float bodyCenterY = radius - (bodyEndDistance + bodyStartDistance) / 2;
        holdNode.height = bodyHeight;
        holdNode.centerY = bodyCenterY;

        holdNode.ForceUpdate();

        // 尾
        // 设置尾颜色
        if (tailDistance < 0)
        {
            tailNode.color = new ColorArgb(0, 1, 1, 1);
        }
        else if(tailDistance > radius)
        {
            headNode.color = new ColorArgb(0, 1, 1, 1);
            tailNode.color = new ColorArgb(0, 1, 1, 1);
        }
        else
        {
            tailNode.color = color;
        }

        // 设置尾位置
        tailNode.position.y = tailDistanceRaw;

        // 设置尾大小
        float tailSize = tailDistance > radius ? 0 : (tailDistance * note.size / radius);
        tailNode.size.x = tailSize;
        tailNode.size.y = tailSize; 

        return null;
    }
}