using Gorge;
using GorgeFramework;
namespace Deenty;

class TapNormal
{
    TimeStack timeStack;
    InputGraph inputGraph;
    HistoryStack historyStack;
    
    TapNormal(DeentyNote note, bool isReverse)
    {
        historyStack = new HistoryStack();
        
        // 第一阶段 -MissStart，无视任何输入（不过滤任何输入，不消耗）直到时间结束，接收或拒绝都进入下一输入
        InputGraphState state0 = new InputGraphState(
            new InputSignalFilter(
                Priority[]:() ->
                {
                    Priority[] priorities = new Priority[2];
                    priorities[0] = new Priority(float:(ISignal signal) -> { return note.respondMoment; });
                    priorities[1] = new Priority(float:(ISignal signal) -> { return note.respondArea.BestDistance((TouchSignal) signal); });
                    return priorities;
                },
                new TouchType[0]{,},
                bool:(TouchSignal signal) ->
                {
                    return false;
                },
                float:() ->
                {
                    DeentyLane lane = (DeentyLane) Environment.FindAliveLane(note.laneType, note.laneName);
                    return lane.MissRespondStartMoment(note.respondMoment);
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
            new InputSignalFilter(
                Priority[]:() ->
                {
                    Priority[] priorities = new Priority[2];
                    priorities[0] = new Priority(float:(ISignal signal) -> { return note.respondMoment; });
                    priorities[1] = new Priority(float:(ISignal signal) -> { return note.respondArea.BestDistance((TouchSignal) signal); });
                    return priorities;
                },
                new TouchType[1]{TouchType.Begin},
                bool:(TouchSignal signal) ->
                {
                    return note.respondArea.IsInRespondArea(signal);
                },
                float:() ->
                {
                    DeentyLane lane = (DeentyLane) Environment.FindAliveLane(note.laneType, note.laneName);
                    return lane.GoodRespondStartMoment(note.respondMoment);
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
            new InputSignalFilter(
                Priority[]:() ->
                {
                    Priority[] priorities = new Priority[2];
                    priorities[0] = new Priority(float:(ISignal signal) -> { return note.respondMoment; });
                    priorities[1] = new Priority(float:(ISignal signal) -> { return note.respondArea.BestDistance((TouchSignal) signal); });
                    return priorities;
                },
                new TouchType[1]{TouchType.Begin},
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
            new InputGraphEdge(false, 1, null, true, false, true, "Accepted"),
            new InputGraphEdge(true, 0, null, true, false, false, "Timeout")
        );
        
        InputGraphState[] states = new InputGraphState[3];
        states[0] = state0;
        states[1] = state1;
        states[2] = state2;
        
        inputGraph = new InputGraph(states, false, false, 0, "Waiting");
        
        timeStack = new TimeStack(false, "BestPerfect");
        
        // Miss由输入图实现
        // Perfect窗口过后，响应变为Good
        timeStack.InitPush(
            new TimeItem(
                float:() ->
                {
                    DeentyLane lane = (DeentyLane) Environment.FindAliveLane(note.laneType, note.laneName);
                    return lane.PerfectRespondEndMoment(note.respondMoment);
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
                    DeentyLane lane = (DeentyLane) Environment.FindAliveLane(note.laneType, note.laneName);
                    return lane.BestPerfectRespondEndMoment(note.respondMoment);
                },
                true,
                "Perfect"
            )
        );
        
        // BestPerfect窗口过后，响应变为BestPerfect
        timeStack.InitPush(
            new TimeItem(
                float:() ->
                {
                    DeentyLane lane = (DeentyLane) Environment.FindAliveLane(note.laneType, note.laneName);
                    return lane.BestPerfectRespondStartMoment(note.respondMoment);
                },
                true,
                "BestPerfect"
            )
        );
        
        // Perfect窗口过后，响应变为Perfect
        timeStack.InitPush(
            new TimeItem(
                float:() ->
                {
                    DeentyLane lane = (DeentyLane) Environment.FindAliveLane(note.laneType, note.laneName);
                    return lane.PerfectRespondStartMoment(note.respondMoment);
                },
                true,
                "Perfect"
            )
        );
        
        // Good窗口过后，响应变为Good
        timeStack.InitPush(
            new TimeItem(
                float:() ->
                {
                    DeentyLane lane = (DeentyLane) Environment.FindAliveLane(note.laneType, note.laneName);
                    return lane.GoodRespondStartMoment(note.respondMoment);
                },
                true,
                "Good"
            )
        );
        
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
        if (isReverse)
        {
            DeentyLane lane = (DeentyLane) Environment.FindAliveLane(note.laneType, note.laneName);
            inputGraph.GoAcceptEdge(lane.MissRespondStartMoment(note.respondMoment), historyStack);
            timeStack.Pop(lane.GoodRespondStartMoment(note.respondMoment), historyStack);
            inputGraph.GoDenyEdge(lane.GoodRespondStartMoment(note.respondMoment), historyStack);
            timeStack.Pop(lane.PerfectRespondStartMoment(note.respondMoment), historyStack);
            timeStack.Pop(lane.BestPerfectRespondStartMoment(note.respondMoment), historyStack);
            inputGraph.GoAcceptEdge(note.respondMoment, historyStack);
        }
    }
}