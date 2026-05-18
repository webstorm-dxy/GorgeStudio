using System;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    public record SymbolicAddress
    {
        public readonly SymbolicGorgeType Type;
        public readonly int Index;

        public SymbolicAddress(SymbolicGorgeType type, int index)
        {
            Type = type;
            Index = index;
        }

        public override string ToString()
        {
            return $"{Type}:{Index}";
        }

        public IntermediateOperator GetAssignOperand()
        {
            return Type.BasicType switch
            {
                BasicType.Int or BasicType.Enum => IntermediateOperator.LocalIntAssign,
                BasicType.Float => IntermediateOperator.LocalFloatAssign,
                BasicType.Bool => IntermediateOperator.LocalBoolAssign,
                BasicType.String => IntermediateOperator.LocalStringAssign,
                BasicType.Object or BasicType.Interface or BasicType.Delegate => IntermediateOperator.LocalObjectAssign,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static implicit operator Address(SymbolicAddress symbolicAddress) =>
            new Address() {Type = symbolicAddress.Type, Index = symbolicAddress.Index};
    }
}