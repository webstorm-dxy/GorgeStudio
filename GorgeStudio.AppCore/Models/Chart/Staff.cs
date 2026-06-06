using System;
using System.Collections.Generic;
using System.Linq;

namespace GorgeStudio.Models.Chart;

public abstract class Staff<T> : IStaff where T : class, IPeriod
{
    public string ClassName { get; set; }
    public bool IsChartClass { get; }
    public string DisplayName { get; set; }

    IEnumerable<IPeriod> IStaff.Periods => Periods;
    public List<T> Periods { get; }

    protected Staff(string className, bool isChartClass, string displayName)
    {
        ClassName = className;
        IsChartClass = isChartClass;
        DisplayName = displayName;
        Periods = new List<T>();
    }

    public abstract string ToGorgeCode();

    public void AddPeriod(IPeriod period)
    {
        if (period is not T periodT)
            throw new ArgumentException($"Period type {period.GetType().Name} is not compatible with Staff<{typeof(T).Name}>");
        Periods.Add(periodT);
    }

    public void RemovePeriod(IPeriod period)
    {
        if (period is not T periodT)
            throw new ArgumentException($"Period type {period.GetType().Name} is not compatible with Staff<{typeof(T).Name}>");
        Periods.Remove(periodT);
    }

    IStaff IStaff.Clone() => Clone();
    protected abstract Staff<T> Clone();

    public bool TryGetPeriod(string periodName, out IPeriod period)
    {
        period = Periods.FirstOrDefault(p => p.MethodName == periodName);
        return period != null;
    }

    public bool CheckPeriodNameConflict(string periodNameToInsert)
    {
        if (periodNameToInsert == ClassName)
            return true;
        return Periods.Any(p => p.MethodName == periodNameToInsert);
    }
}
