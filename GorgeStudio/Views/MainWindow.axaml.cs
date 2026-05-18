using Avalonia.Controls;

namespace GorgeStudio.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 暴露嵌入区域控件，供 App 层创建 EmbedService 时使用。
    /// </summary>
    internal Control EmbedHostControl => EmbedHost;
}
