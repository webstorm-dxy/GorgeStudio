using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression
{
    /// <summary>
    /// 数组构造方法调用
    /// 如果所有参数和Injector均为编译时常量，则本表达式为编译时常量，值为对应的ClassCreator
    /// </summary>
    public class ArrayConstructorInvocationExpression : DynamicValueExpression
    {
        private readonly GorgeType _itemType;
        private readonly IGorgeValueExpression _listObject;
        private readonly IGorgeValueExpression _length;

        public ArrayConstructorInvocationExpression(SymbolicGorgeType itemType, IGorgeValueExpression listObject,
            IGorgeValueExpression length, CodeBlockScope block, ParserRuleContext expressionLocation) : base(block,
            expressionLocation)
        {
            _itemType = itemType;
            _listObject = listObject;
            _length = length;

            ValueType = SymbolicGorgeType.Array(itemType);
        }

        public override SymbolicGorgeType ValueType { get; }

        public override SymbolicAddress AppendCodes(CodeBlockScope codeBlockScope, List<IntermediateCode> existCodes)
        {
            var lengthAddress = _length.AppendCodes(Block, existCodes);
            var listObjectAddress = _listObject.AppendCodes(Block, existCodes);

            var newObjectAddress = Block.AddTempVariable(ValueType);
            var code = _itemType.BasicType switch
            {
                BasicType.Int or BasicType.Enum => IntermediateCode.InvokeIntArrayConstructor((Address) lengthAddress,
                    (Address) listObjectAddress),
                BasicType.Float => IntermediateCode.InvokeFloatArrayConstructor((Address) lengthAddress,
                    (Address) listObjectAddress),
                BasicType.Bool => IntermediateCode.InvokeBoolArrayConstructor((Address) lengthAddress,
                    (Address) listObjectAddress),
                BasicType.String => IntermediateCode.InvokeStringArrayConstructor((Address) lengthAddress,
                    (Address) listObjectAddress),
                BasicType.Object or BasicType.Interface => IntermediateCode.InvokeObjectArrayConstructor(
                    (Address) lengthAddress,
                    (Address) listObjectAddress),
                _ => throw new Exception("该类序列Injector尚未完成")
            };
            existCodes.Add(code);
            existCodes.Add(IntermediateCode.GetReturn(newObjectAddress, newObjectAddress.Type));


            return newObjectAddress;
        }
    }
}