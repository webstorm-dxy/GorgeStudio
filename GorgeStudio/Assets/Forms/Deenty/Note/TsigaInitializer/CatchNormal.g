using Gorge;
using GorgeFramework;
namespace Deenty;

class CatchNormal
{
    TimeStack timeStack;
    InputGraph inputGraph;
    HistoryStack historyStack;
    
    CatchNormal(DeentyNote note, bool isReverse)
    {
        historyStack = new HistoryStack();
        
        // 第一阶段 -GoodStart，无视任何输入（不过滤任何输入，过滤命中拒绝，不消耗）直到时间结束，
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
        
        // 第二阶段 GoodStart-GoodRespondEndMoment，时间内捕获Keep，消耗，接收进入成功，拒绝进入失败
        // 非Miss点击
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
                false,
                // 已经忘了为什么这里要挂拒绝消耗了，可能是为了抑制对catch的点击穿透
                true
            ),
            new InputGraphEdge(false, 1, null, true, true, true, "Accepted"),
            new InputGraphEdge(true, 0, null, true, false, false, "Timeout")
        );
        
        InputGraphState[] states = new InputGraphState[2];
        states[0] = state0;
        states[1] = state1;
        
        inputGraph = new InputGraph(states, false, false, 0, "Waiting");
        
        timeStack = new TimeStack(false, null);
        
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
        
        // respondMoment后允许响应
        timeStack.InitPush(
            new TimeItem(
                float:() ->
                {
                    return note.respondMoment;
                },
                true,
                "BestPerfect"
            )
        );
        
        /*
            反转情况对应正转路径
            GoodStart : 0->1
            RespondMoment : BestPerfect弹栈
            RespondMoment : 1->2
            Accept
        */
        if (isReverse)
        {
            DeentyLane lane = (DeentyLane) Environment.FindAliveLane(note.laneType, note.laneName);
            inputGraph.GoAcceptEdge(lane.GoodRespondStartMoment(note.respondMoment), historyStack);
            timeStack.Pop(note.respondMoment, historyStack);
            inputGraph.GoAcceptEdge(note.respondMoment, historyStack);
        }
    }
}