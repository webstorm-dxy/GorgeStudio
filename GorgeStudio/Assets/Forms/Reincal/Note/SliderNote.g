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
    string displayName = "滑动Note"
]
@EditableElement(type = "Note", editUpdateMode = EditUpdateMode.RePlay)
class SliderNote : ReincalNote
{
    [
        auto defaultValue = 0,
        string type = "基本",
        int order = 2,
        string displayName = "位置",
        string information = "角度|横轴为以打击时刻为0点的时间，单位秒；纵轴为相对于角度，取基础值加曲线纵轴",
        delegate<bool:float> check = bool:(float position) -> { return true; }
    ]
    @Inject<float>
    float position = ^position;
    
    // 实际判定线半径
    float judgementLineRadius = 6.239245;
    
    // 使用二次函数映射前的判定线半径
    float judgementLineUnmapRadius;
    
    SliderNote() : super()
    {
    }
}