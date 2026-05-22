using Gorge;
using GorgeFramework;
namespace BeeBoo;

class TapTsiga
{
    TimeStack timeStack;
    InputGraph inputGraph;
    HistoryStack historyStack;
    
    TapTsiga(Tap note, bool isReverse, bool isAutoPlay)
    {
        historyStack = new HistoryStack();
        
        Channel channel = note.channel;
        
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

        string signalChannelName = "Channel" + note.channelId;
        
        // 第一阶段 -MissStart，无视任何输入（不过滤任何输入，不消耗）直到时间结束，接收或拒绝都进入下一输入
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
                    return missStart;
                },
                TimeMode.CatchBefore,
                false,
                false
            ),
            new InputGraphEdge(false, 1, null, false, false, false, null),
            new InputGraphEdge(false, 1, null, false, false, false, null)
        );
        
        // 第二阶段 MissStart-GreatStart，时间内捕获Begin，消耗，接收进入失败，拒绝进入下一输入
        // 处理早点产生的miss
        InputGraphState state1 = new InputGraphState(
            new FloatSignalFilter(
                signalChannelName,
                Priority[]:() ->
                {
                    Priority[] priorities = new Priority[1];
                    priorities[0] = new Priority(float:(ISignal signal) -> { return note.hitTime; });
                    return priorities;
                },
                new FloatSignalConditionType[1]{FloatSignalConditionType.In},
                bool:(FloatSignal signal) ->
                {
                    return signal.value == 1;
                },
                float:() ->
                {
                    return goodStart;
                },
                TimeMode.CatchBefore,
                true,
                false
            ),
            new InputGraphEdge(true, 0, null, false, false, false, null),
            new InputGraphEdge(false, 1, null, false, false, false, null)
        );
        
        // 第三阶段 GreatStart-GreatEnd，时间内捕获Begin，消耗，接收进入成功，拒绝进入失败
        // 非Miss点击
        InputGraphState state2 = new InputGraphState(
            new FloatSignalFilter(
                signalChannelName,
                Priority[]:() ->
                {
                    Priority[] priorities = new Priority[1];
                    priorities[0] = new Priority(float:(ISignal signal) -> { return note.hitTime; });
                    return priorities;
                },
                new FloatSignalConditionType[1]{FloatSignalConditionType.In},
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
                false
            ),
            new InputGraphEdge(false, 1, null, true, false, true, "Accepted"),
            new InputGraphEdge(true, 0, null, true, false, true, "Timeout")
        );
        
        InputGraphState[] states = new InputGraphState[3];
        states[0] = state0;
        states[1] = state1;
        states[2] = state2;
        
        inputGraph = new InputGraph(states, false, false, 0, "Waiting");
        
        timeStack = new TimeStack(false, null);

        if(isAutoPlay)
        {
            timeStack = new TimeStack(false, "BestPerfect");
        }
        else
        {
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
            
            // BestPerfect窗口到来，响应变为BestPerfect
            timeStack.InitPush(
                new TimeItem(
                    float:() ->
                    {
                        return bestPerfectStart;
                    },
                    true,
                    "BestPerfect"
                )
            );
            
            // Perfect窗口到来，响应变为Perfect
            timeStack.InitPush(
                new TimeItem(
                    float:() ->
                    {
                        return perfectStart;
                    },
                    true,
                    "Perfect"
                )
            );
            
            // Good窗口到来，响应变为Good
            timeStack.InitPush(
                new TimeItem(
                    float:() ->
                    {
                        return goodStart;
                    },
                    true,
                    "Good"
                )
            );
        }
        


        if(isReverse)
        {
            inputGraph = new InputGraph(states, false, false, -1, "Denied");
            timeStack = new TimeStack(false, null);
        }
    }
}