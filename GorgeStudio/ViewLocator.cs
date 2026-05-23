using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using GorgeStudio.ViewModels;

namespace GorgeStudio;

/// <summary>
/// 视图定位器，基于命名约定将 ViewModel 映射到对应的 View。
/// 例如，将 <c>MainWindowViewModel</c> 替换为 <c>MainWindow</c> 来查找视图类型。
/// 通过反射创建 View 实例，无法找到对应 View 时显示 "Not Found" 文本。
/// </summary>
/// <remarks>
/// 此类实现 <see cref="IDataTemplate"/>，由 Avalonia 数据模板系统自动调用。
/// <see cref="Build"/> 方法负责创建控件，<see cref="Match"/> 方法判断数据对象
/// 是否为 ViewModel（所有 ViewModel 继承自 <see cref="ViewModelBase"/>）。
/// 注意：此类使用了反射，在裁剪（trimming）环境下可能无法正常工作。
/// </remarks>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    /// <summary>
    /// 根据 ViewModel 类型通过命名约定查找并创建对应的 View 控件。
    /// </summary>
    /// <param name="param">ViewModel 实例。为 <c>null</c> 时返回 <c>null</c>。</param>
    /// <returns>
    /// 找到对应的 View 类型时，返回通过反射创建的新实例；
    /// 找不到时返回一个显示 "Not Found: {类型名}" 的 <see cref="TextBlock"/>。
    /// 如果 <paramref name="param"/> 为 <c>null</c>，返回 <c>null</c>。
    /// </returns>
    /// <remarks>
    /// 命名约定：将 ViewModel 类型全名中的 "ViewModel" 替换为 "View"，
    /// 然后通过反射查找对应的类型。例如：
    /// <c>GorgeStudio.ViewModels.MainWindowViewModel</c> →
    /// <c>GorgeStudio.Views.MainWindow</c>。
    /// </remarks>
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    /// <summary>
    /// 判断指定的数据对象是否为此模板的目标类型。
    /// </summary>
    /// <param name="data">待判断的数据上下文对象。</param>
    /// <returns>当 <paramref name="data"/> 是 <see cref="ViewModelBase"/> 的实例时返回 <c>true</c>。</returns>
    /// <remarks>
    /// 此方法被 Avalonia 数据模板系统调用，用于确定是否使用此 <see cref="ViewLocator"/>
    /// 来为给定的 ViewModel 创建 View。只要数据对象继承自 <see cref="ViewModelBase"/>，
    /// 就会匹配成功。
    /// </remarks>
    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
