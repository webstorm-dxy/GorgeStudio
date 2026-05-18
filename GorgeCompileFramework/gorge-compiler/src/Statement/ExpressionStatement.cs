using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Expression;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Statement
{
    /// <summary>
    /// 单表达式语句
    /// 目前允许所有表达式独立成句，可能后续考虑拒绝没有赋值和调用行为的表达式独立成句
    /// </summary>
    public class ExpressionStatement : IStatement
    {
        private readonly IGorgeValueExpression _expression;

        public CodeBlockScope Block { get; }

        public ExpressionStatement(IGorgeValueExpression expression, CodeBlockScope block,
            ParserRuleContext antlrContext)
        {
            _expression = expression;
            Block = block;
            AntlrContext = antlrContext;
        }

        public void AppendCodes(List<IntermediateCode> existCodes)
        {
            _expression.AppendCodes(Block, existCodes);
        }

        public ParserRuleContext AntlrContext { get; }
    }
}