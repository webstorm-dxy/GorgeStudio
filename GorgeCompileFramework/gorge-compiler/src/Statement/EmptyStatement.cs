using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Statement
{
    public class EmptyStatement : IStatement
    {
        public EmptyStatement(CodeBlockScope block, ParserRuleContext antlrContext)
        {
            Block = block;
            AntlrContext = antlrContext;
        }

        public CodeBlockScope Block { get; }

        public void AppendCodes(List<IntermediateCode> existCodes)
        {
        }

        public ParserRuleContext AntlrContext { get; }
    }
}