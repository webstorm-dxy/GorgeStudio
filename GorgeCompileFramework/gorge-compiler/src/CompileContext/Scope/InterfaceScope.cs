using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompilerException;
using Gorge.GorgeLanguage.Objective;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.CompileContext.Scope
{
    /// <summary>
    /// 接口符号域，用于存储接口相关的符号信息。
    /// 包含方法符号。
    /// </summary>
    public class InterfaceScope : MethodContainerScope
    {
        public override HashSet<SymbolType> AllowedSymbolTypes { get; } = new()
        {
            SymbolType.MethodGroup
        };

        /// <summary>
        /// 本域的接口符号
        /// </summary>
        public readonly InterfaceSymbol InterfaceSymbol;

        /// <summary>
        /// Using表达式
        /// </summary>
        public readonly GorgeParser.ExpressionContext[] UsingsParserTree;

        /// <summary>
        /// 本接口的语法树
        /// </summary>
        public readonly GorgeParser.InterfaceDeclarationContext ParserTree;

        /// <summary>
        /// 接口的声明信息。
        /// 冻结声明后可用
        /// </summary>
        public CompiledInterface Interface { get; private set; }

        /// <summary>
        /// 接口所在的源文件地址
        /// </summary>
        public readonly string SourceFilePath;

        public override bool IsNative => InterfaceSymbol.IsNative;

        public override SymbolicGorgeType Type => InterfaceSymbol.Type;
        
        public override IReadOnlyList<GenericsSymbol> TypeGenericsSymbols => new List<GenericsSymbol>();

        public InterfaceScope(NamespaceScope parentNamespace, InterfaceSymbol interfaceSymbol,
            GorgeParser.InterfaceDeclarationContext parserTree,
            GorgeParser.ExpressionContext[] usingsParserTree) : base(null)
        {
            InterfaceSymbol = interfaceSymbol;
            ParserTree = parserTree;
            UsingsParserTree = usingsParserTree;
            SourceFilePath = parserTree.Start.TokenSource.SourceName;
            // 由于自身为根，需要补充在namespace中添加下属
            parentNamespace.SubScopes.Add(this);
        }

        public override HashSet<ModifierType> AllowedMethodModifierTypes { get; } = new();

        private int _methodId = 0;

        /// <summary>
        /// 接口内的方法数
        /// </summary>
        public int MethodCount => _methodId;

        /// <summary>
        /// 方法表
        /// key是id，value是方法符号
        /// </summary>
        public Dictionary<int, MethodSymbol> Methods { get; } = new();

        public override MethodScope DeclareMethod(string name, SymbolicGorgeType returnType,
            ParameterList parameterList,
            Dictionary<ModifierType, IToken> modifiers, IToken definitionToken,
            GorgeParser.MethodDeclarationContext parserTree)
        {
            EnsureDeclarationNotFreeze();

            MethodGroupScope methodGroups;
            if (TryGetSymbol(name, out var symbol, null, false))
            {
                if (symbol is not MethodGroupSymbol methodGroupSymbol)
                {
                    throw new UnexpectedSymbolTypeCompilerException(
                        new List<CodeLocation>() {definitionToken.CodeLocation()},
                        symbol.SymbolType, SymbolType.MethodGroup);
                }

                methodGroups = methodGroupSymbol.MethodGroupScope;
            }
            else
            {
                methodGroups = DeclareMethodGroup(name, definitionToken, parserTree);
            }

            var method =
                methodGroups.DeclareMethod(returnType, parameterList, _methodId++, definitionToken, parserTree);
            Methods.Add(method.MethodSymbol.Id, method.MethodSymbol);

            foreach (var (modifier, token) in modifiers)
            {
                method.MethodSymbol.AddModifier(modifier, token);
            }

            return method;
        }

        /// <summary>
        /// 在类中声明方法组
        /// </summary>
        /// <param name="identifier">方法组标识符，即组内方法类名</param>
        /// <param name="definitionToken">定义该方法组首方法的词法Token</param>
        /// <param name="parserTree">定义该方法组首方法的语法树</param>
        /// <returns>方法组符号域</returns>
        private MethodGroupScope DeclareMethodGroup(string identifier, IToken definitionToken,
            GorgeParser.MethodDeclarationContext parserTree)
        {
            EnsureDeclarationNotFreeze();

            var methodGroupSymbol = new MethodGroupSymbol(this, null, identifier, definitionToken.CodeLocation(), parserTree,
                AllowedMethodModifierTypes);
            AddSymbol(methodGroupSymbol);
            var methodGroupScope = methodGroupSymbol.MethodGroupScope;
            MethodGroups.Add(methodGroupSymbol, methodGroupScope);
            return methodGroupScope;
        }

        public override void FreezeDeclaration()
        {
            base.FreezeDeclaration();
            Interface = InterfaceSymbol.ToInterface();
        }
    }
}