using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    /// <summary>
    /// 注入器字段符号
    /// </summary>
    public class InjectorFieldSymbol : Symbol<string>
    {
        /// <summary>
        /// 注入器字段类型
        /// </summary>
        public readonly SymbolicGorgeType FieldType;

        /// <summary>
        /// 注入器字段编号
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// 注入器字段在注入器实例存储索引
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// 注入器默认值存储索引，为null表示无默认值
        /// </summary>
        public readonly int? DefaultValueIndex;

        /// <summary>
        /// 注入器字段符号域
        /// </summary>
        public readonly InjectorFieldScope InjectorFieldScope;

        public InjectorFieldSymbol(InjectorScope scope, SymbolicGorgeType fieldType, string identifier, int id, int index,
            int? defaultValueIndex, CodeLocation definitionToken, CodeRange definitionRange) : base(scope, identifier,
            definitionToken, definitionRange)
        {
            FieldType = fieldType;
            Id = id;
            Index = index;
            DefaultValueIndex = defaultValueIndex;
            InjectorFieldScope = new InjectorFieldScope(scope, this);
        }

        public override SymbolType SymbolType => SymbolType.Field;
        public override HashSet<ModifierType> AllowedModifierTypes { get; } = new();

        public override void CheckModifierConflict(ModifierType modifierType, IToken modifierToken)
        {
            throw new UnreachableException();
        }

        public InjectorFieldInformation ToInjectorFieldInformation()
        {
            return new InjectorFieldInformation(Id,Identifier,FieldType,Index,DefaultValueIndex,InjectorFieldScope.MetadataScope.ToMetadata());
        }
    }
}