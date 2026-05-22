using Gorge;
using GorgeFramework;
namespace Deenty;

class DeentyNote : Note
{
    [
        string type = "基本",
        int order = -1,
        string displayName = "轨道名",
        string information = "所属轨道的名称",
        delegate<bool:string> check = bool:(string laneName) -> { return true; }
    ]
    @Inject
    string laneName = ^laneName;
    
    [
        auto defaultValue = 0.0,
        string type = "基本",
        int order = 0,
        string displayName = "响应时刻",
        string information = "单位秒，>=0",
        delegate<bool:float> check = bool:(float respondMoment) -> { return respondMoment >= 0; }
    ]
    @Inject
    float respondMoment = ^respondMoment;
    
    [
        auto defaultValue = 0.6,
        string type = "基本",
        int order = 1,
        string displayName = "响应时间",
        string information = "单位秒，>0",
        delegate<bool:float> check = bool:(float respondTime) -> { return respondTime > 0; }
    ]
    @Inject
    float respondTime = ^respondTime;
    
    [
        auto defaultValue = 0.6,
        string type = "基本",
        int order = 2,
        string displayName = "驻留时间",
        string information = "单位秒，>0",
        delegate<bool:float> check = bool:(float stayTime) -> { return stayTime > 0; }
    ]
    @Inject
    float stayTime = ^stayTime;
    
    [
        auto defaultValue = VariableFloat : {baseValue : -5.0},
        string type = "效果",
        int order = 1000,
        string displayName = "叠放顺序",
        string information = "数字，值小者覆盖值大者，超出+-10则不显示|横轴为响应进度，0代表出现时刻，1代表响应时刻；纵轴为叠放顺序加值，实时叠放顺序为基值加上纵轴值",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ positionZ) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat positionZ = new ^positionZ();
    
    [
        auto defaultValue = VariableFloat : {baseValue : 1.0},
        string type = "效果",
        int order = 1001,
        string displayName = "不透明度",
        string information = "数字，0-1之间|横轴为响应进度，0代表出现时刻，1代表响应时刻；纵轴为插值进度，0代表基值，1代表完全不透明，-1代表完全透明",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ alpha) -> { return alpha.^baseValue >= 0 && alpha.^baseValue <= 1; }
    ]
    @Inject<VariableFloat^>
    VariableFloat alpha = new ^alpha();
    
    [
        auto defaultValue = false,
        string type = "效果",
        int order = 1002,
        string displayName = "多押提示",
        string information = "开启|关闭",
        delegate<bool:bool> check = bool:(bool isChord) -> { return true; }
    ]
    @Inject
    bool isChord = ^isChord;
    
    [
        auto defaultValue = 0.6,
        string type = "响应",
        int order = 1003,
        string displayName = "击中效果时间",
        string information = "数字，秒，需大于0",
        delegate<bool:float> check = bool:(float respondHintKeepTime) -> { return respondHintKeepTime > 0; }
    ]
    @Inject
    float respondHintKeepTime = ^respondHintKeepTime;
    
    [
        auto defaultValue = VariableFloat : {baseValue : 2.0},
        string type = "响应",
        int order = 1004,
        string displayName = "击中效果尺寸",
        string information = "数字，标准坐标系|横轴为效果进度，0代表出现时刻，1代表结束时刻；纵轴为尺寸加值，实时尺寸为基值加上纵轴值",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ respondHintSize) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat respondHintSize = new ^respondHintSize();
    
    [
        auto defaultValue = null,
        string type = "响应",
        int order = 1005,
        string displayName = "击中效果曲线",
        string information = "横轴为效果进度，0代表出现时刻，1代表结束时刻；纵轴为动画进度，0代表动画起初，1代表动画末尾",
        delegate<bool:FunctionCurve> check = bool:(FunctionCurve respondHintProcessCurve) -> { return true; }
    ]
    @Inject<FunctionCurve^>
    FunctionCurve respondHintProcessCurve = ^respondHintProcessCurve != null
                                            ? new ^respondHintProcessCurve()
                                            : ((FunctionCurve) new LinearCurve(0.0, 0.0, 1.1, 1.1));
    
    [
        auto defaultValue = VariableFloat : {baseValue : 0.0},
        string type = "效果",
        int order = 1006,
        string displayName = "色相",
        string information = "数字，0到1之间，超出循环|横轴为响应进度，0代表出现时刻，1代表响应时刻；纵轴为色相加值，实时色相为基值加上纵轴值",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ hue) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat hue = new ^hue();
    
    [
        auto defaultValue = VariableFloat : {baseValue : 0.0},
        string type = "效果",
        int order = 1007,
        string displayName = "饱和度",
        string information = "数字，差值进度，0代表基值，1代表全饱和，-1代表零饱和|纵轴为插值进度，0代表基值，1代表饱和度1，-1代表饱和度-1",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ saturation) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat saturation = new ^saturation();
    
    [
        auto defaultValue = VariableFloat : {baseValue : 0.0},
        string type = "效果",
        int order = 1008,
        string displayName = "亮度",
        string information = "数字，差值进度，0代表基值，1代表全亮，-1代表全黑|纵轴为插值进度，0代表基值，1代表亮度1，-1代表亮度-1",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ brightness) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat brightness = new ^brightness();
    
    [auto defaultValue = null]
    @Inject
    NoteLinkage linkage = ^linkage;
    
    string laneType;
    bool isDark;
    
    NineSliceSprite graphNode;
    
    IRespondArea respondArea;
    
    DeentyNote()
    {
    }
    
    float HideMoment()
    {
        return respondMoment + stayTime;
    }
    
    int Combo()
    {
        return 1;
    }
    
    float AutoPlayHoldTime()
    {
        return 0.1;
    }
    
    void ReInjectNote(DeentyNote^ newInjector)
    {
        laneName = newInjector.^laneName;
        respondMoment = newInjector.^respondMoment;
        respondTime = newInjector.^respondTime;
        stayTime = newInjector.^stayTime;
        positionZ = new (newInjector.^positionZ)();
        alpha = new (newInjector.^alpha)();
        isChord = newInjector.^isChord;
        respondHintKeepTime = newInjector.^respondHintKeepTime;
        respondHintSize = new (newInjector.^respondHintSize)();
        respondHintProcessCurve = newInjector.^respondHintProcessCurve != null
                                  ? new (newInjector.^respondHintProcessCurve)()
                                  : (FunctionCurve) new LinearCurve(0.0, 0.0, 1.1, 1.1);
        hue = new (newInjector.^hue)();
        saturation = new (newInjector.^saturation)();
        brightness = new (newInjector.^brightness)();
    }
    
    @PeriodModifier
    static void EnablePeriodTimeOffset(DeentyNote^ noteInjector, PeriodConfig periodConfig)
    {
        noteInjector.^respondMoment = noteInjector.^respondMoment + periodConfig.timeOffset;
    }
}