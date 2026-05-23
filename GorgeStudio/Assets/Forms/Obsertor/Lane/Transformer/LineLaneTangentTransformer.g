using Gorge;
using GorgeFramework;
namespace Obsertor;

class LineLaneTangentTransformer :: ITransformer
{
    LineLane lane;
    
    LineLaneTangentTransformer(LineLane lane)
    {
        this.lane = lane;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float tangent = lane.tangent.Evaluate(now);
        lane.curve.startTangent = tangent;
        lane.curve.endTangent = tangent;
        int pointCount = 1000;
        Vector2[] curvePoints = new Vector2[pointCount];
        float startX = -8;
        float endX = 8;
        float step = (endX - startX) / (pointCount - 1);
        float x = startX;
        for (int i = 0; i < pointCount; i = i + 1)
        {
            curvePoints[i] = new Vector2(x, lane.curve.Evaluate(x));
            x = x + step;
        }
        
        lane.lineGraphNode.points = curvePoints;
        return null;
    }
}