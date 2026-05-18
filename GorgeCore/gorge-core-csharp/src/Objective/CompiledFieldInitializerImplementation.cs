using System.Collections.Generic;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeLanguage.Objective
{
    public class CompiledFieldInitializerImplementation : VirtualMachineExecutableBase
    {
        public CompiledFieldInitializerImplementation(FieldInformation information,
            List<IntermediateCode> code, TypeCount localVariableCount, string className) : base(className,
            "FieldInitialize")
        {
            Information = information;
            Code = code.ToArray();
            LocalVariableCount = localVariableCount;
        }

        public FieldInformation Information { get; }

        public override GorgeType ReturnType => Information.Type;
        public override IntermediateCode[] Code { get; }
        public override TypeCount LocalVariableCount { get; }
    }
}