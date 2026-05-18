using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.AssignmentTarget
{
    /// <summary>
    /// 本类字段引用表达式（赋值号左侧）
    /// 获取待赋值字段所在对象并装载到临时地址中
    /// 返回待赋值字段所在对象所在临时地址
    /// Metadata中的FieldIndex存储待赋值字段的索引
    /// 表达式返回类型为待赋值字段的类型
    /// </summary>
    public class FieldReference : AssignmentTargetExpression
    {
        public FieldReference(IFieldSymbol field, CodeBlockScope context, ParserRuleContext antlrContext) :
            base(
                context, antlrContext)
        {
            ValueType = field.Type;

            FieldIndex = field.Index;
            AssignmentTargetType = AssignmentTargetType.Field;

            AssignType = field.Type;
        }

        public override SymbolicGorgeType ValueType { get; }

        protected override SymbolicAddress AppendNotConstantCodes(CodeBlockScope codeBlockScope,
            List<IntermediateCode> existCodes)
        {
            var result = Block.AddTempVariable(Block.ClassSymbol.Type);
            existCodes.Add(IntermediateCode.LoadThis(result));
            return result;
        }

        public override int FieldIndex { get; }
        public override AssignmentTargetType AssignmentTargetType { get; }
        public override SymbolicGorgeType AssignType { get; }
        public override Address DynamicAccessorAddress => default;
    }
}