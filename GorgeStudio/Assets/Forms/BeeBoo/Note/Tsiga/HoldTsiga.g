using Gorge;
using GorgeFramework;
namespace BeeBoo;

class HoldTsiga
{
    TimeStack timeStack;
    InputGraph inputGraph;
    HistoryStack historyStack;
    
    HoldTsiga(Hold note, bool isReverse, bool isAutoPlay)
    {
        historyStack = new HistoryStack();
        
        delegate<void:Note,TimeStack,float,HistoryStack> pushInnerRespondTimeItem =
            void:(Note holdNote, TimeStack timeStack, float chartTime, HistoryStack historyStack) ->
            {
                Hold note = (Hold) holdNote;
                
                // 垫一个不会被弹出的时间栈项，从而屏蔽预置的Tap判定结果表
                timeStack.Push(
                    chartTime,
                    new TimeItem(
                        float:() ->
                        {
                            return Math.FloatPositiveInfinity();
                        },
                        true,
                        null
                    ),
                    historyStack
                );
                
                if (note.innerNotes == null)
                {
                    return;
                }
                
                // 中间响应倒序压栈
                for (int i = note.innerNotes.length - 1; i >= 0; i = i - 1)
                {
                    int index = i;
                    timeStack.Push(
                        chartTime,
                        new TimeItem(
                            float:() ->
                            {
                                return note.hitTime + note.innerNotes[i];
                            },
                            true,
                            "BestPerfect"
                        ),
                        historyStack
                    );
                }
            };
        
        Channel channel = note.channel;
        
        float missStart = note.hitTime - 0.24;
        float goodStart = note.hitTime - 0.18;
        float perfectStart = note.hitTime - 0.12;
        float bestPerfectStart = note.hitTime - 0.06;
        float bestPerfectEnd = note.hitTime + 0.06;
        float perfectEnd = note.hitTime + 0.12;
        float goodEnd = note.hitTime + 0.18;
        float missEnd = note.hitTime + 0.24;
        float holdEndGreatStart = note.hitTime + note.holdTime - 0.18;
        float holdEnd = note.hitTime + note.holdTime;
        float range = 0.2;

        string signalChannelName = "Channel" + note.channelId;
        
        // 第一阶段 -hitTime，无视任何输入（不过滤任何输入，不消耗）直到时间结束，接收或拒绝都进入下一输入
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
                    return note.hitTime;
                },
                TimeMode.CatchBefore,
                false,
                false
            ),
            new InputGraphEdge(false, 1, null, false, false, false, null),
            new InputGraphEdge(false, 1, null, false, false, false, null)
        );
        
        // 第二阶段 hitTime-goodEnd，时间内捕获Keep，拒绝进入失败，接受进入保持阶段
        // 保持起步
        InputGraphState state1 = new InputGraphState(
            new FloatSignalFilter(
                signalChannelName,
                Priority[]:() ->
                {
                    Priority[] priorities = new Priority[1];
                    priorities[0] = new Priority(float:(ISignal signal) -> { return note.hitTime; });
                    return priorities;
                },
                new FloatSignalConditionType[1]{FloatSignalConditionType.Keep},
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
            new InputGraphEdge(false, 1, pushInnerRespondTimeItem, false, true, true, "Holding"),
            new InputGraphEdge(true, 0, null, false, false, false, null)
        );
        
        // 第三阶段 -HoldEndGreatStart，时间内捕获Keep，消耗，接收进入成功，拒绝进入失败
        // 保持
        InputGraphState state2 = new InputGraphState(
            new FloatSignalFilter(
                signalChannelName,
                Priority[]:() ->
                {
                    Priority[] priorities = new Priority[1];
                    priorities[0] = new Priority(float:(ISignal signal) -> { return note.hitTime; });
                    return priorities;
                },
                new FloatSignalConditionType[1]{FloatSignalConditionType.Keep},
                bool:(FloatSignal signal) ->
                {
                    return signal.value == 1;
                },
                float:() ->
                {
                    return holdEndGreatStart;
                },
                TimeMode.KeepUntil,
                false,
                false
            ),
            new InputGraphEdge(false, 1, null, false, true, false, null),
            new InputGraphEdge(true, 0, null, true, false, true, "Timeout")
        );
        
        // 第四阶段 HoldEndGreatStart - HoldEnd，时间内不捕获任何操作
        // 已完成Hold，延时到HoldTime完成后进入Accepted状态
        InputGraphState state3 = new InputGraphState(
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
                    return holdEnd;
                },
                TimeMode.CatchBefore,
                false,
                false
            ),
            new InputGraphEdge(false, 1, null, true, true, false, "Accepted"),
            new InputGraphEdge(false, 1, null, true, true, false, "Accepted")
        );
        
        InputGraphState[] states = new InputGraphState[4];
        states[0] = state0;
        states[1] = state1;
        states[2] = state2;
        states[3] = state3;
        
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
        
        // 暂不考虑反转
        if(isReverse)
        {
            inputGraph = new InputGraph(states, false, false, -1, "Denied");
            timeStack = new TimeStack(false, null);
        }

        /*
            反转情况对应正转路径
            MissStart : 0->1
            GoodStart : Good弹栈
            GoodStart : 1->2 deny
            PerfectStart : Perfect弹栈
            BestPerfectStart : BestPerfect弹栈
            RespondMoment : 2->3
            Accept
        */
        /*
        if(isReverse)
        {
            DeentyLane lane = (DeentyLane)Environment.FindAliveLane(note.laneType, note.laneName);
            inputGraph.GoAcceptEdge(lane.MissRespondStartMoment(note.respondMoment), historyStack);
            timeStack.Pop(lane.GoodRespondStartMoment(note.respondMoment), historyStack);
            inputGraph.GoDenyEdge(lane.GoodRespondStartMoment(note.respondMoment), historyStack);
            timeStack.Pop(lane.PerfectRespondStartMoment(note.respondMoment), historyStack);
            timeStack.Pop(lane.BestPerfectRespondStartMoment(note.respondMoment), historyStack);
            inputGraph.GoAcceptEdge(note.respondMoment, historyStack);
        }
        */
    }
}