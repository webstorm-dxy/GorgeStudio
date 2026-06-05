using Gorge;
namespace GorgeFramework;

// 信号过滤器
// 目前只包含触控信号
native class InputSignalFilter : SignalFilter
{
    // 触控区
    delegate<bool:TouchSignal> touchArea;

    InputSignalFilter(delegate<Priority[]> priority, int[] touchType, delegate<bool:TouchSignal> touchArea, delegate<float> endTime, TimeMode timeMode, bool acceptConsume, bool denyConsume);
    
    bool CanDetect(string channelName);
    
    bool Detect(string channelName, int signalId, int conditionType, ISignal signalValue, ISignal lastSignalValue);
}