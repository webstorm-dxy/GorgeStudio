using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Statement
{
    public class ContinueStatement : IStatement
    {
        private readonly List<LeaveBlockTarget> _targets;

        public ContinueStatement(List<LeaveBlockTarget> targets, CodeBlockScope block, ParserRuleContext antlrContext)
        {
            _targets = targets;
            Block = block;
            AntlrContext = antlrContext;
        }

        public CodeBlockScope Block { get; }

        public void AppendCodes(List<IntermediateCode> existCodes)
        {
            existCodes.Add(Block.RegisterContinueJump(_targets.ToArray()));
        }

        public ParserRuleContext AntlrContext { get; }
    }
}