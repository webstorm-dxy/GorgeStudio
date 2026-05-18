using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Expression.Tools;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.UnaryLeftAssociativityLevel
{
    /// <summary>
    /// 数组访问表达式
    ///   a[b]
    /// </summary>
    public class ArrayAccessExpression : BaseValueExpression
    {
        private readonly IGorgeValueExpression _arrayObject;
        private readonly IGorgeValueExpression _index;
        public override SymbolicGorgeType ValueType { get; }

        public ArrayAccessExpression(IGorgeValueExpression arrayObject, IGorgeValueExpression index,
            CodeBlockScope block, ParserRuleContext antlrContext) : base(block, antlrContext)
        {
            _arrayObject = arrayObject;
            _index = index;

            if (_arrayObject.ValueType.BasicType is not BasicType.Object)
            {
                throw new Exception("无法对基本类型进行数组访问");
            }

            ValueType = _arrayObject.ValueType.Assert<ArrayType>(_arrayObject.ExpressionLocation).ItemType;
            IsCompileConstant = false;
            CompileConstantValue = null;
        }

        protected override SymbolicAddress AppendNotConstantCodes(CodeBlockScope codeBlockScope,
            List<IntermediateCode> existCodes)
        {
            CommonImmediateCodes.AppendArrayGet(ValueAddress, existCodes,
                (Address) _arrayObject.AppendCodes(Block, existCodes),
                (Address) _index.AppendCodes(Block, existCodes));
            return ValueAddress;
        }

        public override bool IsCompileConstant { get; }
        public override object CompileConstantValue { get; }
    }
}