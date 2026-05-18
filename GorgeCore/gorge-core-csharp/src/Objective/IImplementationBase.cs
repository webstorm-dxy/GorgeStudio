using System.Collections.Generic;

namespace Gorge.GorgeLanguage.Objective
{
    public interface IImplementationBase 
    {
        public IEnumerable<GorgeClass> Classes { get; }
        
        public IEnumerable<GorgeInterface> Interfaces { get; }

        public IEnumerable<GorgeEnum> Enums { get; }
    }
}