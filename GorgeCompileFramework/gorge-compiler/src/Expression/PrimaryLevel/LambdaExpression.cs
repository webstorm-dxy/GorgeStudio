using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.PrimaryLevel
{
    public class LambdaExpression : DynamicValueExpression
    {
        private readonly SymbolicAddress _delegateObjectAddress;
        private readonly List<IntermediateCode> _saveOuterCode;

        public LambdaExpression(SymbolicAddress delegateObjectAddress, List<IntermediateCode> saveOuterCode,
            CodeBlockScope block,
            ParserRuleContext expressionLocation) : base(block, expressionLocation)
        {
            _delegateObjectAddress = delegateObjectAddress;
            _saveOuterCode = saveOuterCode;
            ValueType = delegateObjectAddress.Type;
        }

        public override SymbolicAddress AppendCodes(CodeBlockScope codeBlockScope, List<IntermediateCode> existCodes)
        {
            existCodes.AddRange(_saveOuterCode);
            return _delegateObjectAddress;
        }

        public override SymbolicGorgeType ValueType { get; }
    }
}