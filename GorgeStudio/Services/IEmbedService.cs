using System;
using System.Threading.Tasks;

namespace GorgeStudio.Services;

/// <summary>
/// 嵌入外部进程的抽象服务。ViewModel 通过此接口触发嵌入，不依赖具体 View 控件。
/// </summary>
public interface IEmbedService
{
    /// <summary>启动外部进程并嵌入到宿主区域。</summary>
    Task<bool> LaunchAsync();

    /// <summary>状态变化通知，ViewModel 订阅以更新 UI。</summary>
    event Action<string>? StatusChanged;
}
