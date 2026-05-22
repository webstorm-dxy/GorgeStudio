using Gorge;
using GorgeFramework;
namespace Obsertor;

class LineHoldPositionTransformer :: ITransformer
{
    Hold note;
    
    LineHoldPositionTransformer(Hold note)
    {
        this.note = note;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float t = now - note.hitTime;

        note.nowPosition = note.position + note.holdLine.Evaluate(t);
        
        float startDistance = note.distance.Evaluate(t);
        float startDistanceRaw = startDistance;

        // 身
        if (startDistance < 0)
        {
            startDistance = 0;
        }
        
        float endDistance = note.endDistance.Evaluate(now - note.hitTime - note.holdTime);
        if (endDistance < 0)
        {
            endDistance = 0;
        }
        
        float height = endDistance - startDistance;
        float centerY = (endDistance + startDistance) / 2;

        note.graphNode.height = height;
        note.graphNode.centerY = centerY;
        
        float startTime = now - note.hitTime;
        if (startTime < 0)
        {
            startTime = 0;
        }
        
        float endTime = (endDistance - startDistance) / note.holdLength * note.holdTime;
        
        note.holdLaneMapping.b = -startDistanceRaw * note.holdTime / note.holdLength;
        note.graphNode.ForceUpdate();
        
        return null;
    }
}