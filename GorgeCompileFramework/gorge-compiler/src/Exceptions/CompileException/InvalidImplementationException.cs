using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;

namespace Gorge.GorgeCompiler.Exceptions.CompileException
{
    /// <summary>
    /// 实现不可实现的符号引发的异常
    /// </summary>
    public class InvalidImplementationException<TSymbolIdentifier> : GorgeCompileException
    {
        /// <summary>
        /// 生成错误信息
        /// </summary>
        /// <returns>格式化的错误信息</returns>
        private static string GenerateErrorMessage(Symbol<TSymbolIdentifier> extensionSymbol)
        {
            return $"{extensionSymbol.SymbolType.DisplayName()}{extensionSymbol.Identifier}不能被实现";
        }

        public InvalidImplementationException(Symbol<TSymbolIdentifier> extensionSymbol, params CodeLocation[] positions)
            : base(GenerateErrorMessage(extensionSymbol), positions)
        {
        }
    }
}