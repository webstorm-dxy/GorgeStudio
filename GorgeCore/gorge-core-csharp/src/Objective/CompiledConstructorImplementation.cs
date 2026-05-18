using System.Collections.Generic;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeLanguage.Objective
{
    public class CompiledConstructorImplementation : VirtualMachineExecutableBase
    {
        public ConstructorInformation Declaration { get; }
        public override GorgeType ReturnType => null;
        public override IntermediateCode[] Code { get; }
        public override TypeCount LocalVariableCount { get; }

        public CompiledConstructorImplementation(ConstructorInformation declaration,
            List<IntermediateCode> code, TypeCount localVariableCount, string className) : base(className,
            "Constructor")
        {
            Declaration = declaration;
            Code = code.ToArray();
            LocalVariableCount = localVariableCount;
        }
    }
}