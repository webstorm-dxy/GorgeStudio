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
    public class AdditionExpression : BaseValueExpression
    {
        private readonly IGorgeValueExpression _left;
        private readonly IGorgeValueExpression _right;

        public AdditionExpression(IGorgeValueExpression left, IGorgeValueExpression right, CodeBlockScope block,
            ParserRuleContext antlrContext) : base(block, antlrContext)
        {
            // 判定返回值类型并执行类型检查
            switch (left.ValueType.BasicType)
            {
                case BasicType.Int:
                    ValueType = right.ValueType.BasicType switch
                    {
                        BasicType.Int => SymbolicGorgeType.Int,
                        BasicType.Float => SymbolicGorgeType.Float,
                        BasicType.String => SymbolicGorgeType.String,
                        _ => throw new ExpressionOperandWrongTypeException("加法", "右", right.ValueType.BasicType,
                            BasicType.Int,
                            BasicType.Float, BasicType.String)
                    };
                    break;
                case BasicType.Float:
                    ValueType = right.ValueType.BasicType switch
                    {
                        BasicType.Int or BasicType.Float => SymbolicGorgeType.Float,
                        BasicType.String => SymbolicGorgeType.String,
                        _ => throw new ExpressionOperandWrongTypeException("加法", "右", right.ValueType.BasicType,
                            BasicType.Int,
                            BasicType.Float, BasicType.String)
                    };
                    break;
                case BasicType.String:
                    ValueType = right.ValueType.BasicType switch
                    {
                        BasicType.Int or BasicType.Float or BasicType.String or BasicType.Bool => SymbolicGorgeType
                            .String,
                        _ => throw new ExpressionOperandWrongTypeException("加法", "右", right.ValueType.BasicType,
                            BasicType.Int,
                            BasicType.Float, BasicType.String)
                    };
                    break;
                default:
                    throw new ExpressionOperandWrongTypeException("加法", "右", right.ValueType.BasicType, BasicType.Int,
                        BasicType.Float, BasicType.String);
            }

            _left = left;
            _right = right;
            var isCompileConstant = left.IsCompileConstant && right.IsCompileConstant;
            IsCompileConstant = isCompileConstant;
            if (isCompileConstant)
            {
                if (left.ValueType.BasicType is BasicType.String ||
                    right.ValueType.BasicType is BasicType.String) // 字符串拼接
                {
                    CompileConstantValue =
                        (string) left.CompileConstantValue + (string) right.CompileConstantValue;
                }
                else // 数值计算
                {
                    if (left.ValueType.BasicType == BasicType.Int &&
                        right.ValueType.BasicType == BasicType.Int)
                    {
                        CompileConstantValue =
                            (int) left.CompileConstantValue + (int) right.CompileConstantValue;
                    }
                    else
                    {
                        CompileConstantValue = Convert.ToSingle(left.CompileConstantValue) +
                                               Convert.ToSingle(right.CompileConstantValue);
                    }
                }
            }
        }

        public override SymbolicGorgeType ValueType { get; }

        protected override SymbolicAddress AppendNotConstantCodes(CodeBlockScope codeBlockScope,
            List<IntermediateCode> existCodes)
        {
            // 子表达式类型转换
            var leftAddress = AppendCastCode(_left.AppendCodes(codeBlockScope, existCodes), ValueType, existCodes);
            var rightAddress = AppendCastCode(_right.AppendCodes(codeBlockScope, existCodes), ValueType, existCodes);

            // 运算操作
            var resultAddress = Block.AddTempVariable(ValueType);
            var code = resultAddress.Type.BasicType switch
            {
                BasicType.Int => IntermediateCode.IntAddition(resultAddress, (Address) leftAddress,
                    (Address) rightAddress),
                BasicType.Float => IntermediateCode.FloatAddition(resultAddress, (Address) leftAddress,
                    (Address) rightAddress),
                BasicType.String => IntermediateCode.StringAddition(resultAddress, (Address) leftAddress,
                    (Address) rightAddress),
                _ => throw new Exception("未知结果类型")
            };

            existCodes.Add(code);
            return resultAddress;
        }

        /// <summary>
        /// 自动生成类型转换代码
        /// 在默认自动转换的基础上，增加int,float,bool到string的转换
        /// </summary>
        /// <param name="addressToConvert"></param>
        /// <param name="typeToConvertTo"></param>
        /// <param name="existCodes"></param>
        /// <returns></returns>
        private SymbolicAddress AppendCastCode(SymbolicAddress addressToConvert, SymbolicGorgeType typeToConvertTo,
            List<IntermediateCode> existCodes)
        {
            try
            {
                return CommonImmediateCodes.AppendAutoCastCode(Block, addressToConvert, typeToConvertTo, existCodes);
            }
            catch (Exception)
            {
                // int到string的自动转换
                if (typeToConvertTo.BasicType is BasicType.String && addressToConvert.Type.BasicType is BasicType.Int)
                {
                    var convertedAddress = Block.AddTempVariable(SymbolicGorgeType.String);
                    existCodes.Add(IntermediateCode.IntCastToString(convertedAddress, (Address) addressToConvert));
                    return convertedAddress;
                }

                // float到string的自动转换
                if (typeToConvertTo.BasicType is BasicType.String && addressToConvert.Type.BasicType is BasicType.Float)
                {
                    var convertedAddress = Block.AddTempVariable(SymbolicGorgeType.String);
                    existCodes.Add(IntermediateCode.FloatCastToString(convertedAddress, (Address) addressToConvert));
                    return convertedAddress;
                }

                // bool到string的自动转换
                if (typeToConvertTo.BasicType is BasicType.String && addressToConvert.Type.BasicType is BasicType.Bool)
                {
                    var convertedAddress = Block.AddTempVariable(SymbolicGorgeType.String);
                    existCodes.Add(IntermediateCode.BoolCastToString(convertedAddress, (Address) addressToConvert));
                    return convertedAddress;
                }

                throw new Exception($"不能将{addressToConvert.Type}表达式类型隐式转换为{typeToConvertTo}地址类型");
            }
        }

        public override bool IsCompileConstant { get; }
        public override object CompileConstantValue { get; }
    }
}