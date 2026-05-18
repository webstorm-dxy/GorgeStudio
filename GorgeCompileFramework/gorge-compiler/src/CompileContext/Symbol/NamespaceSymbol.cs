using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    public class NamespaceSymbol : Symbol<string>
    {
        public override SymbolType SymbolType => SymbolType.Namespace;
        public override HashSet<ModifierType> AllowedModifierTypes { get; } = new();
        
        public NamespaceScope NamespaceScope { get; }
        
        public NamespaceSymbol(NamespaceScope scope, string identifier, CodeLocation definitionToken,
            CodeRange definitionRange) : base(scope, identifier, definitionToken, definitionRange)
        {
            NamespaceScope = new NamespaceScope(scope, identifier);
        }

        public override void CheckModifierConflict(ModifierType modifierType, IToken modifierToken)
        {
            throw new UnreachableException();
        }
    }
}