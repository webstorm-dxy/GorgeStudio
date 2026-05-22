using Avalonia.Controls;

namespace GorgeStudio.Views;

/// <summary>
/// 应用程序主窗口的 View（代码隐藏）。
/// 负责初始化 Avalonia 控件树，并暴露嵌入宿主控件供外部服务使用。
/// </summary>
/// <remarks>
/// 具体的 UI 布局定义在同名的 .axaml 文件中。
/// <see cref="EmbedHostControl"/> 属性由 App 层在 DI 配置阶段读取，
/// 传递给 <see cref="Services.EmbedService.EmbedService"/> 作为 Godot 窗口的嵌入目标。
/// </remarks>
public partial class MainWindow : Window
{
    /// <summary>
    /// 初始化主窗口的视觉组件（由 Avalonia 从 .axaml 文件自动生成的 InitializeComponent 方法）。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 获取 .axaml 中定义的嵌入宿主控件引用。
    /// 此控件是 Godot 外部进程窗口在 IDE 中的嵌入容器。
    /// </summary>
    /// <remarks>
    /// 此属性为 <c>internal</c>，仅在 GorgeStudio 程序集内部可见。
    /// App 层通过此属性获取控件引用，传递给 <c>EmbedService</c> 构造函数。
    /// </remarks>
    internal Control EmbedHostControl => EmbedHost;
}
