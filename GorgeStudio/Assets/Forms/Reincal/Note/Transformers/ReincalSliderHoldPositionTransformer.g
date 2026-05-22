using Gorge;
using GorgeFramework;
namespace Reincal;

class ReincalSliderHoldPositionTransformer :: ITransformer
{
    ReincalSliderHold note;
    
    ReincalSliderHoldPositionTransformer(ReincalSliderHold note)
    {
        this.note = note;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float t = now - note.hitTime;
        // 头
        float startDistanceRaw = note.distance.Evaluate(t);
        // 与圆心的距离
        float centerDistance = note.judgementLineUnmappedRadius - startDistanceRaw;
        if (centerDistance < 0)
        {
            centerDistance = 0;
        }
        
        note.headGraphNode.centerY = -centerDistance;
        if (t > 0)
        {
            note.headGraphNode.color = new ColorArgb(0, 1, 1, 1);
        }
        else
        {
            note.headGraphNode.color = new ColorArgb(1, 1, 1, 1);
        }
        
        // 身
        float startDistance = startDistanceRaw;
        if (startDistance > note.judgementLineUnmappedRadius)
        {
            startDistance = note.judgementLineUnmappedRadius;
        }
        else if (startDistance < 0)
        {
            startDistance = 0;
        }
        
        float endDistance = note.endDistance.Evaluate(now - note.hitTime - note.holdTime);
        if (endDistance > note.judgementLineUnmappedRadius)
        {
            endDistance = note.judgementLineUnmappedRadius;
        }
        else if (endDistance < 0)
        {
            endDistance = 0;
        }
        
        float height = endDistance - startDistance;
        float centerY = (endDistance + startDistance) / 2 - note.judgementLineUnmappedRadius;
        note.holdGraphNode.height = height;
        note.holdGraphNode.centerY = centerY;
        
        float startTime = now - note.hitTime;
        if (startTime < 0)
        {
            startTime = 0;
        }
        
        float endTime = (endDistance - startDistance) / note.holdLength * note.holdTime;
        
        note.holdLaneMapping.b = (note.judgementLineUnmappedRadius - startDistanceRaw) * note.holdTime / note.holdLength;
        note.holdGraphNode.ForceUpdate();
        
        // 尾
        // 与圆心的距离
        float centerEndDistance = note.judgementLineUnmappedRadius - endDistance;
        if (centerEndDistance < 0)
        {
            centerEndDistance = 0;
        }
        
        note.tailGraphNode.centerY = -centerEndDistance;
        if (t > note.holdTime)
        {
            note.tailGraphNode.color = new ColorArgb(0, 1, 1, 1);
        }
        else
        {
            note.tailGraphNode.color = new ColorArgb(1, 1, 1, 1);
        }
        
        return null;
    }
}