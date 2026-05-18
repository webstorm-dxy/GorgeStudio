using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.AssignmentTarget
{
    /// <summary>
    /// this出现在赋值左侧
    /// </summary>
    public class This : AssignmentTargetExpression
    {
        public This(CodeBlockScope block, ParserRuleContext antlrContext) : base(block, antlrContext)
        {
            ValueType = block.ClassSymbol.Type;
            AssignmentTargetType = AssignmentTargetType.This;
            AssignType = block.ClassSymbol.Type;
        }

        public override SymbolicGorgeType ValueType { get; }

        public override int FieldIndex => default;

        public override AssignmentTargetType AssignmentTargetType { get; }
        public override SymbolicGorgeType AssignType { get; }
        public override Address DynamicAccessorAddress => default;

        protected override SymbolicAddress AppendNotConstantCodes(CodeBlockScope codeBlockScope,
            List<IntermediateCode> existCodes)
        {
            existCodes.Add(IntermediateCode.LoadThis(ValueAddress));

            return ValueAddress;
        }
    }
}