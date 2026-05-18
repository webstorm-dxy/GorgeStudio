using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    /// <summary>
    /// 构造器符号
    /// </summary>
    public class ConstructorSymbol : Symbol<ParameterList>
    {
        /// <summary>
        /// 所在构造方法组
        /// </summary>
        public readonly ConstructorGroupScope ConstructorGroupScope;

        // /// <summary>
        // /// 调用的超类构造方法
        // /// </summary>
        // public readonly ConstructorSymbol SuperConstructor;

        /// <summary>
        /// 本构造方法对应的符号域
        /// </summary>
        public readonly ConstructorScope ConstructorScope;

        public readonly int Id;

        public ConstructorSymbol(ConstructorGroupScope scope, ParameterList parameterList, int id,
            CodeLocation definitionToken, CodeRange definitionRange) : base(scope, parameterList, definitionToken,
            definitionRange)
        {
            Id = id;
            ConstructorGroupScope = scope;
            ConstructorScope = new ConstructorScope(scope, this);
        }

        public override SymbolType SymbolType => SymbolType.Constructor;

        public override HashSet<ModifierType> AllowedModifierTypes { get; } = new()
        {
            ModifierType.Injector
        };

        public bool IsInjector => Modifiers.ContainsKey(ModifierType.Injector);

        public override void CheckModifierConflict(ModifierType modifierType, IToken modifierToken)
        {
            if (Modifiers.TryGetValue(modifierType, out var existModifier))
            {
                throw new DuplicateModifierException(existModifier, modifierToken);
            }
        }
    }
}