using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.LogicalLevel
{
    /// <summary>
    /// 二元逻辑运算：逻辑与、逻辑或
    /// 要求操作数是bool
    /// 结果是bool
    /// </summary>
    public class BinaryLogicalExpression : BaseValueExpression
    {
        private readonly BinaryLogicalOperator _logicalOperator;
        private readonly IGorgeValueExpression _left;
        private readonly IGorgeValueExpression _right;

        public BinaryLogicalExpression(BinaryLogicalOperator logicalOperator, IGorgeValueExpression left,
            IGorgeValueExpression right, CodeBlockScope context, ParserRuleContext antlrContext) : base(context,
            antlrContext)
        {
            if (left.ValueType.BasicType != BasicType.Bool)
            {
                throw new GorgeCompileException($"操作符{logicalOperator}的左操作数类型必须为bool，但实际类型为{right.ValueType}",antlrContext);
            }

            if (right.ValueType.BasicType != BasicType.Bool)
            {
                throw new GorgeCompileException($"操作符{logicalOperator}的右操作数类型必须为bool，但实际类型为{right.ValueType}",antlrContext);
            }

            ValueType = SymbolicGorgeType.Bool;
            _logicalOperator = logicalOperator;
            _left = left;
            _right = right;
            IsCompileConstant = left.IsCompileConstant && right.IsCompileConstant;
            if (IsCompileConstant)
            {
                switch (logicalOperator)
                {
                    case BinaryLogicalOperator.And:

                        CompileConstantValue = (bool) left.CompileConstantValue && (bool) right.CompileConstantValue;
                        break;
                    case BinaryLogicalOperator.Or:
                        CompileConstantValue = (bool) left.CompileConstantValue || (bool) right.CompileConstantValue;
                        break;
                    default:
                        throw new Exception("未知操作符");
                }
            }
        }

        public override SymbolicGorgeType ValueType { get; }

        protected override SymbolicAddress AppendNotConstantCodes(CodeBlockScope codeBlockScope,
            List<IntermediateCode> existCodes)
        {
            // 子表达式返回地址
            var leftResultAddress = _left.AppendCodes(Block, existCodes);
            var rightResultAddress = _right.AppendCodes(Block, existCodes);

            var resultAddress = Block.AddTempVariable(SymbolicGorgeType.Bool);

            var code = _logicalOperator switch
            {
                BinaryLogicalOperator.And => IntermediateCode.LogicalAnd(resultAddress, (Address) leftResultAddress,
                    (Address) rightResultAddress),
                BinaryLogicalOperator.Or => IntermediateCode.LogicalOr(resultAddress, (Address) leftResultAddress,
                    (Address) rightResultAddress),
                _ => throw new Exception("未知运算符")
            };

            existCodes.Add(code);
            return resultAddress;
        }

        public override bool IsCompileConstant { get; }
        public override object CompileConstantValue { get; }
    }

    public enum BinaryLogicalOperator
    {
        And,
        Or
    }
}