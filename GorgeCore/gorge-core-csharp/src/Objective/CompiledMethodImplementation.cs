using System.Collections.Generic;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeLanguage.Objective
{
    public class CompiledMethodImplementation : VirtualMachineExecutableBase
    {
        public MethodInformation Declaration { get; }

        public override GorgeType ReturnType => Declaration.ReturnType;
        public override IntermediateCode[] Code { get; }
        public override TypeCount LocalVariableCount { get; }

        public CompiledMethodImplementation(MethodInformation declaration, List<IntermediateCode> code,
            TypeCount localVariableCount, string className) : base(className, declaration.Name)
        {
            Declaration = declaration;
            Code = code.ToArray();
            LocalVariableCount = localVariableCount;
        }
    }
}