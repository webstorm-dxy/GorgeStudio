using System.Collections.ObjectModel;

namespace GorgeStudio.Models;

/// <summary>
/// 元素列表面板中 TreeView 的节点数据模型。
/// 支持按命名空间→类/枚举/接口→成员的层级展示编译结果。
/// </summary>
public class TreeNode
{
    /// <summary>
    /// 节点在 TreeView 中显示的文本。
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// 节点的完全限定名称（如 "Namespace.ClassName"）。
    /// </summary>
    public string? FullName { get; init; }

    /// <summary>
    /// 节点所代表的数据类别，用于图标和样式区分。
    /// </summary>
    public TreeNodeCategory Category { get; init; }

    /// <summary>
    /// 节点关联的底层数据对象。
    /// 对于 Class 节点为 CompiledClassInfo，Field 节点为 FieldInfo，等等。
    /// 选中节点时，此对象会被传递给属性面板展示。
    /// </summary>
    public object? Tag { get; init; }

    /// <summary>
    /// 子节点集合，用于构建树形层级。
    /// </summary>
    public ObservableCollection<TreeNode> Children { get; init; } = new();

    /// <summary>
    /// 节点是否展开。绑定到 TreeViewItem.IsExpanded。
    /// </summary>
    public bool IsExpanded { get; set; }
}

/// <summary>
/// TreeView 节点所代表的数据类别。
/// </summary>
public enum TreeNodeCategory
{
    Namespace,
    Class,
    Enum,
    Interface,
    Field,
    Method,
    StaticMethod,
    Constructor,
    InjectorField,
    Annotation,
    Parameter,
    Staff,
    Period,
    Element
}
