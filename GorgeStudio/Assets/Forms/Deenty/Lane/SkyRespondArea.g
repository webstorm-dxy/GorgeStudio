using Gorge;
using GorgeFramework;
namespace Deenty;

class SkyRespondArea :: IRespondArea
{
    DeentySkySingleNote note;
    
    SkyRespondArea(DeentySkySingleNote note)
    {
        this.note = note;
    }
    
    bool IsInRespondArea(TouchSignal touch)
    {
        SkyArea skyArea = (SkyArea) Environment.FindAliveLane("Deenty.SkyArea", note.laneName);
        // 获取失败则返回false
        if (skyArea == null)
        {
            return false;
        }
        
        Vector3 touchPointPosition = skyArea.judgementNode.GlobalPositionToLocalPosition(touch.position.ToVector3());
        
        // 如果XY轴均在判定边界内，则判定命中，只受理论尺寸影响，不随尺寸变化而改变
        float judgementBound = Math.Abs(note.size.baseValue) / 2 + skyArea.respondAreaHalfAdditionInterval;
        return Math.Abs(touchPointPosition.x - note.positionX.baseValue) <= judgementBound &&
               Math.Abs(touchPointPosition.y - note.positionY.baseValue) <= judgementBound;
    }
    
    float BestDistance(TouchSignal touch)
    {
        // 直接在Global坐标下比较
        return Vector2.Distance(touch.position, note.graphNode.GlobalPosition().ToVector2());
    }
}