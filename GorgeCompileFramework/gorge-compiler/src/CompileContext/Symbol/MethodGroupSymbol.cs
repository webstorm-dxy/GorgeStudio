using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    /// <summary>
    /// 方法组符号
    /// </summary>
    public class MethodGroupSymbol : Symbol<string>
    {
        public MethodGroupScope MethodGroupScope { get; }

        private readonly Annotation[] _annotations;

        public MethodGroupSymbol(MethodContainerScope parentClassScope, MethodGroupScope superMethodGroupScope,
            string identifier, CodeLocation definitionToken, CodeRange definitionRange,
            HashSet<ModifierType> allowedMethodModifierTypes) : base(parentClassScope, identifier, definitionToken,
            definitionRange)
        {
            MethodGroupScope = new MethodGroupScope(parentClassScope, superMethodGroupScope, this);
            AllowedMethodModifierTypes = allowedMethodModifierTypes;
        }

        public override SymbolType SymbolType => SymbolType.MethodGroup;
        public override HashSet<ModifierType> AllowedModifierTypes { get; } = new();

        /// <summary>
        /// 下辖方法的可用修饰符表
        /// </summary>
        public HashSet<ModifierType> AllowedMethodModifierTypes { get; }

        public override void CheckModifierConflict(ModifierType modifierType, IToken modifierToken)
        {
            throw new UnreachableException();
        }
    }
}