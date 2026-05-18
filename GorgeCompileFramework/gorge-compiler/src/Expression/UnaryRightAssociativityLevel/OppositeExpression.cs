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
    /// 取相反数
    /// 要求操作数是int或float
    /// 结果与操作数类型一直
    /// </summary>
    public class OppositeExpression : BaseValueExpression
    {
        private readonly IGorgeValueExpression _operand;

        public OppositeExpression(IGorgeValueExpression operand, CodeBlockScope context,
            ParserRuleContext antlrContext) :
            base(
                context, antlrContext)
        {
            if (operand.ValueType.BasicType is not BasicType.Int and not BasicType.Float)
            {
                throw new Exception($"操作符Opposite的操作数类型必须为int或float，但实际类型为{operand.ValueType}");
            }

            ValueType = operand.ValueType;
            _operand = operand;
            IsCompileConstant = operand.IsCompileConstant;
            if (IsCompileConstant)
            {
                if (operand.ValueType.BasicType is BasicType.Int)
                {
                    CompileConstantValue = -(int) operand.CompileConstantValue;
                }
                else
                {
                    CompileConstantValue = -(float) operand.CompileConstantValue;
                }
            }
        }

        public override SymbolicGorgeType ValueType { get; }

        protected override SymbolicAddress AppendNotConstantCodes(CodeBlockScope codeBlockScope,
            List<IntermediateCode> existCodes)
        {
            // 子表达式返回地址
            var operandResultAddress = _operand.AppendCodes(Block, existCodes);

            var code = _operand.ValueType.BasicType switch
            {
                BasicType.Int => IntermediateCode.IntOpposite(ValueAddress, (Address) operandResultAddress),
                BasicType.Float => IntermediateCode.FloatOpposite(ValueAddress, (Address) operandResultAddress),
                _ => throw new Exception("未知操作数类型")
            };

            existCodes.Add(code);
            return ValueAddress;
        }

        public override bool IsCompileConstant { get; }
        public override object CompileConstantValue { get; }
    }
}