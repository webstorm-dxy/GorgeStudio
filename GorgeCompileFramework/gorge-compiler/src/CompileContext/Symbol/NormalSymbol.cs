using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    /// <summary>
    /// 普通符号定义，不允许任何修饰符
    /// </summary>
    public class NormalSymbol : Symbol<string>
    {
        public NormalSymbol(SymbolScope<string> scope, string identifier, CodeLocation definitionToken, CodeRange definitionRange,
            SymbolType symbolType) : base(scope, identifier, definitionToken, definitionRange)
        {
            SymbolType = symbolType;
        }

        public override SymbolType SymbolType { get; }

        public override HashSet<ModifierType> AllowedModifierTypes { get; } = new();

        public override void CheckModifierConflict(ModifierType modifierType, IToken modifierToken)
        {
            throw new UnreachableException();
        }
    }
}