using Gorge;
using GorgeFramework;
namespace Deenty;

class LineStripNotePositionXTransformer :: ITransformer
{
    Hold note;
    
    // 保存的显示边界，只在提前释放时使用
    float displayStartBorder;
    
    // 追赶完成标记，只在提前释放时使用
    bool isCaught;
    
    // 最后更新时间，只在提前释放时使用
    float lastUpdateMoment;
    
    LineStripNotePositionXTransformer(Hold note)
    {
        this.note = note;
        lastUpdateMoment = note.respondMoment;
    }
    
    IAutomatonCommand[] Transform(float now)
    {
        string automatonState = note.automaton.GetState();
        note.isDark = automatonState == "Denied";
        float showMoment = note.respondMoment - note.respondTime;
        float startCurveTime = (now - showMoment) / note.respondTime;
        float endCurveTime = (now - showMoment - note.holdTime) / note.respondTime;
        
        /*
            等待按住：按真实边界渲染
            按住中：起点在0，终点按真实边界渲染
            提前放开：起点从0开始以定速追赶真实起点，终点按真实边界渲染
            正常完成：由销毁器立即销毁
        */
        float startBorder;
        
        // 按住状态，起点为0，终点按显示边界渲染
        if (automatonState == "Holding" || (automatonState == "Accepted" && now < note.respondMoment + note.holdTime))
        {
            startBorder = 0;
        }
        // 中途放开状态，头部以定速追逐真实起点
        else if (automatonState == "Denied")
        {
            // 计算真实起点            
            float realStartBorder = (1 - note.positionStartXCurve.Evaluate(startCurveTime)) * note.laneLength;
            // 如果已经追上，则使用真实起点
            if (isCaught)
            {
                startBorder = realStartBorder;
            }
            // 如果没有追上，则实施追逐
            else
            {
                // 计算剩余追逐路程
                float diff = realStartBorder - displayStartBorder;
                // 计算追逐步长
                float step = note.laneLength * (now - lastUpdateMoment) / note.respondTime;
                // 如果能一步追上，则直接追上
                if (Math.Abs(diff) - step < 0.001)
                {
                    isCaught = true;
                    startBorder = realStartBorder;
                }
                // 如果不能一步追上，则追逐一个步长
                else
                {
                    if (diff > 0)
                    {
                        startBorder = displayStartBorder + step;
                    }
                    else
                    {
                        startBorder = displayStartBorder - step;
                    }
                }
            }
        }
        // 等待按住状态，按照显示边界渲染。完成状态也在此类，但是会隐藏
        else
        {
            startBorder = (1 - note.positionStartXCurve.Evaluate(startCurveTime)) * note.laneLength;
        }
        
        // 计算结束边界
        float endBorder = (1 - note.positionEndXCurve.Evaluate(endCurveTime)) * note.laneLength;
        
        // 存储本部变化信息
        displayStartBorder = startBorder;
        lastUpdateMoment = now;
        
        // 实施长度修改
        note.graphNode.size.x = Math.Abs(startBorder - endBorder);
        note.graphNode.position.x = (startBorder + endBorder) / 2;
        
        return null;
    }
}