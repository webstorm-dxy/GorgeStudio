using Gorge;
using GorgeFramework;
namespace Deenty;

class LineRespondArea :: IRespondArea
{
    DeentyLineNote note;
    
    LineRespondArea(DeentyLineNote note)
    {
        this.note = note;
    }
    
    bool IsInRespondArea(TouchSignal touch)
    {
        Line line = (Line) Environment.FindAliveLane("Deenty.Line", note.laneName);
        // 获取失败则返回false
        if (line == null)
        {
            return false;
        }
        
        Vector3 touchPointPosition = line.judgementNode.GlobalPositionToLocalPosition(touch.position.ToVector3());
        
        // 如果XY轴均在判定边界内，则判定命中，只受理论尺寸影响，不随尺寸变化而改变
        float judgementBoundX = line.respondAreaXHalfInterval;
        float judgementBoundY = Math.Abs(note.length.baseValue) / 2 + line.respondAreaYHalfAdditionInterval;
        return Math.Abs(touchPointPosition.x) <= judgementBoundX && Math.Abs(touchPointPosition.y - note.positionY.baseValue) <= judgementBoundY;
    }
    
    float BestDistance(TouchSignal touch)
    {
        // 直接在Global坐标下比较
        return Vector2.Distance(touch.position, note.graphNode.GlobalPosition().ToVector2());
    }
}