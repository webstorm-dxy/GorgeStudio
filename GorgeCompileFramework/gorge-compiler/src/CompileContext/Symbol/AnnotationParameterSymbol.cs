using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    public class AnnotationParameterSymbol : Symbol<string>
    {
        public readonly object Value;

        public AnnotationParameterSymbol(AnnotationScope scope, string identifier, object value,
            CodeLocation definitionToken, CodeRange definitionRange) : base(scope, identifier, definitionToken, definitionRange)
        {
            Value = value;
        }

        public override SymbolType SymbolType => SymbolType.Field;
        public override HashSet<ModifierType> AllowedModifierTypes => new();

        public override void CheckModifierConflict(ModifierType modifierType, IToken modifierToken)
        {
            throw new UnreachableException();
        }
    }
}