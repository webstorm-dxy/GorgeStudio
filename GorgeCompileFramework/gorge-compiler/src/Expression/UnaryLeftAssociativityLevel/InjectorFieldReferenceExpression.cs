using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.UnaryLeftAssociativityLevel
{
    /// <summary>
    /// this字段引用表达式，把地址值封装成一个表达式对象
    /// 如果设置为IsAssignTo，则返回被赋值对象的地址，并在Metadata中加入FieldIndex
    /// </summary>
    public class InjectorFieldReferenceExpression : DynamicValueExpression
    {
        private readonly int _fieldIndex;

        public InjectorFieldReferenceExpression(InjectorFieldSymbol field, CodeBlockScope context,
            ParserRuleContext expressionLocation) : base(context, expressionLocation)
        {
            ValueType = field.FieldType;

            _fieldIndex = field.Index;
        }

        public override SymbolicGorgeType ValueType { get; }

        public override SymbolicAddress AppendCodes(CodeBlockScope codeBlockScope, List<IntermediateCode> existCodes)
        {
            // TODO 默认Injector在0号位，这个可能后续需要通过Block上下文传递
            var injectorAddress = new Address()
            {
                Type = GorgeType.Object("Injector"),
                Index = 0
            };
            var address = Block.AddTempVariable(ValueType);
            var code = ValueType.BasicType switch
            {
                BasicType.Int or BasicType.Enum => IntermediateCode.LoadIntInjectorField(address, injectorAddress,
                    _fieldIndex),
                BasicType.Float =>
                    IntermediateCode.LoadFloatInjectorField(address, injectorAddress, _fieldIndex),
                BasicType.Bool => IntermediateCode.LoadBoolInjectorField(address, injectorAddress, _fieldIndex),
                BasicType.String => IntermediateCode.LoadStringInjectorField(address, injectorAddress,
                    _fieldIndex),
                BasicType.Object => IntermediateCode.LoadObjectInjectorField(
                    address, injectorAddress,
                    _fieldIndex),
                _ => throw new Exception("不支持引用该类型字段")
            };
            existCodes.Add(code);
            return address;
        }
    }
}