using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    public class EnumValueSymbol : Symbol<string>
    {
        public readonly EnumValueScope EnumValueScope;

        public readonly EnumScope EnumScope;

        public readonly int IntValue;

        public EnumValueSymbol(EnumScope scope, string identifier, int intValue, CodeLocation definitionToken,
            CodeRange definitionRange) : base(scope, identifier, definitionToken, definitionRange)
        {
            IntValue = intValue;
            EnumScope = scope;
            EnumValueScope = new EnumValueScope(scope, this);
        }

        public override SymbolType SymbolType => SymbolType.EnumValue;
        public override HashSet<ModifierType> AllowedModifierTypes { get; } = new();

        public override void CheckModifierConflict(ModifierType modifierType, IToken modifierToken)
        {
            throw new UnreachableException();
        }
    }
}