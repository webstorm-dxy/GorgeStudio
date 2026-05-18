using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Expression.Tools;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.UnaryLeftAssociativityLevel
{
    public class DelegateInvokeExpression : DynamicValueExpression
    {
        private readonly IGorgeValueExpression _delegateInstance;
        private readonly IGorgeValueExpression[] _parameterExpressions;

        public DelegateInvokeExpression(IGorgeValueExpression delegateInstance,
            IGorgeValueExpression[] parameterExpressions,
            CodeBlockScope block, ParserRuleContext expressionLocation) : base(block, expressionLocation)
        {
            _delegateInstance = delegateInstance;
            _parameterExpressions = parameterExpressions;

            // TODO 类型检查

            ValueType = delegateInstance.ValueType.Assert<DelegateType>(delegateInstance.ExpressionLocation).ReturnType;
        }

        public override SymbolicAddress AppendCodes(CodeBlockScope codeBlockScope, List<IntermediateCode> existCodes)
        {
            var delegateInstance = _delegateInstance.AppendCodes(Block, existCodes);
            CommonImmediateCodes.SetInvocationArguments(Block, delegateInstance.Type.Assert<DelegateType>(_delegateInstance.ExpressionLocation).ParameterTypes.ToArray(),
                _parameterExpressions, existCodes);

            existCodes.Add(IntermediateCode.InvokeDelegate((Address)delegateInstance));

            if (ValueType is VoidType)
            {
                return default;
            }

            var returnAddress = Block.AddTempVariable(ValueType);
            existCodes.Add(IntermediateCode.GetReturn(returnAddress, ValueType));

            return returnAddress;
        }

        public override SymbolicGorgeType ValueType { get; }
    }
}