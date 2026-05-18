using System.Collections.Generic;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions.CompileException;

namespace Gorge.GorgeCompiler.Exceptions
{
    /// <summary>
    /// 符号存在性断言失败
    /// </summary>
    public class SymbolExistenceCompileException<TSymbolIdentifierType> : GorgeCompileException
    {
        public static string GenerateMessage(SymbolScope<TSymbolIdentifierType> scope, TSymbolIdentifierType identifier,
            SymbolType[] symbolTypes)
        {
            if (symbolTypes.Length == 0)
            {
                return $"{scope}中不存在名为{identifier}的符号";
            }
            else
            {
                return $"{scope}中不存在名为{identifier}的{string.Join("、", symbolTypes)}";
            }
        }

        public SymbolExistenceCompileException(SymbolScope<TSymbolIdentifierType> scope,
            TSymbolIdentifierType identifier, List<CodeLocation> position, params SymbolType[] symbolTypes) : base(
            GenerateMessage(scope, identifier, symbolTypes), position.ToArray())
        {
        }
    }
}