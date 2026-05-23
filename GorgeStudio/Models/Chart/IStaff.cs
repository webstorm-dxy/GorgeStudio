using System.Collections.Generic;

namespace GorgeStudio.Models.Chart;

/// <summary>
/// 谱表（对应一个 Gorge 类）。
/// </summary>
public interface IStaff
{
    string ClassName { get; set; }
    bool IsChartClass { get; }
    string DisplayName { get; set; }
    IEnumerable<IPeriod> Periods { get; }

    bool TryGetPeriod(string periodName, out IPeriod period);
    string ToGorgeCode();
    bool CheckPeriodNameConflict(string periodNameToInsert);
    void AddPeriod(IPeriod period);
    void RemovePeriod(IPeriod period);
    IStaff Clone();
}
