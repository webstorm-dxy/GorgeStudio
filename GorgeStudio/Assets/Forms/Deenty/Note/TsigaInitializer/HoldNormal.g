using Gorge;
using GorgeFramework;
namespace Deenty;

class HoldNormal
{
    TimeStack timeStack;
    InputGraph inputGraph;
    HistoryStack historyStack;
    
    HoldNormal(Hold note, bool isReverse)
    {
        historyStack = new HistoryStack();
        
        delegate<void:Note,TimeStack,float,HistoryStack> pushInnerRespondTimeItem =
            void:(Note holdNote, TimeStack timeStack, float chartTime, HistoryStack historyStack) ->
            {
                Hold note = (Hold) holdNote;
                // 最后响应压栈
                timeStack.Push(
                    chartTime,
                    new TimeItem(
                        float:() ->
                        {
                            return note.respondMoment + note.holdTime;
                        },
                        true,
                        "MutedBestPerfect"
                    ),
                    historyStack
                );
                
                // 中间响应倒序压栈
                for (int i = 0; i > 0; i = i - 1)
                {
                    int index = i;
                    timeStack.Push(
                        chartTime,
                        new TimeItem(
                            float:() ->
                            {
                                return note.respondMoment + index * (note.holdTime / (note.respondQuantity - 1));
                            },
                            false,
                            "MutedBestPerfect"
                        ),
                        historyStack
                    );
                }
            };
        
        // 第一阶段 ~GoodStart，无视任何输入（不过滤任何输入，过滤命中拒绝，不消耗）直到时间结束，
        InputGraphState state0 = new InputGraphState(
            new InputSignalFilter(
                Priority[]:() ->
                {
                    Priority[] priorities = new Priority[2];
                    priorities[0] = new Priority(float:(ISignal signal) -> { return note.respondMoment; });
                    priorities[1] = new Priority(float:(ISignal signal) -> { return note.respondArea.BestDistance((TouchSignal) signal); });
                    return priorities;
                },
                null,
                new TouchType[0]{,},
                bool:(int signalId)->
                {
                    return true;
                },
                bool:(TouchSignal signal) ->
                {
                    return false;
                },
                float:() ->
                {
                    DeentyLane lane = (DeentyLane) Environment.FindAliveLane(note.laneType, note.laneName);
                    return lane.GoodRespondStartMoment(note.respondMoment);
                },
                TimeMode.CatchBefore,
                false,
                false
            ),
            new InputGraphEdge(false, 1, null, false, false, false, null),
            new InputGraphEdge(false, 1, null, false, false, false, null)
        );
        
        // 第二阶段 GoodStart~RespondMoment，时间内捕获Begin，消耗，接收进入长按阶段，拒绝进入非Begin起手阶段
        InputGraphState state1 = new InputGraphState(
            new InputSignalFilter(
                Priority[]:() ->
                {
                    Priority[] priorities = new Priority[2];
                    priorities[0] = new Priority(float:(ISignal signal) -> { return note.respondMoment; });
                    priorities[1] = new Priority(float:(ISignal signal) -> { return note.respondArea.BestDistance((TouchSignal) signal); });
                    return priorities;
                },
                null,
                new TouchType[1]{TouchType.Begin},
                bool:(int signalId)->
                {
                    return true;
                },
                bool:(TouchSignal signal) ->
                {
                    return note.respondArea.IsInRespondArea(signal);
                },
                float:() ->
                {
                    return note.respondMoment;
                },
                TimeMode.CatchBefore,
                true,
                false
            ),
            new InputGraphEdge(false, 2, pushInnerRespondTimeItem, false, true, true, "Holding"),
            new InputGraphEdge(false, 1, null, false, false, false, null)
        );
        
        // 第三阶段 RespondMoment~GoodEnd，时间内捕获Keep，消耗，接收进入长按阶段，拒绝则失败
        InputGraphState state2 = new InputGraphState(
            new InputSignalFilter(
                Priority[]:() ->
                {
                    Priority[] priorities = new Priority[2];
                    priorities[0] = new Priority(float:(ISignal signal) -> { return note.respondMoment; });
                    priorities[1] = new Priority(float:(ISignal signal) -> { return note.respondArea.BestDistance((TouchSignal) signal); });
                    return priorities;
                },
                null,
                new TouchType[1]{TouchType.Keep},
                bool:(int signalId)->
                {
                    return true;
                },
                bool:(TouchSignal signal) ->
                {
                    return note.respondArea.IsInRespondArea(signal);
                },
                float:() ->
                {
                    DeentyLane lane = (DeentyLane) Environment.FindAliveLane(note.laneType, note.laneName);
                    return lane.GoodRespondEndMoment(note.respondMoment);
                },
                TimeMode.CatchBefore,
                true,
                false
            ),
            new InputGraphEdge(false, 1, pushInnerRespondTimeItem, false, true, true, "Holding"),
            new InputGraphEdge(true, 0, null, true, false, false, null)
        );
        
        // 第四阶段 ~FinishMoment-GoodHalf，时间内捕获Keep，消耗，接收进结束，拒绝失败
        InputGraphState state3 = new InputGraphState(
            new InputSignalFilter(
                Priority[]:() ->
                {
                    Priority[] priorities = new Priority[2];
                    priorities[0] = new Priority(float:(ISignal signal) -> { return note.respondMoment; });
                    priorities[1] = new Priority(float:(ISignal signal) -> { return note.respondArea.BestDistance((TouchSignal) signal); });
                    return priorities;
                },
                null,
                new TouchType[1]{TouchType.Keep},
                bool:(int signalId)->
                {
                    return true;
                },
                bool:(TouchSignal signal) ->
                {
                    return note.respondArea.IsInRespondArea(signal);
                },
                float:() ->
                {
                    DeentyLane lane = (DeentyLane) Environment.FindAliveLane(note.laneType, note.laneName);
                    return note.respondMoment + note.holdTime - lane.goodTimeHalfInterval;
                },
                TimeMode.KeepUntil,
                true,
                false
            ),
            new InputGraphEdge(false, 1, null, true, false, true, "Accepted"),
            new InputGraphEdge(true, 0, null, true, false, false, null)
        );
        
        InputGraphState[] states = new InputGraphState[4];
        states[0] = state0;
        states[1] = state1;
        states[2] = state2;
        states[3] = state3;
        
        inputGraph = new InputGraph(states, false, false, 0, "Waiting");
        
        timeStack = new TimeStack(false, "BestPerfect");
        
        // Miss由输入图实现
        // 原始输入占只处理第一次响应，长条内响应由stackAction压栈
        // Perfect窗口过后，响应变为Good
        timeStack.InitPush(
            new TimeItem(
                float:() ->
                {
                    DeentyLane lane = (DeentyLane) Environment.FindAliveLane(note.laneType, note.laneName);
                    return lane.PerfectRespondEndMoment(note.respondMoment);
                },
                false,
                "Good"
            )
        );
        
        // BestPerfect窗口过后，响应变为Perfect
        timeStack.InitPush(
            new TimeItem(
                float:() ->
                {
                    DeentyLane lane = (DeentyLane) Environment.FindAliveLane(note.laneType, note.laneName);
                    return lane.BestPerfectRespondEndMoment(note.respondMoment);
                },
                false,
                "Perfect"
            )
        );
        
        // Hold第一次响应必然是BestPerfect，所以Good窗口开始后，响应变为BestPerfect
        timeStack.InitPush(
            new TimeItem(
                float:() ->
                {
                    DeentyLane lane = (DeentyLane) Environment.FindAliveLane(note.laneType, note.laneName);
                    return lane.GoodRespondStartMoment(note.respondMoment);
                },
                false,
                "BestPerfect"
            )
        );
        
        /*
            反转情况对应正转路径
            GoodStart : 0->1
            RespondMoment : BestPerfect弹栈
            RespondMoment : 1->2 Deny
            RespondMoment : 2->3 Accept
              含压栈动作
            中间节点 : MutedBestPerfect 陆续弹栈
              穿插FinishMoment-GoodHalf : 3->4 Accept
            FinishMoment : MutedBestPerfect弹栈
            Accept
        */
        if (isReverse)
        {
            DeentyLane lane = (DeentyLane) Environment.FindAliveLane(note.laneType, note.laneName);
            inputGraph.GoAcceptEdge(lane.GoodRespondStartMoment(note.respondMoment), historyStack);
            timeStack.Pop(note.respondMoment, historyStack);
            inputGraph.GoDenyEdge(note.respondMoment, historyStack);
            InputGraphEdge edge = inputGraph.GoAcceptEdge(note.respondMoment, historyStack);
            edge.stackAction(note, timeStack, note.respondMoment, historyStack);
            
            float stateFinishTime = note.respondMoment + note.holdTime - lane.goodTimeHalfInterval;
            bool stateFinishFlag = false; // 是否添加过3->4转移
            for (int i = 1; i < note.respondQuantity - 1; i = i + 1)
            {
                int index = i;
                float time = note.respondMoment + index * (note.holdTime / (note.respondQuantity - 1));
                if (time > stateFinishTime && !stateFinishFlag)
                {
                    inputGraph.GoAcceptEdge(stateFinishTime, historyStack);
                    stateFinishFlag = true;
                }
                timeStack.Pop(time, historyStack);
            }
            
            timeStack.Pop(note.respondMoment + note.holdTime, historyStack);
        }
    }
}