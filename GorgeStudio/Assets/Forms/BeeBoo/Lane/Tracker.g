using Gorge;
using GorgeFramework;
namespace BeeBoo;

[
    delegate<float:Tracker^> display = string:(Tracker^ laneInjector) ->
    {
        return "追踪器 #" + laneInjector.^id;
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 0.2396693, g : 0.6370158, b : 0.8},
    delegate<ElementLine:Tracker^> elementLine = ElementLine:(Tracker^ lane) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[2];
        points[0] = new ElementLinePoint(lane.^startTime, 0.5, 0.8);
        points[1] = new ElementLinePoint(lane.^startTime + lane.^keepTime, 0.5, 0.8);
        return new ElementLine(new ColorArgb(0.3, 1, 1, 1), points);
    },
    string displayName = "追踪器"
]
@EditableElement(type = "轨道", editUpdateMode = EditUpdateMode.ReInject)
class Tracker : Element
{
    // 注入字段

    [
        string type = "基本",
        int order = 0,
        string displayName = "追踪器编号",
        string information = "不能为空",
        delegate<bool:int> check = bool:(int id) -> { return true; }
    ]
    @Inject
    int id = ^id;

    [
        auto defaultValue = 0.0,
        string type = "生命周期",
        int order = 1000,
        string displayName = "生成时刻",
        string information = "单位秒，乐段相对时间",
        delegate<bool:float> check = bool:(float generateTime) -> { return true; },
        string timePointName = "GenerateTime",
        string timePointEarlyAnchor = null,
        string timePointLateAnchor = null
    ]
    @Inject
    float startTime = ^startTime;

    [
        auto defaultValue = 10,
        string type = "生命周期",
        int order = 1001,
        string displayName = "保持时长",
        string information = "单位秒，>0",
        delegate<bool:float> check = bool:(float keepTime) -> { return keepTime > 0; },
        string timePointName = "DestroyTime",
        string timePointEarlyAnchor = "GenerateTime",
        string timePointLateAnchor = null
    ]
    @Inject
    float keepTime = ^keepTime;

    [
        string type = "基本",
        int order = 1,
        string displayName = "路径",
        string information = "不能为空",
        delegate<bool:TrackerPath^[]^> check = bool:(TrackerPath^[]^ loopPath) -> { return true; }
    ]
    @Inject<TrackerPath^[]^>
    TrackerPath^[] loopPath = (^loopPath == null) ? null : (new (^loopPath)[^loopPath.length]);
    
    [
        auto defaultValue = VariableFloat : {baseValue : 1},
        string type = "效果",
        int order = 100,
        string displayName = "不透明度",
        string information = "本地时间，轨道曲线坐标",
        delegate<bool:VariableFloat^> check = bool:(VariableFloat^ alpha) -> { return true; }
    ]
    @Inject<VariableFloat^>
    VariableFloat alpha = new ^alpha();

    // 构造字段
    TrackerPath[] loopPathConfigs;

    // 时变字段
    float localTime;
    float nowAlpha;

    // 存储字段
    Sprite trackerSprite;

    [
        delegate<float:Tracker^> time = float:(Tracker^ laneInjector) ->
        {
            return laneInjector.^startTime;
        }
    ]
    @ForwardTimedGenerate
    [
        delegate<float:Tracker^> time = float:(Tracker^ laneInjector) ->
        {
            return laneInjector.^startTime + laneInjector.^keepTime;
        }
    ]
    @BackwardTimedGenerate
    @EditGenerate
    Tracker()
    {
        if (loopPath == null)
        {
            loopPathConfigs = null;
        }
        else
        {
            loopPathConfigs = new TrackerPath[loopPath.length];
            for (int i = 0; i < loopPath.length; i = i + 1)
            {
                if (loopPath[i] == null)
                {
                    loopPathConfigs[i] = null;
                }
                else
                {
                    loopPathConfigs[i] = new loopPath[i]();
                }
            }
        }

        ImageAsset trackerImage = (ImageAsset) Environment.GetAssetByName("image:BeeBoo/FormAsset/Tracker1");
        trackerSprite = new Sprite(trackerImage.texture);
        trackerSprite.size.x = 0.7;
        trackerSprite.size.y = 0.7;
        trackerSprite.position.z = -3;

        nodes = new Node[1];
        nodes[0] = trackerSprite;

        ITransformer[] transformers = new ITransformer[2];
        transformers[0] = new TrackerReInjectTransformer(this);
        transformers[1] = new TrackerTimeVaryingVariableTransformer(this);
        simulator = new ElementSimulator(transformers);

        ITransformer[] lateTransformers = new ITransformer[1];
        lateTransformers[0] = new TrackerPositionTransformer(this);
        lateIndependentSimulator = new ElementSimulator(lateTransformers);
    }

