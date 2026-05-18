using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    public class MetadataEntrySymbol : Symbol<string>
    {
        public Metadata Metadata { get; private set; }

        public MetadataScope MetadataScope { get; }

        public MetadataEntrySymbol(MetadataScope groupScope, GorgeType entryType, string identifier,
            CodeLocation definitionToken, CodeRange definitionRange) : base(groupScope, identifier, definitionToken,
            definitionRange)
        {
            MetadataScope = groupScope;
            EntryType = entryType;
            Metadata = new Metadata(EntryType, Identifier);
        }

        /// <summary>
        /// 字段类型
        /// </summary>
        public readonly GorgeType EntryType;

        public override SymbolType SymbolType => SymbolType.Field;
        public override HashSet<ModifierType> AllowedModifierTypes { get; } = new();

        public override void CheckModifierConflict(ModifierType modifierType, IToken modifierToken)
        {
            throw new UnreachableException();
        }

        public Metadata ToMetadata()
        {
            return Metadata;
        }

        /// <summary>
        /// 实现本元数据条目
        /// </summary>
        /// <param name="entryValue">条目值</param>
        public void Implement(object entryValue)
        {
            Metadata.Value = entryValue;
        }
    }
}