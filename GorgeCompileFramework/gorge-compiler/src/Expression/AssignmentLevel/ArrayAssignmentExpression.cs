using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeCompiler.Expression.AssignmentTarget;
using Gorge.GorgeCompiler.Expression.Tools;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.AssignmentLevel
{
    /// <summary>
    /// 赋值
    /// 操作数类型必须与被赋值地址的类型一致，除了int可以复制给float，自动进行隐式类型转换
    /// 结果类型为被赋值地址的类型
    /// </summary>
    public class ArrayAssignmentExpression : DynamicValueExpression
    {
        private readonly IGorgeValueExpression _operand;
        private readonly AssignmentTargetExpression _assignTo;

        public ArrayAssignmentExpression(AssignmentTargetExpression assignTo, IGorgeValueExpression operand,
            CodeBlockScope context, ParserRuleContext expressionLocation) : base(context, expressionLocation)
        {
            var valueType = assignTo.ValueType.Assert<ArrayType>(assignTo.ExpressionLocation).ItemType;
            ValueType = valueType;
            if (!operand.ValueType.CanAutoCastTo(valueType))
            {
                throw new GorgeCompileException(
                    $"不能将类型{operand.ValueType.ToGorgeType()}自动转换为{valueType.ToGorgeType()}", operand.ExpressionLocation);
            }

            _operand = operand;
            _assignTo = assignTo;
        }

        public override SymbolicGorgeType ValueType { get; }

        public override SymbolicAddress AppendCodes(CodeBlockScope codeBlockScope, List<IntermediateCode> existCodes)
        {
            // 赋值结果地址
            var convertedAddress = CommonImmediateCodes.AppendAutoCastCode(Block,
                _operand.AppendCodes(codeBlockScope, existCodes),
                ValueType, existCodes);

            // 由于是AssignTo，上级返回是数组对象地址
            var assignToAddress = _assignTo.AppendCodes(codeBlockScope, existCodes);
            // 将int 0处压入index
            existCodes.Add(IntermediateCode.SetIntParameter(0, _assignTo.DynamicAccessorAddress));
            var index = ValueType is IntType ? 1 : 0;
            existCodes.Add(IntermediateCode.SetParameter(ValueType, index, (Address) convertedAddress));
            // 调用赋值方法
            existCodes.Add(IntermediateCode.InvokeMethod((Address) assignToAddress, 1));

            // 返回值等同于被赋值的值，所以返回赋值结果地址也可以
            return convertedAddress;
        }
    }
}