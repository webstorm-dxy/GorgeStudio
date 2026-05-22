using Gorge;
using GorgeFramework;
namespace Reincal;

[
    delegate<float:ReincalSliderTap^> display = string:(ReincalSliderTap^ noteInjector) ->
    {
        return noteInjector.^hitTime + "";
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 1, g : 0.6431372549019608, b : 0},
    delegate<ElementLine:ReincalSliderTap^> elementLine = ElementLine:(ReincalSliderTap^ noteInjector) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[1];
        points[0] = new ElementLinePoint(noteInjector.^hitTime, 0.3, 0.2);
        return new ElementLine(new ColorArgb(1, 1, 0.6431372549019608, 0), points);
    },
    string displayName = "Note"
]
@EditableElement(type = "Note", editUpdateMode = EditUpdateMode.RePlay)
class ReincalNote : Note
{
    [
        auto defaultValue = 0.0,
        string type = "基本",
        int order = 1,
        string displayName = "打击时刻",
        string information = "单位秒，>=0",
        delegate<bool:float> check = bool:(float hitTime) -> { return hitTime >= 0; },
        string timePointName = "HitTime",
        string timePointEarlyAnchor = null,
        string timePointLateAnchor = null,
        string timePointRegion = "RhythmDuration"
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
        string timePointEarlyAnchor = "RhythmDuration.End",
        string timePointLateAnchor = null,
        bool majorTimePoint = false
    ]
    @Inject
    float lagTime = ^lagTime;

    [
        auto defaultValue = LinearFunctionCurve : {k : -8.0, b : 0.0},
        string type = "基本",
        int order = 2,
        string displayName = "距离曲线",
        string information = "标准坐标系|横轴为以打击时刻为0点的时间，单位秒；纵轴为距离轨道判定线的距离",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ positionX) -> { return true; }
    ]
    @Inject<FunctionCurve^>
    FunctionCurve distance = new ^distance();

    [
        auto defaultValue = null,
        string type = "效果",
        int order = 2001,
        string displayName = "颜色",
        string information = "横轴为以打击时刻为0的时间，单位秒",
        delegate<bool:ColorCurve^> check = bool:(ColorCurve^ color) -> { return true; }
    ]
    @Inject<ColorCurve^>
    ColorCurve color = (^color == null) ? null : (new ^color());

    [
        auto defaultValue = false,
        string type = "效果",
        int order = 2101,
        string displayName = "无需打击",
        string information = "该Note是否无需打击",
        delegate<bool:bool> check = bool:(bool color) -> { return true; }
    ]
    @Inject<bool>
    bool isFake = ^isFake;

    ReincalNote() : super()
    {
    }

    @PeriodModifier
    static void PeriodModifier(ReincalNote^ noteInjector, PeriodConfig periodConfig)
    {
        noteInjector.^hitTime = noteInjector.^hitTime + periodConfig.timeOffset;
    }
}