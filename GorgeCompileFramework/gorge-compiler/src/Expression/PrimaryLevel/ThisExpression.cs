using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Block;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.PrimaryLevel
{
    public class ThisExpression : DynamicValueExpression
    {
        public override SymbolicGorgeType ValueType { get; }

        public ThisExpression(CodeBlockScope context, ParserRuleContext expressionLocation) : base(context, expressionLocation)
        {
            if (context.ContextType is BlockContextType.Constant or BlockContextType.Injector
                or BlockContextType.StaticMethod)
            {
                throw new Exception("无法在Static代码块中使用this关键字");
            }


            ValueType = context.ClassSymbol.Type;
        }

        public override SymbolicAddress AppendCodes(CodeBlockScope codeBlockScope, List<IntermediateCode> existCodes)
        {
            var address = Block.AddTempVariable(ValueType);
            existCodes.Add(IntermediateCode.LoadThis(address));
            return address;
        }
    }
}