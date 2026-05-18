using System.Collections.Generic;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeLanguage.Serialization;

public class DeserializedImplementationContext : IImplementationBase
{
    public List<GorgeClass> Classes { get; init; } = new();
    public List<GorgeInterface> Interfaces { get; init; } = new();
    public List<GorgeEnum> Enums { get; init; } = new();

    IEnumerable<GorgeClass> IImplementationBase.Classes => Classes;
    IEnumerable<GorgeInterface> IImplementationBase.Interfaces => Interfaces;
    IEnumerable<GorgeEnum> IImplementationBase.Enums => Enums;
}
