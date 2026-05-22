using Gorge;
using GorgeFramework;
namespace DeentyStoryboard;

class StoryboardEntity : Element
{
    [
        string type = "基本",
        int order = 0,
        string displayName = "资源名",
        string information = "所显示的图片资源名称",
        delegate<bool:string> check = bool:(string assetName) -> { return true; }
    ]
    @Inject
    string assetName = ^assetName;
    
    [
        auto defaultValue = 0.0,
        string type = "基本",
        int order = 1,
        string displayName = "出现时刻",
        string information = "单位秒",
        delegate<bool:float> check = bool:(float startMoment) -> { return true; }
    ]
    @Inject
    float startMoment = ^startMoment;
    
    [
        auto defaultValue = 1.0,
        string type = "基本",
        int order = 2,
        string displayName = "保持时间",
        string information = "单位秒，>0",
        delegate<bool:float> check = bool:(float keepTime) -> { return keepTime > 0; }
    ]
    @Inject
    float keepTime = ^keepTime;
    
    [
        auto defaultValue = ImagePositionMode.ScreenX,
        string type = "基本",
        int order = 3,
        string displayName = "定位模式",
        string information = "控制图片在屏幕上的定位方式",
        delegate<bool:float> check = bool:(ImagePositionMode positionMode) -> { return true; }
    ]
    @Inject
    ImagePositionMode positionMode = ^positionMode;
    
    [
        auto defaultValue = VariableFloat : {baseValue : 0.0},
        string type = "基本",
        int order = 4,
        string displayName = "X坐标",
        string information = "数字，意义取决于坐标模式|横轴为保持进度，0代表出现时间，1代表消失时间；纵轴为坐标加值，实时坐标为基值加上纵轴值",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ positionX) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat positionX = new ^positionX();
    
    [
        auto defaultValue = VariableFloat : {baseValue : 0.0},
        string type = "基本",
        int order = 5,
        string displayName = "Y坐标",
        string information = "数字，意义取决于坐标模式|横轴为保持进度，0代表出现时间，1代表消失时间；纵轴为坐标加值，实时坐标为基值加上纵轴值",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ positionY) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat positionY = new ^positionY();
    
    [
        auto defaultValue = VariableFloat : {baseValue : 0.0},
        string type = "基本",
        int order = 6,
        string displayName = "角度",
        string information = "标准坐标系|横轴为保持进度，0代表出现时间，1代表消失时间；纵轴为角度加值，实时角度为基值加上纵轴值",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ rotationZ) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat rotationZ = new ^rotationZ();
    
    [
        auto defaultValue = VariableFloat : {baseValue : 1.0},
        string type = "基本",
        int order = 7,
        string displayName = "横向尺寸",
        string information = "数字，意义取决于坐标模式|横轴为保持进度，0代表出现时间，1代表消失时间；纵轴为尺寸加值，实时尺寸为基值加上纵轴值",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ scaleX) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat scaleX = new ^scaleX();
    
    [
        auto defaultValue = VariableFloat : {baseValue : 1.0},
        string type = "基本",
        int order = 8,
        string displayName = "纵向尺寸",
        string information = "数字，意义取决于坐标模式|横轴为保持进度，0代表出现时间，1代表消失时间；纵轴为尺寸加值，实时尺寸为基值加上纵轴值",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ scaleY) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat scaleY = new ^scaleY();
    
    [
        auto defaultValue = VariableFloat : {baseValue : 1.0},
        string type = "效果",
        int order = 1000,
        string displayName = "叠放顺序",
        string information = "数字，值小者覆盖值大者，超出+-10则不显示|横轴为保持进度，0代表出现时刻，1代表消失时刻；纵轴为叠放顺序加值，实时叠放顺序为基值加上纵轴值",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ positionZ) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat positionZ = new ^positionZ();
    
    @Editor(code = "new FieldEditorAttribute(EditType.Effect, 1001, \"不透明度\", \"数字，0-1之间|横轴为保持进度，0代表出现时刻，1代表消失时刻；纵轴为插值进度，0代表基值，1代表完全不透明，-1代表完全透明\", new Global.Utilities.VariableFloat {BaseValue = 1f}, new PropertyCheckerVariableFloatBaseGe0Le1(), null)")
    [
        auto defaultValue = VariableFloat : {baseValue : 1.0},
        string type = "效果",
        int order = 1001,
        string displayName = "不透明度",
        string information = "数字，0-1之间|横轴为保持进度，0代表出现时刻，1代表消失时刻；纵轴为插值进度，0代表基值，1代表完全不透明，-1代表完全透明",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ alpha) -> { return alpha.^baseValue >= 0 && alpha.^baseValue <= 1; }
    ]
    @Inject<VariableFloat^>
    VariableFloat alpha = new ^alpha();
    
    [
        auto defaultValue = false,
        string type = "基本",
        int order = 9,
        string displayName = "使用谱面时间",
        string information = "是否在曲线计算时使用谱面时间，否则使用进度",
        delegate<bool:VariableFloat^> check = bool:(bool useChartTime) -> { return true; }
    ]
    @Inject<bool>
    bool useChartTime = ^useChartTime;
    
    Node mainNode;
    
    StoryboardEntity()
    {
    }
}