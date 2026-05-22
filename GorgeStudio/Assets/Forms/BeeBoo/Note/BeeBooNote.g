using Gorge;
using GorgeFramework;
namespace BeeBoo;

[
    delegate<float:BeeBooNote^> display = string:(BeeBooNote^ laneInjector) ->
    {
        return laneInjector.^hitTime + "";
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 0.2396693, g : 0.6370158, b : 0.8},
    delegate<ElementLine:BeeBooNote^> elementLine = ElementLine:(BeeBooNote^ lane) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[2];
        points[0] = new ElementLinePoint(lane.^hitTime, 0.5, 0.8);
        return new ElementLine(new ColorArgb(0.3, 1, 1, 1), points);
    },
    string displayName = "Note"
]
@EditableElement(type = "Note", editUpdateMode = EditUpdateMode.RePlay)
class BeeBooNote : Note
{
    // 注入字段
    
    [
        auto defaultValue = 0,
        string type = "基本",
        int order = 0,
        string displayName = "追踪器编号",
        string information = "",
        delegate<bool:int> check = bool:(int trackerId) -> { return true; }
    ]
    @Inject
    int trackerId = ^trackerId;

    [
        auto defaultValue = 0,
        string type = "基本",
        int order = 1,
        string displayName = "触控通道编号",
        string information = "",
        delegate<bool:int> check = bool:(int channelId) -> { return true; }
    ]
    @Inject
    int channelId = ^channelId;

    [
        auto defaultValue = 0.0,
        string type = "基本",
        int order = 2,
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
        auto defaultValue = 3.0,
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
        auto defaultValue = 1.6,
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
        auto defaultValue = VariableFloat : {
            baseValue : 0.7,
            variationCurve : PiecewiseFunctionCurve : {
                functionPieces : FunctionPiece^ : {
                    FunctionPiece : {
                        functionCurve : CubicHermiteSpline : {
                            startPoint : Vector2 : {
                                x : -1.5,
                                y : 0.0,
                            },
                            startWeight : 0.0,
                            endPoint : Vector2 : {
                                x : -0.3,
                                y : 0.2,
                            },
                            endWeight : 0.5,
                        },
                        startX : (-1.0/0.0),
                        endX : -0.3,
                    },
                    FunctionPiece : {
                        functionCurve : CubicHermiteSpline : {
                            startPoint : Vector2 : {
                                x : -0.3,
                                y : 0.2,
                            },
                            startTangent : 0.0,
                            startWeight : 0.5,
                            endPoint : Vector2 : {
                                x : 0.0,
                                y : 0.0,
                            },
                            endWeight : 0.0,
                        },
                        startX : -0.3,
                        endX : (1.0/0.0),
                    },
                },
            },
        },
        string type = "效果",
        int order = 2000,
        string displayName = "外圈尺寸",
        string information = "标注坐标",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ outerCircleSize) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat outerCircleSize = new ^outerCircleSize();

    [
        auto defaultValue = VariableFloat : {
            baseValue : 0.7, 
            variationCurve : LinearCurve : {
                timeStart : -1.5,
                valueStart : -0.5,
                timeEnd : 0.0,
                valueEnd : 0.0
            }
        },
        string type = "效果",
        int order = 2001,
        string displayName = "内圈尺寸",
        string information = "标注坐标",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ innerCircleSize) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat innerCircleSize = new ^innerCircleSize();