    @ForwardTimedDestroy
    float ForwardDestroyTime()
    {
        return startTime + keepTime;
    }
    
    @BackwardTimedDestroy
    float BackwardDestroyTime()
    {
        return startTime;
    }
    
    @EditTryGenerate
    static bool TryGenerate(Tracker^ newInjector, float chartTime)
    {
        return chartTime >= newInjector.^startTime && chartTime <= newInjector.^startTime + newInjector.^keepTime;
    }

    @PeriodModifier
    static void PeriodModifier(LaneSet^ noteInjector, PeriodConfig periodConfig)
    {
        noteInjector.^startTime = noteInjector.^startTime + periodConfig.timeOffset;
    }

    @EditReInject
    void ReInjectTracker(Tracker^ newInjector)
    {
        id = newInjector.^id;
        startTime = newInjector.^startTime;
        keepTime = newInjector.^keepTime;
        loopPath = (newInjector.^loopPath == null) ? null : (new (newInjector.^loopPath)[newInjector.^loopPath.length]);
        if (loopPath == null)
        {
            loopPathConfigs = null;
        }
        else
        {
            loopPathConfigs = new TrackerPath[loopPath.length];
            for (int i = 0; i < loopPath.length; i = i + 1)
            {
                if (loopPath[i] == null)
                {
                    loopPathConfigs[i] = null;
                }
                else
                {
                    loopPathConfigs[i] = new loopPath[i]();
                }
            }
        }
        alpha = new (newInjector.^alpha)();
    }

    Vector2 GetPosition(float trackerLocalTime)
    {
        float checkedPathTimeSum = 0;
        float pathStartTime = 0;
        TrackerPath targetPath;

        for(int i = 0; i < loopPathConfigs.length; i = i + 1)
        {
            pathStartTime = checkedPathTimeSum;
            targetPath = loopPathConfigs[i];
            float pathTime = targetPath.GetTime();
            if(pathTime + pathStartTime >= trackerLocalTime)
            {
                break for;
            }

            checkedPathTimeSum = checkedPathTimeSum + pathTime;
        }

        if(targetPath == null)
        {
            return null;
        }

        return targetPath.GetPosition(trackerLocalTime - pathStartTime);
    }

    Vector2 GetPosition(float trackerLocalTime, float tolerance)
    {
        float checkedPathTimeSum = 0;
        float pathStartTime = 0;
        TrackerPath targetPath;
        float checkTime = trackerLocalTime;

        for(int i = 0; i < loopPathConfigs.length; i = i + 1)
        {
            pathStartTime = checkedPathTimeSum;
            targetPath = loopPathConfigs[i];
            float pathTime = targetPath.GetTime();
            float targetTime = pathTime + pathStartTime;
            if(Math.Abs(targetTime - trackerLocalTime) < tolerance)
            {
                checkTime = targetTime;
                break for;
            }

            if(targetTime >= trackerLocalTime)
            {
                break for;
            }

            checkedPathTimeSum = checkedPathTimeSum + pathTime;
        }

        if(targetPath == null)
        {
            return null;
        }

        return targetPath.GetPosition(checkTime - pathStartTime);
    }
}