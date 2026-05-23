using Avalonia.Controls;

namespace GorgeStudio.Views;

/// <summary>
/// 属性面板的 View（代码隐藏）。
/// 展示当前选中对象的属性列表，支持字段和方法参数的详细查看。
/// </summary>
/// <remarks>
/// 具体的 UI 布局定义在同名的 .axaml 文件中。
/// 通过 Avalonia 数据绑定连接到 <see cref="ViewModels.PropertiesPanelViewModel"/>。
/// </remarks>
public partial class PropertiesPanelView : UserControl
{
    /// <summary>
    /// 初始化属性面板的视觉组件。
    /// </summary>
    public PropertiesPanelView()
    {
        InitializeComponent();
    }
}
