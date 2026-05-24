using GorgeStudio.Models.Chart;

namespace GorgeStudio.Services;

public interface IPeriodEditingService
{
    IPeriod CreatePeriod(IStaff staff, SimulationScore score, float timeOffset);
    IPeriod InsertPeriod(IStaff staff, IPeriod period);
    IPeriod DuplicatePeriod(IStaff staff, IPeriod source);
    void RemovePeriod(IStaff staff, IPeriod period);
    void UpdatePeriodConfig(IPeriod period, float? timeOffset = null, float? minLength = null);
    void UpdatePeriodTimeOffset(IPeriod period, float timeOffset);
    void UpdatePeriodMinLength(IPeriod period, float minLength);
}
