using System.Collections.Generic;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;

namespace Gorge.GorgeCompiler.Exceptions
{
    /// <summary>
    /// 符号存在性断言失败
    /// </summary>
    public class SymbolExistenceCompilerException<TSymbolIdentifierType> : GorgeCompilerException
    {
        public SymbolExistenceCompilerException(SymbolScope<TSymbolIdentifierType> scope,
            TSymbolIdentifierType identifier,
            List<CodeLocation> position, params SymbolType[] symbolTypes) :
            base(SymbolExistenceCompileException<TSymbolIdentifierType>.GenerateMessage(scope, identifier, symbolTypes),
                position.ToArray())
        {
        }
    }
}