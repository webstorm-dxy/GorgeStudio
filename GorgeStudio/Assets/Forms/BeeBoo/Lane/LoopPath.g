using Gorge;
using GorgeFramework;
namespace BeeBoo;

[
    string displayName = "循环路径",
    LoopPath^ defaultInjector = LoopPath : { iterations : 1, loopPath : null }
]
@Editable
class LoopPath : TrackerPath
{
    // 注入字段

    [
        string type = "基本",
        int order = 0,
        string displayName = "循环次数",
        string information = "负数表示无限循环",
        delegate<bool:int> check = bool:(int id) -> { return true; }
    ]
    @Inject
    int iterations = ^iterations;

    [
        string type = "基本",
        int order = 1,
        string displayName = "路径",
        string information = "不能为空",
        delegate<bool:TrackerPath^[]^> check = bool:(TrackerPath^[]^ loopPath) -> { return true; }
    ]
    @Inject<TrackerPath^[]^>
    TrackerPath^[] loopPath = (^loopPath == null) ? null : (new (^loopPath)[^loopPath.length]);
    
    // 构造字段
    TrackerPath[] loopPathConfigs;

    LoopPath() : super()
    {
        if (loopPath == null)
        {
            loopPathConfigs = null;
        }
        else
        {
            loopPathConfigs = new TrackerPath[loopPath.length];
            for (int i = 0; i < loopPath.length; i = i + 1)
            {
                if (loopPath[i] == null)
                {
                    loopPathConfigs[i] = null;
                }
                else
                {
                    loopPathConfigs[i] = new loopPath[i]();
                }
            }
        }
    }

    float GetTime()
    {
        if(iterations < 0)
        {
            return Math.FloatPositiveInfinity();
        }

        float pathTime = 0;
        for(int i=0; i < loopPathConfigs.length; i = i + 1)
        {
            pathTime = pathTime + loopPathConfigs[i].GetTime();
        }

        return pathTime * iterations;
    }

    Vector2 GetPosition(float time)
    {
        float pathTime = 0;
        for(int i=0; i < loopPath.length; i = i + 1)
        {
            pathTime = pathTime + loopPathConfigs[i].GetTime();
        }

        if(pathTime == 0)
        {
            return new Vector2(0, 0);
        }

        float maxTime = GetTime();

        if(time > maxTime)
        {
            time = maxTime;
        }

        if(time < 0)
        {
            time = 0;
        }

        float iterated = Math.Floor(time / pathTime);
        float iteratedTime = iterated * pathTime;
        float leftTime = time - iteratedTime;

        float checkedPathTimeSum = 0;
        float pathStartTime = 0;
        TrackerPath targetPath;

        for(int i = 0; i < loopPathConfigs.length; i = i + 1)
        {
            pathStartTime = checkedPathTimeSum;
            targetPath = loopPathConfigs[i];
            float pathTime = targetPath.GetTime();
            if(pathTime + pathStartTime >= leftTime)
            {
                break for;
            }

            checkedPathTimeSum = checkedPathTimeSum + pathTime;
        }

        if(targetPath == null)
        {
            return new Vector2(0, 0);
        }

        return targetPath.GetPosition(leftTime - pathStartTime);

        // float pathTime = GetTime();
        // float progress;
        // if(pathTime == 0)
        // {
        //     progress = 0;
        // }
        // else
        // {
        //     progress = time / pathTime;
        //     if(progress > 1)
        //     {
        //         progress = 1;
        //     }
        //     else if(progress < 0)
        //     {
        //         progress = 0;
        //     }
        // }

        // return lane.GetPosition(progress);
    }

    void UpdateReference()
    {
        for (int i = 0; i < loopPathConfigs.length; i = i + 1)
        {
            loopPathConfigs[i].UpdateReference();
        }
    }
}