using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CodeBlock;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Statement
{
    public class BlockListStatement : IStatement
    {
        private readonly List<ICodeBlock> _innerBlocks;

        public CodeBlockScope Block { get; }

        public BlockListStatement(List<ICodeBlock> innerBlocks, CodeBlockScope block, ParserRuleContext antlrContext)
        {
            _innerBlocks = innerBlocks;
            Block = block;
            AntlrContext = antlrContext;
        }

        public void AppendCodes(List<IntermediateCode> existCodes)
        {
            foreach (var block in _innerBlocks)
            {
                block?.AppendCodes(existCodes);
            }

            // 补break和continue离块出口，以便和后续代码顺接
            // 这里的做法假定了后方不会立刻接一个新的block
            // 从编译的角度确实会优先识别为同blockList，而不是第二个BlockList语句
            // 有没有更预防性的做法，比如块负责添加出口而非入口
            existCodes.Add(IntermediateCode.Nop());
            existCodes.Add(IntermediateCode.Nop());
        }

        public ParserRuleContext AntlrContext { get; }
    }
}