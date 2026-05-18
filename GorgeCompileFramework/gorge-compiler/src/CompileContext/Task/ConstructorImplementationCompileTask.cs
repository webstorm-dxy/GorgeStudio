#nullable enable
using System.Collections.Generic;
using System.Linq;
using Gorge.GorgeCompiler.CodeBlock;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeCompiler.Expression;
using Gorge.GorgeCompiler.Optimizer;
using Gorge.GorgeCompiler.Visitors;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.CompileContext.Task
{
    public class ConstructorImplementationCompileTask : IImplementationCompileTask
    {
        private readonly GorgeParser.ConstructorDeclarationContext _constructorDeclarationContext;
        private readonly GorgeParser.SuperClassConstructorContext? _superClassConstructorContext;
        private readonly GorgeParser.CodeBlockListContext _codeBlockListContext;

        private readonly ConstructorSymbol _symbol;
        private readonly ClassSymbol _classSymbol;

        public List<GorgeCompileException> PanicExceptions { get; }

        public ConstructorImplementationCompileTask(ConstructorSymbol constructorSymbol,
            GorgeParser.ConstructorDeclarationContext constructorDeclarationContext,
            GorgeParser.SuperClassConstructorContext? superClassConstructorContext,
            GorgeParser.CodeBlockListContext codeBlockListContext)
        {
            _symbol = constructorSymbol;
            _constructorDeclarationContext = constructorDeclarationContext;
            _superClassConstructorContext = superClassConstructorContext;
            _codeBlockListContext = codeBlockListContext;
            _classSymbol = _symbol.ConstructorScope.ParentConstructorGroupScope.ParentScope.ClassSymbol;
            PanicExceptions = new List<GorgeCompileException>();
        }

        private ConstructorScope? _constructorScope;
        private ConstructorInformation? _superConstructor;
        private IGorgeValueExpression[]? _superClassArgumentExpressions;
        private List<ICodeBlock>? _codeBlocks;
        private CodeBlockScope? _blockContext;

        public void DoCompile(ClassImplementationContext compileContext, bool panicMode, bool generateCode)
        {
            try
            {
                _constructorScope = _symbol.ConstructorScope;

                // 编译超累构造方法调用
                if (_classSymbol.ClassScope.SuperClass != null)
                {
                    if (_superClassConstructorContext == null)
                    {
                        _superClassArgumentExpressions = new IGorgeValueExpression[] { };
                    }
                    else
                    {
                        _superClassArgumentExpressions = _superClassConstructorContext.expression()
                            .Select(a => new ExpressionVisitor(_constructorScope, panicMode).Visit(a)
                                .Assert<IGorgeValueExpression>()).ToArray();
                    }

                    var parameterList = _superClassArgumentExpressions.Select(p => p.ValueType).ToArray();
                    var superConstructorSymbol = _classSymbol.ClassScope.SuperClass.ClassScope.ConstructorGroupScope
                        .GetConstructorByArgumentTypes(parameterList, null,
                            _superClassConstructorContext ??
                            _constructorDeclarationContext.Identifier().CodeLocation());

                    _superConstructor = superConstructorSymbol.ConstructorScope.ConstructorInformation;
                }

                // 编译构造方法的各代码块
                _blockContext = _constructorScope.GetCodeScope();

                // 执行本类构造逻辑
                var codeBlockVisitor = new BlockListVisitor(_blockContext, panicMode);
                _codeBlocks = codeBlockVisitor.Visit(_codeBlockListContext);
                PanicExceptions.AddRange(codeBlockVisitor.PanicExceptions);

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
            if (_constructorScope == null)
            {
                throw new VisitorTemporaryContextException(nameof(_constructorScope));
            }

            var codes = new List<IntermediateCode>();

            // 注入器和参数的加载
            codes.Add(IntermediateCode.LoadInjector(_constructorScope.InjectorAddress));
            foreach (var (_, parameterSymbol) in _constructorScope.Parameters)
            {
                codes.Add(IntermediateCode.LoadParameter(parameterSymbol.Type, parameterSymbol.Address,
                    parameterSymbol.Index));
            }

            // 超类构造方法调用
            if (_superConstructor != null)
            {
                if (_superClassArgumentExpressions == null)
                {
                    throw new VisitorTemporaryContextException(nameof(_superClassArgumentExpressions));
                }

                // 准备父类构造方法调用参数
                codes.Add(IntermediateCode.SetInjector(_constructorScope.InjectorAddress));
                for (var i = 0; i < _superClassArgumentExpressions.Length; i++)
                {
                    var argumentExpression = _superClassArgumentExpressions[i];
                    var argumentAddress = codes.Add(_constructorScope, argumentExpression);
                    codes.Add(IntermediateCode.SetParameter(_superConstructor.Parameters[i].Type,
                        _superConstructor.Parameters[i].Index, (Address) argumentAddress));
                }

                // 调用父类构造逻辑
                codes.Add(IntermediateCode.DoConstruct(_superConstructor.Id));
            }

            if (_codeBlocks == null)
            {
                throw new VisitorTemporaryContextException(nameof(_codeBlocks));
            }

            if (_blockContext == null)
            {
                throw new VisitorTemporaryContextException(nameof(_blockContext));
            }

            foreach (var block in _codeBlocks)
            {
                block.AppendCodes(codes);
            }

            // 中间代码优化
            var (optimized, typeCount) =
                new IntermediateCodeOptimizer(_classSymbol.Identifier, "Constructor", codes,
                    _blockContext.TotalVariableCount()).RebuildCodeList();

            _symbol.ConstructorScope.Implement(
                new CompiledConstructorImplementation(_symbol.ConstructorScope.ConstructorInformation, optimized,
                    typeCount, _classSymbol.Identifier));
        }
    }

    public static class CodeListExtensions
    {
        public static SymbolicAddress Add(this List<IntermediateCode> codes, CodeBlockScope scope,
            IGorgeValueExpression expression) => expression.AppendCodes(scope, codes);
    }
}