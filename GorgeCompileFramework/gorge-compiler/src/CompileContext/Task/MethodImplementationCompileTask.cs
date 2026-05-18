using System.Collections.Generic;
using Gorge.GorgeCompiler.CodeBlock;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeCompiler.Optimizer;
using Gorge.GorgeCompiler.Visitors;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.CompileContext.Task
{
    public class MethodImplementationCompileTask : IImplementationCompileTask
    {
        private readonly GorgeParser.CodeBlockListContext _context;

        private readonly MethodSymbol _symbol;
        private readonly ClassSymbol _classSymbol;
        public List<GorgeCompileException> PanicExceptions { get; }

        public MethodImplementationCompileTask(MethodSymbol methodSymbol, GorgeParser.CodeBlockListContext context)
        {
            _symbol = methodSymbol;
            _context = context;
            if (_symbol.MethodScope.ParentMethodGroupScope.ParentScope is not ClassScope classScope)
            {
                throw new GorgeCompilerException("只有类中的方法需要编译实现");
            }

            _classSymbol = classScope.ClassSymbol;
            PanicExceptions = new List<GorgeCompileException>();
        }

        private MethodScope? _methodScope;
        private CodeBlockScope? _blockScope;
        private List<ICodeBlock>? _codeBlockList;

        public void DoCompile(ClassImplementationContext compileContext, bool panicMode, bool generateCode)
        {
            try
            {
                _methodScope = _symbol.MethodScope;

                // 构造方法块域
                _blockScope = _methodScope.GetCodeScope();

                // 编译各方法块
                // BlockListVisitor会自动构造子块域
                var blockListVisitor = new BlockListVisitor(_blockScope, panicMode);
                _codeBlockList = blockListVisitor.Visit(_context);
                PanicExceptions.AddRange(blockListVisitor.PanicExceptions);

                if (generateCode)
                {
                    DoImplement();
                }
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

        private void DoImplement()
        {
            if (_methodScope == null)
            {
                throw new VisitorTemporaryContextException(nameof(_methodScope));
            }

            if (_blockScope == null)
            {
                throw new VisitorTemporaryContextException(nameof(_blockScope));
            }

            if (_codeBlockList == null)
            {
                throw new VisitorTemporaryContextException(nameof(_codeBlockList));
            }

            var codes = new List<IntermediateCode>();

            // 加载参数
            foreach (var (_, parameterSymbol) in _methodScope.Parameters)
            {
                codes.Add(IntermediateCode.LoadParameter(parameterSymbol.Type, parameterSymbol.Address,
                    parameterSymbol.Index));
            }

            foreach (var block in _codeBlockList)
            {
                block.AppendCodes(codes);
            }

            // 中间代码优化
            var (optimized, typeCount) = new IntermediateCodeOptimizer(_classSymbol.Identifier, _symbol.MethodName,
                codes, _blockScope.TotalVariableCount()).RebuildCodeList();

            _symbol.MethodScope.Implement(new CompiledMethodImplementation(
                _symbol.MethodScope.MethodInformation, optimized, typeCount, _classSymbol.Identifier));
        }
    }
}