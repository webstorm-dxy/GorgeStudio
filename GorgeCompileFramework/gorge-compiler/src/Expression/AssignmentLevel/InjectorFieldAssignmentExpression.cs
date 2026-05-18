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
    /// 注入器字段赋值
    /// 操作数类型必须与被赋值地址的类型一致，除了int可以复制给float，自动进行隐式类型转换
    /// 结果类型为被赋值地址的类型
    /// </summary>
    public class InjectorFieldAssignmentExpression : DynamicValueExpression
    {
        private readonly IGorgeValueExpression _operand;
        private readonly AssignmentTargetExpression _assignTo;

        public InjectorFieldAssignmentExpression(AssignmentTargetExpression assignTo, IGorgeValueExpression operand,
            CodeBlockScope context, ParserRuleContext expressionLocation) : base(context, expressionLocation)
        {
            // 类型检查
            if (!operand.ValueType.CanAutoCastTo(assignTo.AssignType))
            {
                throw new GorgeCompileException($"赋值类型错误，待赋值地址类型是{assignTo.AssignType}，操作数类型是{operand.ValueType}",
                    expressionLocation);
            }

            ValueType = assignTo.AssignType;
            _operand = operand;
            _assignTo = assignTo;
        }

        public override SymbolicGorgeType ValueType { get; }

        public override SymbolicAddress AppendCodes(CodeBlockScope codeBlockScope, List<IntermediateCode> existCodes)
        {
            // 赋值结果地址
            var convertedAddress = CommonImmediateCodes.AppendAutoCastCode(Block,
                _operand.AppendCodes(Block, existCodes),
                _assignTo.AssignType, existCodes);

            // 由于是AssignTo，上级返回是父对象地址
            var assignToAddress = _assignTo.AppendCodes(Block, existCodes);

            var code = IntermediateCode.SetInjectorField(_assignTo.AssignType, assignToAddress, _assignTo.FieldIndex,
                (Address)convertedAddress);

            existCodes.Add(code);

            // 返回值等同于被赋值的值，所以返回赋值结果地址也可以
            return convertedAddress;
        }
    }
}