using Gorge;
using GorgeFramework;
namespace Reincal;

class CatchTsiga
{
    TimeStack timeStack;
    InputGraph inputGraph;
    HistoryStack historyStack;
    
    CatchTsiga(Catch note, bool isReverse)
    {
        historyStack = new HistoryStack();
        
        NormalLane lane = note.lane;
        
        float hitTime = note.hitTime;
        float missStart = note.hitTime - 0.24;
        float goodStart = note.hitTime - 0.18;
        float perfectStart = note.hitTime - 0.12;
        float bestPerfectStart = note.hitTime - 0.06;
        float bestPerfectEnd = note.hitTime + 0.06;
        float perfectEnd = note.hitTime + 0.12;
        float goodEnd = note.hitTime + 0.18;
        float missEnd = note.hitTime + 0.24;
        float range = 0.2;

        string signalChannelName = "NormalLane" + lane.id;
        
        // 第一阶段 -GoodStart，无视任何输入（不过滤任何输入，过滤命中拒绝，不消耗）直到时间结束，
        InputGraphState state0 = new InputGraphState(
            new FloatSignalFilter(
                signalChannelName,
                Priority[]:() ->
                {
                    return new Priority[0];
                },
                new FloatSignalConditionType[0],
                bool:(FloatSignal signal) ->
                {
                    return false;
                },
                float:() ->
                {
                    return goodStart;
                },
                TimeMode.CatchBefore,
                false,
                false
            ),
            new InputGraphEdge(false, 1, null, false, false, false, null),
            new InputGraphEdge(false, 1, null, false, false, false, null)
        );
        
        // 第二阶段 GoodStart-HitTime，时间内捕获Keep，消耗，接收进入成功等待，拒绝进入后等待
        // HitTime前等待划过
        InputGraphState state1 = new InputGraphState(
            new FloatSignalFilter(
                signalChannelName,
                Priority[]:() ->
                {
                    Priority[] priorities = new Priority[1];
                    priorities[0] = new Priority(float:(ISignal signal) -> { return note.hitTime; });
                    return priorities;
                },
                new FloatSignalConditionType[2]{FloatSignalConditionType.Keep, FloatSignalConditionType.In},
                bool:(FloatSignal signal) ->
                {
                    return signal.value == 1;
                },
                float:() ->
                {
                    return hitTime;
                },
                TimeMode.CatchBefore,
                true,
                true
            ),
            new InputGraphEdge(false, 1, null, false, false, false, null),
            new InputGraphEdge(false, 2, null, false, false, false, null)
        );
        
        // 第三阶段 -HitTime，HitTime前已经捕获则进入本状态，不捕获任何输入
        // HitTime前已经划过，等待HitTime自动响应
        InputGraphState state2 = new InputGraphState(
            new FloatSignalFilter(
                signalChannelName,
                Priority[]:() ->
                {
                    Priority[] priorities = new Priority[1];
                    priorities[0] = new Priority(float:(ISignal signal) -> { return note.hitTime; });
                    return priorities;
                },
                new FloatSignalConditionType[2]{FloatSignalConditionType.Keep, FloatSignalConditionType.In},
                bool:(FloatSignal signal) ->
                {
                    return false;
                },
                float:() ->
                {
                    return hitTime;
                },
                TimeMode.CatchBefore,
                false,
                false
            ),
            new InputGraphEdge(false, 2, null, true, true, true, "Accepted"),
            new InputGraphEdge(false, 2, null, true, true, true, "Accepted")
        );
        
        // 第四阶段 HitTime - GoodEnd，时间内捕获Keep，消耗，接收进入成功，拒绝进入失败
        // HitTime后等待划过
        InputGraphState state3 = new InputGraphState(
            new FloatSignalFilter(
                signalChannelName,
                Priority[]:() ->
                {
                    Priority[] priorities = new Priority[1];
                    priorities[0] = new Priority(float:(ISignal signal) -> { return note.hitTime; });
                    return priorities;
                },
                new FloatSignalConditionType[2]{FloatSignalConditionType.Keep, FloatSignalConditionType.In},
                bool:(FloatSignal signal) ->
                {
                    return signal.value == 1;
                },
                float:() ->
                {
                    return goodEnd;
                },
                TimeMode.CatchBefore,
                true,
                true
            ),
            new InputGraphEdge(false, 1, null, true, false, true, "Accepted"),
            new InputGraphEdge(true, 0, null, true, false, false, "Timeout")
        );
        
        InputGraphState[] states = new InputGraphState[4];
        states[0] = state0;
        states[1] = state1;
        states[2] = state2;
        states[3] = state3;
        
        inputGraph = new InputGraph(states, false, false, 0, "Waiting");
        
        timeStack = new TimeStack(false, null);
        
        // Miss由输入图实现
        // Perfect窗口过后，响应变为Good
        timeStack.InitPush(
            new TimeItem(
                float:() ->
                {
                    return perfectEnd;
                },
                true,
                "Good"
            )
        );
        
        // BestPerfect窗口过后，响应变为Perfect
        timeStack.InitPush(
            new TimeItem(
                float:() ->
                {
                    return bestPerfectEnd;
                },
                true,
                "Perfect"
            )
        );
        
        // HitTime过后，允许响应
        timeStack.InitPush(
            new TimeItem(
                float:() ->
                {
                    return note.hitTime;
                },
                true,
                "BestPerfect"
            )
        );
    }
}