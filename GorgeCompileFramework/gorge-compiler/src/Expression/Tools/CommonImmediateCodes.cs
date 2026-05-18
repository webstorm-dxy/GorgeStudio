using System;
using System.Collections.Generic;
using System.Linq;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.Tools
{
    public static class CommonImmediateCodes
    {
        /// <summary>
        /// 计算并设置调用参数
        /// </summary>
        /// <param name="block"></param>
        /// <param name="parameterInformation"></param>
        /// <param name="genericsArguments"></param>
        /// <param name="argumentExpressions"></param>
        /// <param name="existCodes"></param>
        /// <exception cref="Exception"></exception>
        public static void SetInvocationArguments(CodeBlockScope block, ParameterSymbol[] parameterInformation,
            Dictionary<SymbolicGorgeType, SymbolicGorgeType> genericsArguments,
            IGorgeValueExpression[] argumentExpressions, List<IntermediateCode> existCodes)
        {
            // 此处必须先插入所有参数的准备代码，才能压参数，否则如果参数计算中存在调用，就会发生混乱
            var arguments = argumentExpressions.Select(expression => expression.AppendCodes(block, existCodes))
                .ToList();

            for (var i = 0; i < parameterInformation.Length; i++)
            {
                SymbolicGorgeType parameterType;
                // TODO 目前实现简单的泛型填入
                if (parameterInformation[i].Type is GenericsType)
                {
                    if (!genericsArguments.TryGetValue(parameterInformation[i].Type, out parameterType))
                    {
                        throw new Exception($"没有设置{parameterInformation[i].Type}的泛型参数");
                    }
                }
                else
                {
                    parameterType = parameterInformation[i].Type;
                }

                var parameterAddress = AppendAutoCastCode(block, arguments[i], parameterType, existCodes);
                var code = parameterInformation[i].Type.BasicType switch
                {
                    BasicType.Int or BasicType.Enum => IntermediateCode.SetIntParameter(parameterInformation[i].Index,
                        (Address) parameterAddress),
                    BasicType.Float => IntermediateCode.SetFloatParameter(parameterInformation[i].Index,
                        (Address) parameterAddress),
                    BasicType.Bool =>
                        IntermediateCode.SetBoolParameter(parameterInformation[i].Index, (Address) parameterAddress),
                    BasicType.String => IntermediateCode.SetStringParameter(parameterInformation[i].Index,
                        (Address) parameterAddress),
                    BasicType.Object or BasicType.Interface or BasicType.Delegate =>
                        IntermediateCode.SetObjectParameter(parameterInformation[i].Index,
                            (Address) parameterAddress),
                    _ => throw new Exception($"不支持该类型{parameterInformation[i].Type}")
                };

                existCodes.Add(code);
            }
        }

        /// <summary>
        /// 计算并设置调用参数
        /// </summary>
        /// <param name="codeBlockScope"></param>
        /// <param name="parameterTypes"></param>
        /// <param name="argumentExpressions"></param>
        /// <param name="existCodes"></param>
        /// <exception cref="Exception"></exception>
        public static void SetInvocationArguments(CodeBlockScope codeBlockScope, SymbolicGorgeType[] parameterTypes,
            IGorgeValueExpression[] argumentExpressions, List<IntermediateCode> existCodes)
        {
            // 此处必须先插入所有参数的准备代码，才能压参数，否则如果参数计算中存在调用，就会发生混乱
            var parameters = argumentExpressions
                .Select(expression => expression.AppendCodes(codeBlockScope, existCodes)).ToList();

            var parameterTypeCount = new TypeCount();

            for (var i = 0; i < parameterTypes.Length; i++)
            {
                var code = parameterTypes[i].BasicType switch
                {
                    BasicType.Int => IntermediateCode.SetIntParameter(
                        parameterTypeCount.Count(parameterTypes[i].BasicType), (Address) parameters[i]),
                    BasicType.Float => IntermediateCode.SetFloatParameter(
                        parameterTypeCount.Count(parameterTypes[i].BasicType), (Address) parameters[i]),
                    BasicType.Bool =>
                        IntermediateCode.SetBoolParameter(parameterTypeCount.Count(parameterTypes[i].BasicType),
                            (Address) parameters[i]),
                    BasicType.Enum => IntermediateCode.SetIntParameter(
                        parameterTypeCount.Count(parameterTypes[i].BasicType), (Address) parameters[i]),
                    BasicType.String => IntermediateCode.SetStringParameter(
                        parameterTypeCount.Count(parameterTypes[i].BasicType),
                        (Address) parameters[i]),
                    BasicType.Object => IntermediateCode.SetObjectParameter(
                        parameterTypeCount.Count(parameterTypes[i].BasicType),
                        (Address) parameters[i]),
                    _ => throw new Exception("不支持该类型")
                };

                existCodes.Add(code);
            }
        }

        /// <summary>
        /// 生成自动类型转换代码
        /// </summary>
        /// <param name="block">代码块上下文</param>
        /// <param name="addressToConvert">待转换变量地址</param>
        /// <param name="typeToConvertTo">转换目标类型</param>
        /// <param name="existCodes">追加代码表</param>
        /// <returns>转换后变量所在地址</returns>
        public static SymbolicAddress AppendAutoCastCode(CodeBlockScope block, SymbolicAddress addressToConvert,
            SymbolicGorgeType typeToConvertTo, List<IntermediateCode> existCodes)
        {
            if (!addressToConvert.Type.CanAutoCastTo(typeToConvertTo))
            {
                throw new Exception($"不能将{addressToConvert.Type}表达式类型隐式转换为{typeToConvertTo}地址类型");
            }

            // 相等情况下不转换
            if (addressToConvert.Type.Equals(typeToConvertTo))
            {
                return addressToConvert;
            }

            // int到float的隐式类型转换
            if (typeToConvertTo.BasicType is BasicType.Float && addressToConvert.Type.BasicType is BasicType.Int)
            {
                var convertedAddress = block.AddTempVariable(SymbolicGorgeType.Float);
                existCodes.Add(IntermediateCode.IntCastToFloat(convertedAddress, (Address) addressToConvert));
                return convertedAddress;
            }

            // null常量转null string
            if (typeToConvertTo.BasicType is BasicType.String && addressToConvert.Type.BasicType is BasicType.Object &&
                addressToConvert.Type is NullType)
            {
                var convertedAddress = block.AddTempVariable(SymbolicGorgeType.String);
                existCodes.Add(IntermediateCode.LocalAssign(convertedAddress, Immediate.String(null)));
                return convertedAddress;
            }

            // object间转换目前无指令，直接转地址类型
            return addressToConvert;
        }


        /// <summary>
        /// 调用Array的Get方法
        /// </summary>
        /// <param name="resultAddress"></param>
        /// <param name="existCodes"></param>
        /// <param name="arrayObject"></param>
        /// <param name="index"></param>
        public static void AppendArrayGet(Address resultAddress, List<IntermediateCode> existCodes,
            IOperand arrayObject, IOperand index)
        {
            // 布置index到0号参数
            existCodes.Add(IntermediateCode.SetIntParameter(0, index));

            // 调用0号方法
            existCodes.Add(IntermediateCode.InvokeMethod(arrayObject, 0));
            existCodes.Add(IntermediateCode.GetReturn(resultAddress, resultAddress.Type));
        }
    }
}