    [
        auto defaultValue = VariableFloat : {
            baseValue : 0.0,
            variationCurve : PiecewiseFunctionCurve : {
                functionPieces : FunctionPiece^ : {
                    FunctionPiece : {
                        functionCurve : CubicHermiteSpline : {
                            startPoint : Vector2 : {
                                x : -1.5,
                                y : 0.0,
                            },
                            endPoint : Vector2 : {
                                x : -0.3,
                                y : 0.2,
                            },
                            endWeight : 0.0,
                        },
                        startX : (-1.0/0.0),
                        endX : -0.3,
                    },
                    FunctionPiece : {
                        functionCurve : ConstantFunctionCurve : {
                            value : 0.45,
                        },
                        startX : -0.26,
                        endX : -0.24,
                    },
                    FunctionPiece : {
                        functionCurve : ConstantFunctionCurve : {
                            value : 0.45,
                        },
                        startX : -0.21,
                        endX : -0.19,
                    },
                    FunctionPiece : {
                        functionCurve : ConstantFunctionCurve : {
                            value : 0.45,
                        },
                        startX : -0.16,
                        endX : -0.14,
                    },
                    FunctionPiece : {
                        functionCurve : ConstantFunctionCurve : {
                            value : 0.45,
                        },
                        startX : -0.11,
                        endX : -0.09,
                    },
                    FunctionPiece : {
                        functionCurve : ConstantFunctionCurve : {
                            value : 0.45,
                        },
                        startX : -0.06,
                        endX : -0.04,
                    },
                    FunctionPiece : {
                        functionCurve : CubicHermiteSpline : {
                            startPoint : Vector2 : {
                                x : 0.0,
                                y : 0.6,
                            },
                            endPoint : Vector2 : {
                                x : 0.3,
                                y : 0.0,
                            },
                            endWeight : 0.0,
                        },
                        startX : -0.01,
                        endX : (1.0/0.0),
                    },
                },
            },
        },
        string type = "效果",
        int order = 2000,
        string displayName = "内环不透明度",
        string information = "标注坐标",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ alpha) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat alpha = new ^alpha();

    [
        auto defaultValue = VariableFloat : {
            baseValue : 0.0,
            variationCurve : CubicHermiteSpline : {
                startPoint : Vector2 : {
                    x : -3,
                    y : 0.0,
                },
                endPoint : Vector2 : {
                    x : -2.7,
                    y : 1,
                },
                endWeight : 0.0,
            }
        },
        string type = "效果",
        int order = 2001,
        string displayName = "总体不透明度",
        string information = "标注坐标",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ totalAlpha) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat totalAlpha = new ^totalAlpha();

    // 时变字段
    float localTime;
    float nowOuterCircleSize;
    float nowInnerCircleSize;
    float nowAlpha;
    float nowTotalAlpha;

    // 存储字段
    Tracker tracker;
    float trackerHitTime;
    Channel channel;
    Sprite ringSprite;
    Sprite circleSprite;
    Sprite innerCircleSprite;

    BeeBooNote() : super()
    {
        tracker = (Tracker) Environment.FindAliveLane("BeeBoo.Tracker", trackerId);
        if (tracker == null)
        {
            return null;
        }

        trackerHitTime = hitTime - tracker.startTime;

        channel = (Channel) Environment.FindAliveLane("BeeBoo.Channel", channelId);
        if (channel == null)
        {
            return null;
        }

        ImageAsset noteRingImage = (ImageAsset) Environment.GetAssetByName("image:BeeBoo/FormAsset/NoteRing");
        ringSprite = new Sprite(noteRingImage.texture);
        ringSprite.position.z = -2.9;
        ringSprite.color = new ColorArgb(1,1,1,1);

        ImageAsset noteCircleImage = (ImageAsset) Environment.GetAssetByName("image:BeeBoo/FormAsset/NoteCircle");
        circleSprite = new Sprite(noteCircleImage.texture);
        circleSprite.position.z = -2.8 + (hitTime / 30000);

        ImageAsset innerCircleImage = (ImageAsset) Environment.GetAssetByName("image:BeeBoo/FormAsset/Circle");
        innerCircleSprite = new Sprite(innerCircleImage.texture);
        innerCircleSprite.position.z = -2.85;
        innerCircleSprite.color = new ColorArgb(0.3,1,1,1);
    }

    @PeriodModifier
    static void PeriodModifier(BeeBooNote^ noteInjector, PeriodConfig periodConfig)
    {
        noteInjector.^hitTime = noteInjector.^hitTime + periodConfig.timeOffset;
    }

    void ReInjectBeeBooNote(BeeBooNote^ newInjector)
    {
        trackerId = newInjector.^trackerId;
        channelId = newInjector.^channelId;
        hitTime = newInjector.^hitTime;
        leadTime = newInjector.^leadTime;
        lagTime = newInjector.^lagTime;
        outerCircleSize = new (newInjector.^outerCircleSize)();
        innerCircleSize = new (newInjector.^innerCircleSize)();
        alpha = new (newInjector.^alpha)();
        totalAlpha = new (newInjector.^totalAlpha)();
    }
}