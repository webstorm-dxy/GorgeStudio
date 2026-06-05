using Gorge;
using GorgeFramework;
namespace Deenty;

class HoldAuto
{
    TimeStack timeStack;
    InputGraph inputGraph;
    HistoryStack historyStack;
    
    HoldAuto(Hold note, bool isReverse)
    {
        historyStack = new HistoryStack();
        
        // 在RespondMoment时间点上响应，并且将状态切换为Holding
        InputGraphState state0 = new InputGraphState(
            new InputSignalFilter(
                Priority[]:() ->
                {
                    return new Priority[0]{,};
                },
                new TouchType[0]{,},
                bool:(TouchSignal signal) ->
                {
                    return false;
                },
                float:() ->
                {
                    return note.respondMoment;
                },
                TimeMode.CatchBefore,
                false,
                false
            ),
            new InputGraphEdge(false, 1, null, false, true, false, "Holding"),
            new InputGraphEdge(false, 1, null, false, true, false, "Holding")
        );
        
        // 在FinishMoment时间点上响应，并且将状态切换为Accepted
        InputGraphState state1 = new InputGraphState(
            new InputSignalFilter(
                Priority[]:() ->
                {
                    return new Priority[0]{,};
                },
                new TouchType[0]{,},
                bool:(TouchSignal signal) ->
                {
                    return false;
                },
                float:() ->
                {
                    return note.respondMoment + note.holdTime;
                },
                TimeMode.CatchBefore,
                false,
                false
            ),
            new InputGraphEdge(false, 1, null, true, true, false, "Accepted"),
            new InputGraphEdge(false, 1, null, true, true, false, "Accepted")
        );
        
        InputGraphState[] states = new InputGraphState[2];
        states[0] = state0;
        states[1] = state1;
        
        inputGraph = new InputGraph(states, true, true, 0, "Waiting");
        
        timeStack = new TimeStack(false, null);
        timeStack.InitPush(
            new TimeItem(
                float:() ->
                {
                    return note.respondMoment + note.holdTime;
                },
                true,
                "MutedBestPerfect"
            )
        );
        
        // 中间响应倒序压栈
        for (int i = note.respondQuantity - 2; i > 0; i = i - 1)
        {
            int index = i;
            timeStack.InitPush(
                new TimeItem(
                    float:() ->
                    {
                        return note.respondMoment + index * (note.holdTime / (note.respondQuantity - 1));
                    },
                    false,
                    "MutedBestPerfect"
                )
            );
        }
        
        timeStack.InitPush(
            new TimeItem(
                float:() ->
                {
                    return note.respondMoment;
                },
                false,
                "BestPerfect"
            )
        );
        
        /*
            反转情况对应正转路径
            RespondMoment : BestPerfect弹栈
            RespondMoment : 0->1
            中间节点 : MutedBestPerfect陆续弹栈
            FinishMoment : MutedBestPerfect
            FinishMoment : 1->2
            Accept
            TODO 此处尝试使用模拟，这是假定边动作只有时间栈压栈时才能成立
        */
        if (isReverse)
        {
            timeStack.Pop(note.respondMoment, historyStack);
            inputGraph.GoAcceptEdge(note.respondMoment, historyStack);
            for (int i = 1; i < note.respondQuantity - 1; i = i + 1)
            {
                int index = i;
                float time = note.respondMoment + index * (note.holdTime / (note.respondQuantity - 1));
                timeStack.Pop(time, historyStack);
            }
            
            timeStack.Pop(note.respondMoment + note.holdTime, historyStack);
            inputGraph.GoAcceptEdge(note.respondMoment + note.holdTime, historyStack);
        }
    }
}