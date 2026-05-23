using System;
using System.Threading.Tasks;

namespace GorgeStudio.Services;

/// <summary>
/// 嵌入外部进程的抽象服务接口。
/// ViewModel 通过此接口触发嵌入操作，不依赖任何具体的 View 控件或平台实现。
/// 在 Windows 上，外部进程的主窗口被嵌入到 Avalonia 控件区域中；
/// 在 macOS 上，进程以独立窗口方式运行。
/// </summary>
public interface IEmbedService
{
    /// <summary>
    /// 启动外部 Godot 进程并将其主窗口嵌入（或关联）到 IDE 宿主区域。
    /// </summary>
    /// <returns>
    /// 嵌入成功返回 <c>true</c>；如果可执行文件未找到、进程启动失败或窗口超时则返回 <c>false</c>。
    /// </returns>
    /// <remarks>
    /// 调用前会先释放上一次嵌入的资源。通过 <see cref="IWindowEmbedder"/> 工厂创建平台特定的嵌入器。
    /// </remarks>
    Task<bool> LaunchAsync();

    /// <summary>
    /// 嵌入过程中的状态变化通知事件。
    /// ViewModel 订阅此事件以实时更新 UI 状态栏文本。
    /// </summary>
    event Action<string>? StatusChanged;
}
