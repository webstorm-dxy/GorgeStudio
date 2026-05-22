using Gorge;
using GorgeFramework;
namespace BeeBoo;

[
    delegate<float:FreeLaneSet^> display = string:(FreeLaneSet^ laneInjector) ->
    {
        return "自由轨道组 #" + laneInjector.^id;
    },
    ColorArgb^ color = ColorArgb : {a : 1.0, r : 0.2396693, g : 0.6370158, b : 0.8},
    delegate<ElementLine:FreeLaneSet^> elementLine = ElementLine:(FreeLaneSet^ lane) ->
    {
        ElementLinePoint[] points = new ElementLinePoint[2];
        points[0] = new ElementLinePoint(lane.^startTime, 0.5, 0.8);
        points[1] = new ElementLinePoint(lane.^startTime + lane.^keepTime, 0.5, 0.8);
        return new ElementLine(new ColorArgb(0.3, 1, 1, 1), points);
    },
    string displayName = "自由轨道组"
]
@EditableElement(type = "轨道", editUpdateMode = EditUpdateMode.ReInject)
class FreeLaneSet : LaneSet
{
    // 注入字段
    [
        auto defaultValue = Lane^ : { Lane : {:}},
        string type = "基本",
        int order = 2,
        string displayName = "组内轨道",
        string information = "组内轨道标",
        delegate<bool:Lane^[]^> check = bool:(Lane^[]^ laneInjectors) -> { return true; }
    ]
    @Inject<Lane^[]^>
    Lane^[] laneInjectors = (^laneInjectors == null) ? null : (new (^laneInjectors)[^laneInjectors.length]);

    // 时变字段

    // 存储字段

    [
        delegate<float:FreeLaneSet^> time = float:(FreeLaneSet^ laneInjector) ->
        {
            return laneInjector.^startTime;
        }
    ]
    @ForwardTimedGenerate
    [
        delegate<float:FreeLaneSet^> time = float:(FreeLaneSet^ laneInjector) ->
        {
            return laneInjector.^startTime + laneInjector.^keepTime;
        }
    ]
    @BackwardTimedGenerate
    @EditGenerate
    FreeLaneSet() : super()
    {
        if (laneInjectors == null)
        {
            lanes = new Lane[0];
        }
        else
        {
            lanes = new Lane[laneInjectors.length];
            derivedElements = new Element[laneInjectors.length];
            for (int i = 0; i < laneInjectors.length; i = i + 1)
            {
                if (laneInjectors[i] == null)
                {
                    lanes[i] = null;
                }
                else
                {
                    lanes[i] = new laneInjectors[i](startTime, keepTime, baseNode);
                }
                derivedElements[i] = lanes[i];
            }
        }

        ITransformer[] transformers = new ITransformer[2];
        transformers[0] = new LaneSetTimeVaryingVariableTransformer(this);
        
        transformers[1] = new LaneSetPositionTransformer(this);
        simulator = new ElementSimulator(transformers);

        ITransformer[] lateTransformers = new ITransformer[0];
        
        lateIndependentSimulator = new ElementSimulator(lateTransformers);
    }

    Lane GetLane(int laneIndex)
    {
        if(laneIndex >= lanes.length || laneIndex < 0)
        {
            return null;
        }

        return lanes[laneIndex];
    }

    @EditTryGenerate
    static bool TryGenerate(FreeLaneSet^ newInjector, float chartTime)
    {
        return chartTime >= newInjector.^startTime && chartTime <= newInjector.^startTime + newInjector.^keepTime;
    }

    @EditReInject
    IAutomatonCommand[] ReInjectFreeLaneSet(FreeLaneSet^ newInjector)
    {
        ReInjectLaneSet(newInjector);
        laneInjectors = (newInjector.^laneInjectors == null) ? null : (new (newInjector.^laneInjectors)[newInjector.^laneInjectors.length]);

        IAutomatonCommand[] commands = new IAutomatonCommand[laneInjectors.length + derivedElements.length];

        if (laneInjectors == null)
        {
            lanes = new Lane[0];
        }
        else
        {
            lanes = new Lane[laneInjectors.length];
            int commandPointer = 0;
            for (int i = 0; i < derivedElements.length; i = i + 1)
            {
                commands[commandPointer] = new DestroyElementCommand(derivedElements[i], false);
                commandPointer = commandPointer + 1;
            }
            int startPointer = derivedElements.length;
            derivedElements = new Element[laneInjectors.length];
            for (int j = 0; j < laneInjectors.length; j = j + 1)
            {
                if (laneInjectors[j] == null)
                {
                    lanes[j] = null;
                }
                else
                {
                    lanes[j] = new laneInjectors[j](startTime, keepTime, baseNode);
                }
                commands[commandPointer] = new DeriveElementCommand(lanes[j], false);
                derivedElements[j] = lanes[j];
                commandPointer = commandPointer + 1;
            }
        }

        return commands;
    }
}