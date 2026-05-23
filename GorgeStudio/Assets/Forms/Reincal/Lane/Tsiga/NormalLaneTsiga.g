using Gorge;
using GorgeFramework;
namespace Reincal;

class NormalLaneTsiga
{
    TimeStack timeStack;
    InputGraph inputGraph;
    HistoryStack historyStack;
    
    NormalLaneTsiga(NormalLane lane, bool isReverse)
    {
        historyStack = new HistoryStack();
        timeStack = new TimeStack(false, null);
        
        // 第一阶段 无点击，识别点击
        InputGraphState state0 = new InputGraphState(
            new InputSignalFilter(
                Priority[]:() ->
                {
                    Priority[] priorities = new Priority[1];
                    priorities[0] = new Priority(float:(ISignal signal) -> { return lane.GetAimDistance((TouchSignal) signal); });
                    return priorities;
                },
                void:(int signalId, TouchSignal signal)->
                {
                    lane.handlingSignalId = signalId;
                    lane.set = false;
                    return;
                },
                new TouchType[1]{TouchType.Keep},
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
                    }
                    return isInTouchArea;
                },
                float:() ->
                {
                    return Math.FloatPositiveInfinity();
                },
                TimeMode.CatchBefore,
                true,
                true
            ),
            new InputGraphEdge(false, 1, null, false, false, true, null),
            new InputGraphEdge(false, 1, null, false, false, true, null)
        );
        
        // 第二阶段 已点击，识别释放
        InputGraphState state1 = new InputGraphState(
            new InputSignalFilter(
                Priority[]:() ->
                {
                    Priority[] priorities = new Priority[1];
                    priorities[0] = new Priority(float:(ISignal signal) -> { return lane.GetAimDistance((TouchSignal) signal); });
                    return priorities;
                },
                void:(int signalId, TouchSignal signal)->
                {
                    lane.set = true;
                    return;
                },
                new TouchType[1]{TouchType.Keep},
                bool:(int signalId)->
                {
                    return true;
                },
                bool:(TouchSignal signal) ->
                {
                    return lane.IsInTouchArea(signal);
                },
                float:() ->
                {
                    return Math.FloatPositiveInfinity();
                },
                TimeMode.KeepUntil,
                true,
                true
            ),
            new InputGraphEdge(false, -1, null, false, false, true, null),
            new InputGraphEdge(false, -1, null, false, false, true, null)
        );
        
        InputGraphState[] states = new InputGraphState[2];
        states[0] = state0;
        states[1] = state1;
        
        inputGraph = new InputGraph(states, false, false, 0, "Active");
        
        
    }
}