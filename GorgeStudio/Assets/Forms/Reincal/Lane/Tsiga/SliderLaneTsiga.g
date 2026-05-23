using Gorge;
using GorgeFramework;
namespace Reincal;

class SliderLaneTsiga
{
    TimeStack timeStack;
    InputGraph inputGraph;
    HistoryStack historyStack;
    
    SliderLaneTsiga(ReincalSliderLane lane, bool isReverse)
    {
        historyStack = new HistoryStack();
        timeStack = new TimeStack(false, null);
        
        // 第一阶段 无点击，识别点击
        InputGraphState state0 = new InputGraphState(
            new InputSignalFilter(
                Priority[]:() ->
                {
                    Priority[] priorities = new Priority[1];
                    priorities[0] = new Priority(float:(ISignal signal) -> { return 0; });
                    return priorities;
                },
                void:(int signalId, TouchSignal signal)->
                {
                    lane.handlingSignalId = signalId;
                    return;
                },
                new TouchType[1]{TouchType.Begin},
                bool:(int signalId)->
                {
                    return true;
                },
                bool:(TouchSignal signal) ->
                {
                    bool isInTouchArea = lane.IsInTouchArea(signal);
                    if(isInTouchArea)
                    {
                        lane.touchSignalId = lane.handlingSignalId;
                        lane.baseAngle = lane.GetTouchAngle(signal);
                        lane.baseRotation = lane.rotation;
                    }
                    return isInTouchArea;
                },
                float:() ->
                {
                    return Math.FloatPositiveInfinity();
                },
                TimeMode.CatchBefore,
                false,
                false
            ),
            new InputGraphEdge(false, 1, null, false, false, false, null),
            new InputGraphEdge(false, 1, null, false, false, false, null)
        );
        
        // 第二阶段 已点击，拖动更新
        InputGraphState state1 = new InputGraphState(
            new InputSignalFilter(
                Priority[]:() ->
                {
                    Priority[] priorities = new Priority[1];
                    priorities[0] = new Priority(float:(ISignal signal) -> { return 0; });
                    return priorities;
                },
                void:(int signalId, TouchSignal signal)->
                {
                    if(signal == null || signalId != lane.touchSignalId)
                    {
                        return;
                    }
                    lane.nowAngle = lane.GetTouchAngle(signal);
                    lane.rotation = lane.baseRotation + (lane.nowAngle - lane.baseAngle) * 3.1415926 / 180;
                    return;
                },
                new TouchType[1]{TouchType.End},
                bool:(int signalId)->
                {
                    return signalId == lane.touchSignalId;
                },
                bool:(TouchSignal signal) ->
                {
                    return true;
                },
                float:() ->
                {
                    return Math.FloatPositiveInfinity();
                },
                TimeMode.CatchBefore,
                false,
                false
            ),
            new InputGraphEdge(false, -1, null, false, false, false, null),
            new InputGraphEdge(false, -1, null, false, false, false, null)
        );
        
        InputGraphState[] states = new InputGraphState[2];
        states[0] = state0;
        states[1] = state1;
        
        inputGraph = new InputGraph(states, false, false, 0, "Active");
        
        
    }
}