using GorgeStudio.Models.Chart;

namespace GorgeStudio.Services;

public interface IPeriodEditingService
{
    IPeriod CreatePeriod(IStaff staff, SimulationScore score, float timeOffset);
    IPeriod InsertPeriod(IStaff staff, IPeriod period);
    IPeriod DuplicatePeriod(IStaff staff, IPeriod source);
    void RemovePeriod(IStaff staff, IPeriod period);
    void UpdatePeriodTimeOffset(IPeriod period, float timeOffset);
}
