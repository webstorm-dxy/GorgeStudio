using System.Collections.Generic;
using Gorge.GorgeCompiler.CompileContext.Block;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeCompiler.Expression;
using Gorge.GorgeCompiler.Visitors;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.CompileContext.Task
{
    public class MetadataEntryImplementationCompileTask : IImplementationCompileTask
    {
        private readonly GorgeParser.ExpressionContext _context;

        private readonly ClassSymbol _classSymbol;

        private readonly MetadataEntrySymbol _symbol;
        public List<GorgeCompileException> PanicExceptions { get; }

        public MetadataEntryImplementationCompileTask(MetadataEntrySymbol metadataEntrySymbol, ClassSymbol classSymbol,
            GorgeParser.ExpressionContext context)
        {
            _symbol = metadataEntrySymbol;
            _classSymbol = classSymbol;
            _context = context;
            PanicExceptions = new List<GorgeCompileException>();
        }

        public void DoCompile(ClassImplementationContext compileContext, bool panicMode, bool generateCode)
        {
            try
            {
                // TODO 可以补充类型验证
                var block = new CodeBlockScope(BlockContextType.StaticMethod, _classSymbol, null,
                    _classSymbol.ClassScope);
                _symbol.Implement(new ExpressionVisitor(block, panicMode).Visit(_context)
                    .Assert<IGorgeValueExpression>().AssertCompileConstant());
            }
            catch (GorgeCompileException e)
            {
                if (!panicMode)
                {
                    throw;
                }
                PanicExceptions.Add(e);
            }
        }
    }
}