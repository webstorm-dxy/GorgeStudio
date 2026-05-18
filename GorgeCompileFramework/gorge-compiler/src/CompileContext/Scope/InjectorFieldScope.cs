using System.Collections.Generic;
using Gorge.GorgeCompiler.CompileContext.Symbol;

namespace Gorge.GorgeCompiler.CompileContext.Scope
{
    /// <summary>
    /// 注入器字段符号域，用于存储类的注入器字段的符号信息。
    /// 包含Metadata
    /// </summary>
    public class InjectorFieldScope : StringSymbolScope, IMetadataScopeContainer
    {
        public override HashSet<SymbolType> AllowedSymbolTypes { get; } = new();

        public MetadataScope MetadataScope { get; }

        public InjectorFieldScope(InjectorScope parentScope, InjectorFieldSymbol injectorFieldSymbol) : base(
            parentScope)
        {
            MetadataScope = new MetadataScope(this);
        }
    }
}