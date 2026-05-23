using Gorge;
using GorgeFramework;
namespace BeeBoo;

class LaneCurveTransformer :: ITransformer
{

    Lane lane;

    LaneCurveTransformer(Lane lane)
    {
        this.lane = lane;
    }

    IAutomatonCommand[] Transform(float now)
    {
        Vector2[] curvePoints = new Vector2[lane.pointCount];
        float step = (lane.nowDrawEndPosition - lane.nowDrawStartPosition) / (lane.pointCount - 1);
        float x = lane.nowDrawStartPosition;
        float scale = lane.nowScale;
        for (int i = 0; i < lane.pointCount; i = i + 1)
        {
            curvePoints[i] = new Vector2(x * scale, lane.laneCurve.Evaluate(x)  * scale);
            x = x + step;
        }

        lane.laneNode.points = curvePoints;
        
        return null;
    }

}