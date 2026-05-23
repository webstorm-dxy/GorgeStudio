using Gorge;
using GorgeFramework;
namespace Reincal;

class NormalLanePositionTransformer :: ITransformer
{
    NormalLane lane;
    
    NormalLanePositionTransformer(NormalLane lane)
    {
        this.lane = lane;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        float t = now - lane.generateTime;
        float position = lane.position.EvaluateAdd(t);
        lane.nowPosition = position;
        // 这里的分母是滑动轨道的size
        float radius = lane.radius / 19.14;
        float x = Math.Sin(position) * radius;
        float y = - Math.Cos(position) * radius;
        lane.graphNode.position.x = x;
        lane.graphNode.position.y = y;
        float roataion = position * 180 / 3.1415926;
        lane.graphNode.rotation.z = roataion;
        return null;
    }
}