using Gorge;
using GorgeFramework;
namespace BeeBoo;

class TrackerPath
{
    injector TrackerPath()
    {
    }

    // 获取路径所用时间
    float GetTime()
    {
        // Abstract
        return 0;
    }

    // 获取路径位置
    // time : 路径时间
    Vector2 GetPosition(float time)
    {
        // Abstract
        return null;
    }

    // 更新轨道引用
    void UpdateReference()
    {
        // Abstract
        return;
    }
}