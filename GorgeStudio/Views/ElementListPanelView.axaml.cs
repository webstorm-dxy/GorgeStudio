using Avalonia.Controls;

namespace GorgeStudio.Views;

/// <summary>
/// 元素列表面板的 View（代码隐藏）。
/// 提供编译后项目中类、枚举、接口的树形列表展示。
/// </summary>
/// <remarks>
/// 具体的 UI 布局定义在同名的 .axaml 文件中。
/// 通过 Avalonia 数据绑定连接到 <see cref="ViewModels.ElementListPanelViewModel"/>。
/// </remarks>
public partial class ElementListPanelView : UserControl
{
    /// <summary>
    /// 初始化元素列表面板的视觉组件。
    /// </summary>
    public ElementListPanelView()
    {
        InitializeComponent();
    }
}
