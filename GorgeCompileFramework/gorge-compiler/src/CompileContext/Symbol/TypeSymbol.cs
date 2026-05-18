using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    /// <summary>
    /// 表示类型的符号定义，用于存储和管理编译器语法树中类型的元数据及作用域信息。
    /// </summary>
    public abstract class TypeSymbol : Symbol<string>
    {
        /// <summary>
        /// 本类型的GorgeType
        /// </summary>
        public abstract SymbolicGorgeType Type { get; }

        /// <summary>
        /// 表示类型符号的符号定义，用于存储和管理编译器语法树中类型的元数据及作用域信息。
        /// </summary>
        /// <param name="scope">所在符号域</param>
        /// <param name="identifier">标识符</param>
        /// <param name="definitionToken">定义本符号的词法单元</param>
        /// <param name="definitionRange">定义本符号的代码范围</param>
        public TypeSymbol(SymbolScope<string> scope, string identifier, CodeLocation definitionToken,
            CodeRange definitionRange) : base(scope, identifier, definitionToken, definitionRange)
        {
        }

        public override HashSet<ModifierType> AllowedModifierTypes { get; } = new()
        {
            ModifierType.Native
        };

        public override void CheckModifierConflict(ModifierType modifierType, IToken modifierToken)
        {
            if (Modifiers.TryGetValue(modifierType, out var existModifier))
            {
                throw new DuplicateModifierException(existModifier, modifierToken);
            }
        }
    }
}