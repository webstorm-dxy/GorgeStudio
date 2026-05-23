using Gorge;
using GorgeFramework;
namespace Reincal;

class NormalLaneSettingCircleTransformer :: ITransformer
{
    NormalLane lane;

    float speed = 30;
    float base;
    float alphaSpeed = 6;
    float lastAlphaChangeTime = 0;
    
    NormalLaneSettingCircleTransformer(NormalLane lane)
    {
        this.lane = lane;
        base = Random.RandomFloat(0, 180);
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        // 环自转
        float nowRotation = base + now * speed;
        Sprite circle = lane.settingCircleNode;
        circle.rotation.z = nowRotation;

        // 根据按下状态调整环的不透明度
        if(lastAlphaChangeTime == 0)
        {
            lastAlphaChangeTime = now;
        }
        float alphaChangeTime = now - lastAlphaChangeTime;
        lastAlphaChangeTime = now;
        ColorArgb color = circle.color;
        float alphaNow = color.a;
        if(lane.set)
        {
            alphaNow = alphaNow + alphaSpeed * alphaChangeTime * 3;
            if(alphaNow > 1)
            {
                alphaNow = 1;
            }
        }
        else
        {
            alphaNow = alphaNow - alphaSpeed * alphaChangeTime;
            if(alphaNow < 0)
            {
                alphaNow = 0;
            }
        }
        color.a = alphaNow;
        return null;
    }
}