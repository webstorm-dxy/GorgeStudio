using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    public class VariableSymbol : LocalSymbol
    {
        public VariableSymbol(CodeBlockScope scope, string identifier, SymbolicAddress address, CodeLocation definitionToken,
            CodeRange definitionRange) : base(scope, identifier, address.Type, address, definitionToken,
            definitionRange)
        {
        }

        public override SymbolType SymbolType => SymbolType.Variable;
        public override HashSet<ModifierType> AllowedModifierTypes { get; } = new();

        public override void CheckModifierConflict(ModifierType modifierType, IToken modifierToken)
        {
            throw new UnreachableException();
        }
    }

    public class ParameterSymbol : LocalSymbol
    {
        public readonly int Index;

        public ParameterSymbol(CodeBlockScope scope, SymbolicGorgeType type, string identifier, int index,
            SymbolicAddress address, CodeLocation definitionToken, CodeRange definitionRange) : base(scope, identifier,
            type, address, definitionToken, definitionRange)
        {
            Index = index;
        }

        public override SymbolType SymbolType => SymbolType.Parameter;
        public override HashSet<ModifierType> AllowedModifierTypes { get; } = new();

        public override void CheckModifierConflict(ModifierType modifierType, IToken modifierToken)
        {
            throw new UnreachableException();
        }
    }
}