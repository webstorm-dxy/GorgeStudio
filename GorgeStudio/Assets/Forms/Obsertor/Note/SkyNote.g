using Gorge;
using GorgeFramework;
namespace Obsertor;

class SkyNote : ObsertorNote
{
    [
        auto defaultValue = Vector2 : {x : 0, y : 0},
        string type = "基本",
        int order = 2,
        string displayName = "位置",
        string information = "角度|横轴为以打击时刻为0点的时间，单位秒；纵轴为相对于角度，取基础值加曲线纵轴",
        delegate<bool:Vector2^> check = bool:(Vector2^ position) -> { return true; }
    ]
    @Inject<Vector2^>
    Vector2 position = new ^position();

    Sprite indicatorNode;
    Sprite graphNode;
    
    SkyNote() : super()
    {
    }

    float GetAimDistance(TouchSignal signal)
    {
        return Vector2.Distance(signal.position, position);
    }
}