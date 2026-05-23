using CommunityToolkit.Mvvm.ComponentModel;

namespace GorgeStudio.ViewModels;

/// <summary>
/// 所有 ViewModel 的抽象基类。继承自 CommunityToolkit.Mvvm 的 <see cref="ObservableObject"/>，
/// 为派生类提供属性变更通知（INotifyPropertyChanged）等 MVVM 基础能力。
/// </summary>
/// <remarks>
/// 当前为空的抽象类，未添加额外成员。设计为扩展点，未来可在此基类中添加
/// 所有 ViewModel 共享的逻辑，如错误处理、导航、消息传递等。
/// 所有具体 ViewModel 必须继承此类，以满足 <see cref="ViewLocator"/> 的匹配规则。
/// </remarks>
public abstract class ViewModelBase : ObservableObject
{
}
