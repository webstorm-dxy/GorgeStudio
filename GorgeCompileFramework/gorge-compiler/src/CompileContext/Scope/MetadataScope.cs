using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeCompiler.CompileContext.Scope
{
    public class MetadataScope : StringSymbolScope
    {
        public override HashSet<SymbolType> AllowedSymbolTypes => new HashSet<SymbolType>()
        {
            SymbolType.Field
        };

        public MetadataScope(ISymbolScope parentScope) : base(parentScope)
        {
        }

        /// <summary>
        /// 声明一个元数据项
        /// </summary>
        /// <param name="entryType">元数据项类型</param>
        /// <param name="identifier">元数据项标识符</param>
        /// <param name="definitionToken">元数据项定义词法单元</param>
        /// <param name="definitionRange">元数据项定义代码范围</param>
        public MetadataEntrySymbol DeclareEntry(GorgeType entryType, string identifier, IToken definitionToken,
            CodeRange definitionRange)
        {
            var entry = new MetadataEntrySymbol(this, entryType, identifier, definitionToken.CodeLocation(), definitionRange);
            AddSymbol(entry);
            return entry;
        }

        public Dictionary<string, Metadata> ToMetadata()
        {
            var metadata = new Dictionary<string, Metadata>();
            foreach (var (metadataEntryName, metadataEntrySymbol) in Symbols)
            {
                if (metadataEntrySymbol is not MetadataEntrySymbol entrySymbol) continue;
                metadata.Add(entrySymbol.Identifier, entrySymbol.ToMetadata());
            }

            return metadata;
        }
    }
}