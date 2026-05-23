using Gorge;
using GorgeFramework;
namespace Deenty;

class LineLengthTransformer :: ITransformer
{
    Line lane;
    
    LineLengthTransformer(Line lane)
    {
        this.lane = lane;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        Vector2 baseSize = Environment.ViewportSize();
        
        // 计算各种情况下的判定线本地尺寸
        switch (lane.lengthMode)
        {
            case LineLengthMode.GamePlayPanel:
                // 直接赋值
                lane.lineGraphNode.size.y = lane.length.EvaluateAdd(now);
                break;
            case LineLengthMode.ScreenX:
                // 渲染基的宽度乘以长度比例
                lane.lineGraphNode.size.y = baseSize.x * lane.length.EvaluateAdd(now);
                break;
            case LineLengthMode.ScreenY:
                // 渲染基的高度乘以长度比例
                lane.lineGraphNode.size.y = baseSize.y * lane.length.EvaluateAdd(now);
                break;
            case LineLengthMode.Enough:
                // 计算渲染基的四个角的世界坐标
                float baseHalfSizeX = baseSize.x / 2;
                float baseHalfSizeY = baseSize.y / 2;
                Vector2 lanePosition = lane.positionNode.position.ToVector2();
                // 计算渲染基的四个角到自身的距离
                float d1 = Vector2.Distance(lanePosition, new Vector2(-baseHalfSizeX, -baseHalfSizeY));
                float d2 = Vector2.Distance(lanePosition, new Vector2(baseHalfSizeX, -baseHalfSizeY));
                float d3 = Vector2.Distance(lanePosition, new Vector2(-baseHalfSizeX, baseHalfSizeY));
                float d4 = Vector2.Distance(lanePosition, new Vector2(baseHalfSizeX, baseHalfSizeY));
                // 计算渲染基的四个角到自身的距离的最大值
                float maxDistance = Math.Max(d1, d2, d3, d4);
                // 长度是该距离的两倍，转换到本地后再+1确保超出
                lane.lineGraphNode.size.y = maxDistance * 2 + 1;
                break;
        }
        return null;
    }
}