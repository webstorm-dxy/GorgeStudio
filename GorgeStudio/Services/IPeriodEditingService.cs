using GorgeStudio.Models.Chart;

namespace GorgeStudio.Services;

public interface IPeriodEditingService
{
    IPeriod CreatePeriod(IStaff staff, SimulationScore score, float timeOffsetSeconds);
    IPeriod InsertPeriod(IStaff staff, IPeriod period);
    IPeriod DuplicatePeriod(IStaff staff, IPeriod source);
    void RemovePeriod(IStaff staff, IPeriod period);
    void UpdatePeriodConfig(IPeriod period, float? timeOffsetSeconds = null, float? minLengthSeconds = null);
    void UpdatePeriodTimeOffset(IPeriod period, float timeOffsetSeconds);
    void UpdatePeriodMinLength(IPeriod period, float minLengthSeconds);
}
