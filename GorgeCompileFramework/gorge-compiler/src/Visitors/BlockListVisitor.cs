using System.Collections.Generic;
using Gorge.GorgeCompiler.CodeBlock;
using Gorge.GorgeCompiler.CompileContext.Scope;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.Visitors
{
    public class BlockListVisitor : GorgePanicableVisitor<List<ICodeBlock>?>
    {
        private readonly CodeBlockScope _superBlock;

        public BlockListVisitor(CodeBlockScope superBlock, bool panicMode) : base(panicMode)
        {
            _superBlock = superBlock;
        }

        public override List<ICodeBlock> VisitCodeBlockList(GorgeParser.CodeBlockListContext context)
        {
            var codeBlockList = new List<ICodeBlock>();
            foreach (var codeBlock in context.codeBlock())
            {
                var blockVisitor = new BlockVisitor(_superBlock, PanicMode);
                codeBlockList.Add(blockVisitor.Visit(codeBlock));
                PanicExceptions.AddRange(blockVisitor.PanicExceptions);
            }

            return codeBlockList;
        }
    }
}