using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    public interface IFieldSymbol : ISymbol
    {
        /// <summary>
        /// 字段实例存储索引
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// 字段类型
        /// </summary>
        public SymbolicGorgeType Type { get; }

        /// <summary>
        /// 声明类型，声明本字段的类型
        /// </summary>
        public SymbolicGorgeType DeclaringType { get; }
    }

    /// <summary>
    /// 字段符号
    /// </summary>
    public class FieldSymbol : Symbol<string>, IFieldSymbol
    {
        /// <summary>
        /// 字段符号域
        /// </summary>
        public readonly FieldScope FieldScope;

        /// <summary>
        /// 字段编号
        /// </summary>
        public readonly int Id;

        public int Index { get; }
        public SymbolicGorgeType Type { get; }
        public SymbolicGorgeType DeclaringType { get; }

        public FieldSymbol(ClassScope parentClassScope, SymbolicGorgeType fieldType, string identifier, int id,
            int index, CodeLocation definitionToken, GorgeParser.FieldDeclarationContext parseTree) : base(parentClassScope,
            identifier, definitionToken, parseTree)
        {
            Type = fieldType;
            DeclaringType = parentClassScope.ClassSymbol.Type;
            Id = id;
            Index = index;
            FieldScope = new FieldScope(parentClassScope, this);
        }

        public override SymbolType SymbolType => SymbolType.Field;
        public override HashSet<ModifierType> AllowedModifierTypes => new();

        public override void CheckModifierConflict(ModifierType modifierType, IToken modifierToken)
        {
            throw new UnreachableException();
        }
    }
}