using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeCompiler.Expression.Tools;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.AdditionLevel
{
    /// <summary>
    /// 运算：加减乘除模
    /// 要求操作数是int或float
    /// 如果两操作数都是int，则结果是int，否则是float
    /// </summary>
    public class CalculateExpression : BaseValueExpression
    {
        private readonly CalculateOperator _calculateOperator;
        private readonly IGorgeValueExpression _left;
        private readonly IGorgeValueExpression _right;

        public CalculateExpression(CalculateOperator calculateOperator, IGorgeValueExpression left,
            IGorgeValueExpression right,
            CodeBlockScope context, ParserRuleContext antlrContext) : base(context, antlrContext)
        {
            // 判定返回值类型并执行类型检查
            switch (left.ValueType.BasicType)
            {
                case BasicType.Int:
                    ValueType = right.ValueType.BasicType switch
                    {
                        BasicType.Int => SymbolicGorgeType.Int,
                        BasicType.Float => SymbolicGorgeType.Float,
                        _ => throw new GorgeCompileException($"操作符{calculateOperator}的右操作数类型必须为int或float，但实际类型为{right.ValueType}",antlrContext)
                    };
                    break;
                case BasicType.Float:
                    ValueType = right.ValueType.BasicType switch
                    {
                        BasicType.Int or BasicType.Float => SymbolicGorgeType.Float,
                        _ => throw new GorgeCompileException($"操作符{calculateOperator}的右操作数类型必须为int或float，但实际类型为{right.ValueType}",antlrContext)
                    };
                    break;
                default:
                    throw new GorgeCompileException($"操作符{calculateOperator}的左操作数类型必须为int或float，但实际类型为{left.ValueType}",antlrContext);
            }

            _calculateOperator = calculateOperator;
            _left = left;
            _right = right;
            IsCompileConstant = left.IsCompileConstant && right.IsCompileConstant;
            if (IsCompileConstant)
            {
                switch (calculateOperator)
                {
                    case CalculateOperator.Subtraction:
                        if (left.ValueType.BasicType == BasicType.Int &&
                            right.ValueType.BasicType == BasicType.Int)
                        {
                            CompileConstantValue =
                                (int) left.CompileConstantValue - (int) right.CompileConstantValue;
                        }
                        else
                        {
                            CompileConstantValue = Convert.ToSingle(left.CompileConstantValue)
                                                   - Convert.ToSingle(right.CompileConstantValue);
                        }

                        break;
                    case CalculateOperator.Multiplication:
                        if (left.ValueType.BasicType == BasicType.Int &&
                            right.ValueType.BasicType == BasicType.Int)
                        {
                            CompileConstantValue =
                                (int) left.CompileConstantValue * (int) right.CompileConstantValue;
                        }
                        else
                        {
                            CompileConstantValue = Convert.ToSingle(left.CompileConstantValue)
                                                   * Convert.ToSingle(right.CompileConstantValue);
                        }

                        break;
                    case CalculateOperator.Division:
                        if (left.ValueType.BasicType == BasicType.Int &&
                            right.ValueType.BasicType == BasicType.Int)
                        {
                            CompileConstantValue =
                                (int) left.CompileConstantValue / (int) right.CompileConstantValue;
                        }
                        else
                        {
                            CompileConstantValue = Convert.ToSingle(left.CompileConstantValue)
                                                   / Convert.ToSingle(right.CompileConstantValue);
                        }

                        break;
                    case CalculateOperator.Remainder:
                        if (left.ValueType.BasicType == BasicType.Int &&
                            right.ValueType.BasicType == BasicType.Int)
                        {
                            CompileConstantValue =
                                (int) left.CompileConstantValue % (int) right.CompileConstantValue;
                        }
                        else
                        {
                            CompileConstantValue = Convert.ToSingle(left.CompileConstantValue)
                                                   % Convert.ToSingle(right.CompileConstantValue);
                        }

                        break;
                    default:
                        throw new Exception("未知运算符");
                }
            }
        }

        public override SymbolicGorgeType ValueType { get; }

        protected override SymbolicAddress AppendNotConstantCodes(CodeBlockScope codeBlockScope,
            List<IntermediateCode> existCodes)
        {
            // 操作数地址
            var leftAddress =
                CommonImmediateCodes.AppendAutoCastCode(Block, _left.AppendCodes(codeBlockScope, existCodes), ValueType,
                    existCodes);
            var rightAddress =
                CommonImmediateCodes.AppendAutoCastCode(Block, _right.AppendCodes(codeBlockScope, existCodes),
                    ValueType, existCodes);

            // 隐式强制类型转换
            var resultAddress = Block.AddTempVariable(ValueType);

            // 运算操作
            var code = resultAddress.Type.BasicType switch
            {
                BasicType.Int => _calculateOperator switch
                {
                    CalculateOperator.Subtraction => IntermediateCode.IntSubtraction(resultAddress,
                        (Address) leftAddress,
                        (Address) rightAddress),
                    CalculateOperator.Multiplication => IntermediateCode.IntMultiplication(resultAddress,
                        (Address) leftAddress, (Address) rightAddress),
                    CalculateOperator.Division => IntermediateCode.IntDivision(resultAddress, (Address) leftAddress,
                        (Address) rightAddress),
                    CalculateOperator.Remainder => IntermediateCode.IntRemainder(resultAddress, (Address) leftAddress,
                        (Address) rightAddress),
                    _ => throw new Exception("未知运算符")
                },
                BasicType.Float => _calculateOperator switch
                {
                    CalculateOperator.Subtraction => IntermediateCode.FloatSubtraction(resultAddress,
                        (Address) leftAddress,
                        (Address) rightAddress),
                    CalculateOperator.Multiplication => IntermediateCode.FloatMultiplication(resultAddress,
                        (Address) leftAddress, (Address) rightAddress),
                    CalculateOperator.Division => IntermediateCode.FloatDivision(resultAddress, (Address) leftAddress,
                        (Address) rightAddress),
                    CalculateOperator.Remainder => IntermediateCode.FloatRemainder(resultAddress, (Address) leftAddress,
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

    public enum CalculateOperator
    {
        Subtraction,
        Multiplication,
        Division,
        Remainder
    }
}