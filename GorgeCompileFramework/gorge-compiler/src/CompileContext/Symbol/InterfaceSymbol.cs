using System.Linq;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeLanguage.Objective;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    /// <summary>
    /// 接口的符号定义
    /// </summary>
    public class InterfaceSymbol : TypeSymbol
    {
        /// <summary>
        /// 本接口对应的符号域
        /// </summary>
        public readonly InterfaceScope InterfaceScope;

        public readonly NamespaceScope NamespaceScope;

        public InterfaceSymbol(NamespaceScope scope, string identifier, CodeLocation definitionToken,
            GorgeParser.InterfaceDeclarationContext parserTree,
            GorgeParser.ExpressionContext[] usingsParserTree) : base(scope, identifier, definitionToken, parserTree)
        {
            NamespaceScope = scope;
            InterfaceScope = new InterfaceScope(scope, this, parserTree, usingsParserTree);
        }

        /// <summary>
        /// 本类是否为native类
        /// </summary>
        public bool IsNative => Modifiers.ContainsKey(ModifierType.Native);

        public override SymbolType SymbolType => SymbolType.Interface;
        public override SymbolicGorgeType Type => SymbolicGorgeType.Interface(this);

        public string FullName => Type.ToGorgeType().FullName;

        public CompiledInterface ToInterface()
        {
            foreach (var (_, scope) in InterfaceScope.MethodGroups)
            {
                scope.FreezeDeclaration();
            }

            return new CompiledInterface(Type,
                Modifiers.ContainsKey(ModifierType.Native),
                (from methodGroup in InterfaceScope.MethodGroups
                    from method in methodGroup.Value.Methods
                    select method.Value.MethodInformation).ToArray());
        }
    }
}