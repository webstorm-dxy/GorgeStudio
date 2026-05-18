using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeCompiler.Expression.Tools;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.ComparisonLevel
{
    /// <summary>
    /// 比较：大于小于，大等于小等于
    /// 要求操作数是int或float
    /// 结果是bool
    /// </summary>
    public class ComparisonExpression : BaseValueExpression
    {
        private readonly ComparisonOperator _comparisionOperator;
        private readonly IGorgeValueExpression _left;
        private readonly IGorgeValueExpression _right;
        private readonly SymbolicGorgeType _expressionType; // 表达式类型，Int或Float，用于强制类型转换和确定运算符

        public ComparisonExpression(ComparisonOperator comparisionOperator, IGorgeValueExpression left,
            IGorgeValueExpression right,
            CodeBlockScope context, ParserRuleContext antlrContext) : base(context, antlrContext)
        {
            // 判定表达式类型并执行类型检查
            switch (left.ValueType.BasicType)
            {
                case BasicType.Int:
                    _expressionType = right.ValueType.BasicType switch
                    {
                        BasicType.Int => SymbolicGorgeType.Int,
                        BasicType.Float => SymbolicGorgeType.Float,
                        _ => throw new GorgeCompileException(
                            $"操作符{comparisionOperator}的右操作数类型必须为int或float，但实际类型为{right.ValueType}",antlrContext)
                    };
                    break;
                case BasicType.Float:
                    _expressionType = right.ValueType.BasicType switch
                    {
                        BasicType.Int or BasicType.Float => SymbolicGorgeType.Float,
                        _ => throw new GorgeCompileException(
                            $"操作符{comparisionOperator}的右操作数类型必须为int或float，但实际类型为{right.ValueType}",antlrContext)
                    };

                    break;
                default:
                    throw new GorgeCompileException($"操作符{comparisionOperator}的左操作数类型必须为int或float，但实际类型为{left.ValueType}",antlrContext);
            }

            ValueType = SymbolicGorgeType.Bool;
            _comparisionOperator = comparisionOperator;
            _left = left;
            _right = right;
            IsCompileConstant = left.IsCompileConstant && right.IsCompileConstant;
            if (IsCompileConstant)
            {
                switch (comparisionOperator)
                {
                    case ComparisonOperator.Less:
                        if (left.ValueType.BasicType == BasicType.Int &&
                            right.ValueType.BasicType == BasicType.Int)
                        {
                            CompileConstantValue =
                                (int) left.CompileConstantValue < (int) right.CompileConstantValue;
                        }
                        else
                        {
                            CompileConstantValue =
                                (float) left.CompileConstantValue < (float) right.CompileConstantValue;
                        }

                        break;
                    case ComparisonOperator.Greater:
                        if (left.ValueType.BasicType == BasicType.Int &&
                            right.ValueType.BasicType == BasicType.Int)
                        {
                            CompileConstantValue =
                                (int) left.CompileConstantValue > (int) right.CompileConstantValue;
                        }
                        else
                        {
                            CompileConstantValue =
                                (float) left.CompileConstantValue > (float) right.CompileConstantValue;
                        }

                        break;
                    case ComparisonOperator.LessEqual:
                        if (left.ValueType.BasicType == BasicType.Int &&
                            right.ValueType.BasicType == BasicType.Int)
                        {
                            CompileConstantValue =
                                (int) left.CompileConstantValue <= (int) right.CompileConstantValue;
                        }
                        else
                        {
                            CompileConstantValue =
                                (float) left.CompileConstantValue <= (float) right.CompileConstantValue;
                        }

                        break;
                    case ComparisonOperator.GreaterEqual:
                        if (left.ValueType.BasicType == BasicType.Int &&
                            right.ValueType.BasicType == BasicType.Int)
                        {
                            CompileConstantValue =
                                (int) left.CompileConstantValue >= (int) right.CompileConstantValue;
                        }
                        else
                        {
                            CompileConstantValue =
                                (float) left.CompileConstantValue >= (float) right.CompileConstantValue;
                        }

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
            // 操作数地址和类型转换
            var leftAddress =
                CommonImmediateCodes.AppendAutoCastCode(Block, _left.AppendCodes(Block, existCodes), _expressionType,
                    existCodes);
            var rightAddress = CommonImmediateCodes.AppendAutoCastCode(Block, _right.AppendCodes(Block, existCodes),
                _expressionType, existCodes);

            var resultAddress = Block.AddTempVariable(ValueType);
            var code = _expressionType.BasicType switch
            {
                BasicType.Int => _comparisionOperator switch
                {
                    ComparisonOperator.Less => IntermediateCode.IntLess(resultAddress, (Address) leftAddress,
                        (Address) rightAddress),
                    ComparisonOperator.Greater => IntermediateCode.IntGreater(resultAddress, (Address) leftAddress,
                        (Address) rightAddress),
                    ComparisonOperator.LessEqual => IntermediateCode.IntLessEqual(resultAddress, (Address) leftAddress,
                        (Address) rightAddress),
                    ComparisonOperator.GreaterEqual => IntermediateCode.IntGreaterEqual(resultAddress,
                        (Address) leftAddress,
                        (Address) rightAddress),
                    _ => throw new Exception("未知运算符")
                },
                BasicType.Float => _comparisionOperator switch
                {
                    ComparisonOperator.Less => IntermediateCode.FloatLess(resultAddress, (Address) leftAddress,
                        (Address) rightAddress),
                    ComparisonOperator.Greater => IntermediateCode.FloatGreater(resultAddress, (Address) leftAddress,
                        (Address) rightAddress),
                    ComparisonOperator.LessEqual => IntermediateCode.FloatLessEqual(resultAddress,
                        (Address) leftAddress,
                        (Address) rightAddress),
                    ComparisonOperator.GreaterEqual => IntermediateCode.FloatGreaterEqual(resultAddress,
                        (Address) leftAddress,
                        (Address) rightAddress),
                    _ => throw new Exception("未知运算符")
                },
                _ => throw new Exception("未知结果类型")
            };

            existCodes.Add(code);
            return resultAddress;
        }

        public override bool IsCompileConstant { get; }
        public override object CompileConstantValue { get; }
    }

    public enum ComparisonOperator
    {
        Less,
        Greater,
        LessEqual,
        GreaterEqual
    }
}