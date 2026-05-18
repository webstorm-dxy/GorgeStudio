using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.AssignmentTarget
{
    /// <summary>
    /// 本地变量作为赋值对象
    /// 值地址为变量所在地址
    /// </summary>
    public class LocalVariable : AssignmentTargetExpression
    {
        private readonly SymbolicAddress _variableAddress;

        public LocalVariable(SymbolicAddress variableAddress, CodeBlockScope block,
            ParserRuleContext antlrContext) : base(block, antlrContext)
        {
            _variableAddress = variableAddress;
            ValueType = variableAddress.Type;
            AssignType = variableAddress.Type;
            FieldIndex = variableAddress.Index;
            AssignmentTargetType = AssignmentTargetType.LocalVariable;
        }

        public override int FieldIndex { get; }
        public override AssignmentTargetType AssignmentTargetType { get; }
        public override SymbolicGorgeType AssignType { get; }
        public override Address DynamicAccessorAddress => default;
        public override SymbolicGorgeType ValueType { get; }

        protected override SymbolicAddress AppendNotConstantCodes(CodeBlockScope codeBlockScope,
            List<IntermediateCode> existCodes)
        {
            return _variableAddress;
        }
    }
}