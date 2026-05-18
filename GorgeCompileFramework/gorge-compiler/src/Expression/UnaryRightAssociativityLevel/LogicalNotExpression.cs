using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.UnaryRightAssociativityLevel
{
    /// <summary>
    /// 逻辑非
    /// 要求操作数是bool
    /// 结果是bool
    /// </summary>
    public class LogicalNotExpression : BaseValueExpression
    {
        private readonly IGorgeValueExpression _operand;

        public LogicalNotExpression(IGorgeValueExpression operand, CodeBlockScope context,
            ParserRuleContext antlrContext) :
            base(context, antlrContext)
        {
            if (operand.ValueType.BasicType != BasicType.Bool)
            {
                throw new Exception($"操作符Not的操作数类型必须为bool，但实际类型为{operand.ValueType}");
            }

            ValueType = SymbolicGorgeType.Bool;
            _operand = operand;
            IsCompileConstant = operand.IsCompileConstant;
            if (IsCompileConstant)
            {
                CompileConstantValue = !(bool) operand.CompileConstantValue;
            }
        }

        public override SymbolicGorgeType ValueType { get; }

        protected override SymbolicAddress AppendNotConstantCodes(CodeBlockScope codeBlockScope,
            List<IntermediateCode> existCodes)
        {
            existCodes.Add(IntermediateCode.LogicalNot(ValueAddress, (Address) _operand.AppendCodes(Block, existCodes)));
            return ValueAddress;
        }

        public override bool IsCompileConstant { get; }
        public override object CompileConstantValue { get; }
    }
}