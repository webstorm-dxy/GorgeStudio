using System.Collections.Generic;
using System.Linq;
using Gorge.GorgeCompiler.CompileContext;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions.CompileException;

namespace Gorge.GorgeCompiler.Exceptions
{
    /// <summary>
    /// 同名符号重复声明异常
    /// </summary>
    public class DuplicateSymbolDeclarationException<TSymbolIdentifierType> : GorgeCompileException
    {
        /// <summary>
        /// 生成错误信息
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="existSymbol"></param>
        /// <param name="newSymbol"></param>
        /// <returns>格式化的错误信息</returns>
        private static string GenerateErrorMessage(SymbolScope<TSymbolIdentifierType> scope, Symbol<TSymbolIdentifierType> existSymbol, Symbol<TSymbolIdentifierType> newSymbol)
        {
            return $"{scope}符号域中'{existSymbol.Identifier}'被重复声明";
        }

        /// <summary>
        /// 同名符号重复声明异常
        /// </summary>
        /// <param name="scope">符号所在域</param>
        /// <param name="existSymbol">以存在的符号</param>
        /// <param name="newSymbol">待添加的符号</param>
        /// <param name="compileMessage">编译信息，如果为null</param>
        public DuplicateSymbolDeclarationException(SymbolScope<TSymbolIdentifierType> scope, Symbol<TSymbolIdentifierType> existSymbol, Symbol<TSymbolIdentifierType> newSymbol,
            string? compileMessage = null) : base(
            GenerateErrorMessage(scope, existSymbol, newSymbol), 
            existSymbol.DefinitionToken.Concat(newSymbol.DefinitionToken).ToArray() )
        {
        }
    }
}