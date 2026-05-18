using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Expression.Tools;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.AssignmentTarget
{
    /// <summary>
    /// 字段访问表达式（赋值号左侧）
    ///   a.b
    /// 获取待赋值字段所在对象并装载到临时地址中
    /// 返回待赋值字段所在对象所在临时地址
    /// 表达式返回类型为待赋值字段的类型
    /// </summary>
    public class FieldAccess : AssignmentTargetExpression
    {
        private readonly AssignmentTargetExpression _objectOperand;
        public override SymbolicGorgeType ValueType { get; }

        public override int FieldIndex { get; }
        public override AssignmentTargetType AssignmentTargetType { get; }
        public override SymbolicGorgeType AssignType { get; }
        public override Address DynamicAccessorAddress => default;

        public FieldAccess(AssignmentTargetExpression objectOperand, FieldSymbol field,
            CodeBlockScope block, ParserRuleContext antlrContext) : base(block, antlrContext)
        {
            _objectOperand = objectOperand;
            FieldIndex = field.Index;
            AssignmentTargetType = AssignmentTargetType.Field;
            ValueType = objectOperand.AssignType;
            AssignType = field.Type;
        }

        protected override SymbolicAddress AppendNotConstantCodes(CodeBlockScope codeBlockScope,
            List<IntermediateCode> existCodes)
        {
            var operandAddress = _objectOperand.AppendCodes(Block, existCodes);

            switch (_objectOperand.AssignmentTargetType)
            {
                case AssignmentTargetType.This:
                case AssignmentTargetType.LocalVariable:
                    return operandAddress;
                case AssignmentTargetType.Field:
                    // 取出本级字段所在对象
                    existCodes.Add(IntermediateCode.LoadObjectField(ValueAddress, (Address)operandAddress,
                        _objectOperand.FieldIndex));
                    return ValueAddress;
                case AssignmentTargetType.InjectorField:
                    // 取出本级字段所在对象
                    existCodes.Add(IntermediateCode.LoadInjectorField(ValueAddress, (Address)operandAddress,
                        _objectOperand.FieldIndex));
                    return ValueAddress;
                case AssignmentTargetType.Array:
                    // 取出本级字段所在对象
                    CommonImmediateCodes.AppendArrayGet(ValueAddress, existCodes, (Address)operandAddress,
                        _objectOperand.DynamicAccessorAddress);
                    return ValueAddress;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}