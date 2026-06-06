using System;
using System.Linq;
using Gorge.GorgeLanguage.Objective;
using Gorge.Native.Gorge;
using GorgeStudio.Models.Chart;

namespace GorgeStudio.Services;

public class PeriodEditingService : IPeriodEditingService
{
    private const string PeriodConfigFullName = "GorgeFramework.PeriodConfig";
    private const string WavAudioAssetFullName = "GorgeFramework.WavAudioAsset";
    private const string AudioAssetFullName = "GorgeFramework.AudioAsset";

    public IPeriod CreatePeriod(IStaff staff, SimulationScore score, float timeOffsetSeconds)
    {
        if (score.ClassDeclarations == null)
            throw new InvalidOperationException("SimulationScore.ClassDeclarations is null.");

        if (!score.ClassDeclarations.TryGetValue(PeriodConfigFullName, out var periodConfigDecl))
        {
            var existingPeriod = score.Stave.SelectMany(s => s.Periods).FirstOrDefault();
            if (existingPeriod != null)
                periodConfigDecl = existingPeriod.ConfigInjector.InjectedClassDeclaration;
            else
                throw new InvalidOperationException($"Cannot create period: {PeriodConfigFullName} not found in ClassDeclarations and no existing period to clone config from.");
        }

        var config = new CompiledInjector(periodConfigDecl);

        var timeField = periodConfigDecl.InjectorFields.FirstOrDefault(f => f.Name == "timeOffset");
        if (timeField != null)
            config.SetInjectorFloat(timeField.Index, timeOffsetSeconds);

        if (staff is ElementStaff elementStaff)
        {
            return new ElementPeriod(elementStaff.FormName, "Period", config);
        }

        if (staff is AudioStaff)
        {
            Injector audioInjector;
            if (score.ClassDeclarations.TryGetValue(WavAudioAssetFullName, out var wavDecl))
                audioInjector = new CompiledInjector(wavDecl);
            else if (score.ClassDeclarations.TryGetValue(AudioAssetFullName, out var audioDecl))
                audioInjector = new CompiledInjector(audioDecl);
            else
                throw new InvalidOperationException($"Cannot create AudioPeriod: neither {WavAudioAssetFullName} nor {AudioAssetFullName} found in ClassDeclarations.");

            return new AudioPeriod("Period", config, audioInjector);
        }

        throw new ArgumentException($"Unsupported staff type: {staff.GetType().Name}");
    }

    public IPeriod InsertPeriod(IStaff staff, IPeriod period)
    {
        var baseName = period.MethodName;
        var name = baseName;
        var counter = 1;

        while (staff.CheckPeriodNameConflict(name))
        {
            name = $"{baseName}{counter}";
            counter++;
        }

        period.MethodName = name;
        staff.AddPeriod(period);
        return period;
    }

    public IPeriod DuplicatePeriod(IStaff staff, IPeriod source)
    {
        var clone = source.Clone();
        clone.MethodName = source.MethodName;
        return InsertPeriod(staff, clone);
    }

    public void RemovePeriod(IStaff staff, IPeriod period)
    {
        staff.RemovePeriod(period);
    }

    private const float MinimumPeriodLengthSeconds = 0.25f;

    public void UpdatePeriodConfig(IPeriod period, float? timeOffsetSeconds = null, float? minLengthSeconds = null)
    {
        var newConfig = (Injector)period.ConfigInjector.Clone();
        var decl = newConfig.InjectedClassDeclaration;

        if (timeOffsetSeconds.HasValue)
        {
            if (!decl.TryGetInjectorFieldByName("timeOffset", out var field))
                throw new InvalidOperationException("PeriodConfig missing timeOffset field.");
            newConfig.SetInjectorFloat(field.Index, Math.Max(0, timeOffsetSeconds.Value));
        }

        if (minLengthSeconds.HasValue)
        {
            if (!decl.TryGetInjectorFieldByName("minLength", out var field))
                throw new InvalidOperationException("PeriodConfig missing minLength field.");
            newConfig.SetInjectorFloat(field.Index, Math.Max(MinimumPeriodLengthSeconds, minLengthSeconds.Value));
        }

        period.UpdateConfig(newConfig);
    }

    public void UpdatePeriodTimeOffset(IPeriod period, float timeOffsetSeconds)
    {
        UpdatePeriodConfig(period, timeOffsetSeconds: timeOffsetSeconds);
    }

    public void UpdatePeriodMinLength(IPeriod period, float minLengthSeconds)
    {
        UpdatePeriodConfig(period, minLengthSeconds: minLengthSeconds);
    }
}
