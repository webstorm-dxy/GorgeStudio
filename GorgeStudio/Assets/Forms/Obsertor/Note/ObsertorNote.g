using Gorge;
using GorgeFramework;
namespace Obsertor;

class ObsertorNote : Note
{
    [
        auto defaultValue = 0.0,
        string type = "基本",
        int order = 1,
        string displayName = "打击时刻",
        string information = "单位秒，>=0",
        delegate<bool:float> check = bool:(float hitTime) -> { return true; },
        string timePointName = "HitTime",
        string timePointEarlyAnchor = null,
        string timePointLateAnchor = null
    ]
    @Inject
    float hitTime = ^hitTime;
    
    [
        auto defaultValue = 1.5,
        string type = "生命周期",
        int order = 1000,
        string displayName = "超前生成时间",
        string information = "单位秒，>=0",
        delegate<bool:float> check = bool:(float leadTime) -> { return leadTime >= 0; },
        string timePointName = "GenerateTime",
        string timePointEarlyAnchor = null,
        string timePointLateAnchor = "HitTime",
        bool majorTimePoint = false
    ]
    @Inject
    float leadTime = ^leadTime;
    
    [
        auto defaultValue = 1.5,
        string type = "生命周期",
        int order = 1001,
        string displayName = "滞后销毁时间",
        string information = "单位秒，>=0",
        delegate<bool:float> check = bool:(float lagTime) -> { return lagTime >= 0; },
        string timePointName = "DestroyTime",
        string timePointEarlyAnchor = "HitTime",
        string timePointLateAnchor = null,
        bool majorTimePoint = false
    ]
    @Inject
    float lagTime = ^lagTime;

    [
        auto defaultValue = LinearFunctionCurve : {k : -96.0, b : 0.0},
        string type = "基本",
        int order = 2,
        string displayName = "距离曲线",
        string information = "标准坐标系|横轴为以打击时刻为0点的时间，单位秒；纵轴为距离轨道判定线的距离",
        delegate<bool:FunctionCurve^> check = bool:(FunctionCurve^ distance) -> { return true; }
    ]
    @Inject<FunctionCurve^>
    FunctionCurve distance = new ^distance();

    Node colorNode;

    ObsertorNote() : super()
    {
    }

    @PeriodModifier
    static void PeriodModifier(ObsertorNote^ noteInjector, PeriodConfig periodConfig)
    {
        noteInjector.^hitTime = noteInjector.^hitTime + periodConfig.timeOffset;
    }

    float GetAimDistance(TouchSignal signal)
    {
        // virtual
        return 0;
    }
}