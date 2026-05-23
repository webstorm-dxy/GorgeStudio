namespace GorgeStudio.Models.Chart;

/// <summary>
/// 时间轴上的一个元素，表示谱面中的一个事件（音符、拍点等）。
/// </summary>
public class TimelineElement
{
    /// <summary>
    /// 元素在时间轴上的显示标签。
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// 起始时间（毫秒）。
    /// </summary>
    public double StartTime { get; init; }

    /// <summary>
    /// 持续时间（毫秒）。0 表示瞬时事件。
    /// </summary>
    public double Duration { get; init; }

    /// <summary>
    /// 元素分类（如 Note, Tap, Hold, Flick 等）。
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// 所属的 Injector 字段名称。
    /// </summary>
    public string SourceInjector { get; init; } = string.Empty;
}
