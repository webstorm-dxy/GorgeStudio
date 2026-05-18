#nullable enable
using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeCompiler.Expression.Tools;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.EqualityLevel
{
    /// <summary>
    /// 相等：等于，不等于
    /// 要求操作数是int、float、bool
    /// int和float之间可比，提供隐式类型转换，其他比较要求类型相同
    /// TODO 理应支持全类型，但是暂缓
    /// 结果是bool
    /// </summary>
    public class EqualityExpression : BaseValueExpression
    {
        private readonly EqualityOperator _equalityOperator;
        private readonly IGorgeValueExpression _left;
        private readonly IGorgeValueExpression _right;
        private readonly SymbolicGorgeType _expressionType; // 表达式类型，Int或Float，用于强制类型转换和确定运算符

        public EqualityExpression(EqualityOperator equalityOperator, IGorgeValueExpression left,
            IGorgeValueExpression right,
            CodeBlockScope context, ParserRuleContext antlrContext) : base(context, antlrContext)
        {
            if (left.ValueType.CanAutoCastTo(right.ValueType))
            {
                _expressionType = right.ValueType;
            }
            else if (right.ValueType.CanAutoCastTo(left.ValueType))
            {
                _expressionType = left.ValueType;
            }
            else
            {
                throw new GorgeCompileException("相等表达式的两操作数类型不相同或无法互相转换",antlrContext);
            }

            ValueType = SymbolicGorgeType.Bool;
            _equalityOperator = equalityOperator;
            _left = left;
            _right = right;
            IsCompileConstant = left.IsCompileConstant && right.IsCompileConstant;
            if (IsCompileConstant)
            {
                CompileConstantValue = equalityOperator switch
                {
                    EqualityOperator.Equality => left.CompileConstantValue == right.CompileConstantValue,
                    EqualityOperator.Inequality => left.CompileConstantValue != right.CompileConstantValue,
                    _ => throw new Exception("未知运算符")
                };
            }
        }

        public override SymbolicGorgeType ValueType { get; }

        protected override SymbolicAddress AppendNotConstantCodes(CodeBlockScope codeBlockScope,
            List<IntermediateCode> existCodes)
        {
            // 操作数地址
            var leftAddress =
                CommonImmediateCodes.AppendAutoCastCode(Block, _left.AppendCodes(Block, existCodes), _expressionType,
                    existCodes);
            var rightAddress = CommonImmediateCodes.AppendAutoCastCode(Block, _right.AppendCodes(Block, existCodes),
                _expressionType, existCodes);

            var code = _expressionType.BasicType switch
            {
                BasicType.Int or BasicType.Enum => _equalityOperator switch
                {
                    EqualityOperator.Equality => IntermediateCode.IntEquality(ValueAddress, (Address) leftAddress,
                        (Address) rightAddress),
                    EqualityOperator.Inequality => IntermediateCode.IntInequality(ValueAddress, (Address) leftAddress,
                        (Address) rightAddress),
                    _ => throw new Exception("未知运算符")
                },
                BasicType.Float => _equalityOperator switch
                {
                    EqualityOperator.Equality => IntermediateCode.FloatEquality(ValueAddress, (Address) leftAddress,
                        (Address) rightAddress),
                    EqualityOperator.Inequality => IntermediateCode.FloatInequality(ValueAddress, (Address) leftAddress,
                        (Address) rightAddress),
                    _ => throw new Exception("未知运算符")
                },
                BasicType.Bool => _equalityOperator switch
                {
                    EqualityOperator.Equality =>
                        IntermediateCode.BoolEquality(ValueAddress, (Address) leftAddress, (Address) rightAddress),
                    EqualityOperator.Inequality => IntermediateCode.BoolInequality(ValueAddress, (Address) leftAddress,
                        (Address) rightAddress),
                    _ => throw new Exception("未知运算符")
                },
                BasicType.String => _equalityOperator switch
                {
                    EqualityOperator.Equality => IntermediateCode.StringEquality(ValueAddress, (Address) leftAddress,
                        (Address) rightAddress),
                    EqualityOperator.Inequality => IntermediateCode.StringInequality(ValueAddress,
                        (Address) leftAddress,
                        (Address) rightAddress),
                    _ => throw new Exception("未知运算符")
                },
                BasicType.Object or BasicType.Delegate or BasicType.Interface => _equalityOperator switch
                {
                    EqualityOperator.Equality => IntermediateCode.ObjectEquality(ValueAddress, (Address) leftAddress,
                        (Address) rightAddress),
                    EqualityOperator.Inequality => IntermediateCode.ObjectInequality(ValueAddress,
                        (Address) leftAddress,
                        (Address) rightAddress),
                    _ => throw new Exception("未知运算符")
                },
                _ => throw new Exception("未知结果类型")
            };

            existCodes.Add(code);

            return ValueAddress;
        }

        public override bool IsCompileConstant { get; }
        public override object CompileConstantValue { get; }
    }

    public enum EqualityOperator
    {
        Equality,
        Inequality
    }
}