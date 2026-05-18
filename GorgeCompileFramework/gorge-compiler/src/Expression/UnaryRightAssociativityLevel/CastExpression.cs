#nullable enable
using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.UnaryRightAssociativityLevel
{
    /// <summary>
    /// 强制类型转换
    /// 要求操作数是int或float // TODO 应该支持int和枚举间的转换（未必？需要再想），以及不同object间的转换
    /// 结果与转换的目标类型一致
    /// </summary>
    public class CastExpression : BaseValueExpression
    {
        private readonly IGorgeValueExpression _operand;

        public CastExpression(SymbolicGorgeType castTo, IGorgeValueExpression operand, CodeBlockScope context,
            ParserRuleContext antlrContext) : base(context, antlrContext)
        {
            switch (castTo.BasicType)
            {
                case BasicType.Int:
                    switch (operand.ValueType.BasicType)
                    {
                        case BasicType.Int:
                        case BasicType.Float:
                            break;
                        case BasicType.Bool:
                        case BasicType.String:
                        case BasicType.Object:
                            throw new CastOperandWrongTypeException(operand.ValueType, castTo, antlrContext);
                        default:
                            throw new Exception("未知类型");
                    }

                    break;
                case BasicType.Float:
                    switch (operand.ValueType.BasicType)
                    {
                        case BasicType.Int:
                        case BasicType.Float:
                            break;
                        case BasicType.Bool:
                        case BasicType.String:
                        case BasicType.Object:
                            throw new CastOperandWrongTypeException(operand.ValueType, castTo, antlrContext);
                        default:
                            throw new Exception("未知类型");
                    }

                    break;
                case BasicType.Object:
                case BasicType.Interface:
                    if (!operand.ValueType.CanCastTo(castTo))
                    {
                        throw new CastOperandWrongTypeException(operand.ValueType, castTo, antlrContext);
                    }

                    break;
                case BasicType.Bool:
                case BasicType.Enum:
                case BasicType.String:

                default:
                    throw new Exception("未知类型");
            }

            ValueType = castTo;
            _operand = operand;
            IsCompileConstant = operand.IsCompileConstant;
            if (IsCompileConstant)
            {
                CompileConstantValue = castTo.BasicType switch
                {
                    BasicType.Int => (int) operand.CompileConstantValue,
                    BasicType.Float => (float) operand.CompileConstantValue,
                    BasicType.Object => (GorgeObject) operand.CompileConstantValue, // Object为常量可能只有null一种情况？
                    _ => throw new Exception("未知类型")
                };
            }
        }

        public override SymbolicGorgeType ValueType { get; }

        protected override SymbolicAddress AppendNotConstantCodes(CodeBlockScope codeBlockScope,
            List<IntermediateCode> existCodes)
        {
            // 子表达式返回地址
            var operandResultAddress = _operand.AppendCodes(Block, existCodes);

            // 相同类型强制转换无实际动作
            if (ValueType.Equals(_operand.ValueType))
            {
                return operandResultAddress;
            }

            var resultAddress = Block.AddTempVariable(ValueType);

            var code = ValueType.BasicType switch
            {
                BasicType.Int => _operand.ValueType.BasicType switch
                {
                    BasicType.Float => IntermediateCode.FloatCastToInt(resultAddress, (Address) operandResultAddress),
                    _ => throw new Exception("未知操作数类型")
                },
                BasicType.Float => _operand.ValueType.BasicType switch
                {
                    BasicType.Int => IntermediateCode.IntCastToFloat(resultAddress, (Address) operandResultAddress),
                    _ => throw new Exception("未知操作数类型")
                },
                BasicType.Object or BasicType.Interface => _operand.ValueType.BasicType switch
                {
                    BasicType.Object or BasicType.Interface => IntermediateCode.ObjectCastToObject(resultAddress,
                        (Address) operandResultAddress),
                    _ => throw new GorgeCompileException("未知操作数类型", ExpressionLocation)
                },
                _ => throw new GorgeCompileException("未知操作数类型", ExpressionLocation)
            };

            existCodes.Add(code);
            return resultAddress;
        }

        public override bool IsCompileConstant { get; }
        public override object CompileConstantValue { get; }
    }
}