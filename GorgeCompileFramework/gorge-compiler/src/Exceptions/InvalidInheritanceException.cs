using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions.CompileException;

namespace Gorge.GorgeCompiler.Exceptions
{
    /// <summary>
    /// 继承不可继承的符号引发的异常
    /// </summary>
    public class InvalidInheritanceException<TSymbolIdentifier> : GorgeCompileException
    {
        /// <summary>
        /// 生成错误信息
        /// </summary>
        /// <returns>格式化的错误信息</returns>
        private static string GenerateErrorMessage(Symbol<TSymbolIdentifier> extensionSymbol)
        {
            return $"{extensionSymbol.SymbolType.DisplayName()}{extensionSymbol.Identifier}不能被继承";
        }

        public InvalidInheritanceException(Symbol<TSymbolIdentifier> extensionSymbol, params CodeLocation[] positions) :
            base(GenerateErrorMessage(extensionSymbol), positions)
        {
        }
    }
}