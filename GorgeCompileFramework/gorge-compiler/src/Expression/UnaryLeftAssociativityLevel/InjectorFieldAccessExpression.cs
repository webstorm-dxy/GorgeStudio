using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.UnaryLeftAssociativityLevel
{
    /// <summary>
    /// 字段访问表达式（非赋值号左侧）
    ///   a.b
    /// 获取字段值并装载到临时地址中
    /// 返回字段所在临时地址
    /// </summary>
    public class InjectorFieldAccessExpression : DynamicValueExpression
    {
        /// <summary>
        /// 字段所在类
        /// </summary>
        private readonly IGorgeValueExpression _injectorReferenceOperand;

        public override SymbolicGorgeType ValueType { get; }

        /// <summary>
        /// 字段索引
        /// </summary>
        private readonly int _fieldIndex;

        public InjectorFieldAccessExpression(IGorgeValueExpression injectorReferenceOperand, InjectorFieldSymbol field,
            CodeBlockScope block, ParserRuleContext expressionLocation) : base(block, expressionLocation)
        {
            _injectorReferenceOperand = injectorReferenceOperand;

            _fieldIndex = field.Index;

            ValueType = field.FieldType;
        }

        public override SymbolicAddress AppendCodes(CodeBlockScope codeBlockScope, List<IntermediateCode> existCodes)
        {
            var injectorAddress = _injectorReferenceOperand.AppendCodes(Block, existCodes);
            existCodes.Add(IntermediateCode.LoadInjectorField(ValueAddress, (Address) injectorAddress, _fieldIndex));
            return ValueAddress;
        }
    }
}