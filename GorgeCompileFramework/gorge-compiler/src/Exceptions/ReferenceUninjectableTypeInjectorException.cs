using Gorge.GorgeCompiler.CompileContext;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions.CompileException;

namespace Gorge.GorgeCompiler.Exceptions
{
    /// <summary>
    /// 引用不可注入的类型的注入器的异常
    /// </summary>
    public class ReferenceUninjectableTypeInjectorException<TSymbolIdentifier> : GorgeCompileException
    {
        /// <summary>
        /// 生成错误信息
        /// </summary>
        /// <returns>格式化的错误信息</returns>
        private static string GenerateErrorMessage(Symbol<TSymbolIdentifier> uninjectableSymbol)
        {
            switch (uninjectableSymbol.SymbolType)
            {
                case SymbolType.Interface:
                    return $"不能直接使用接口{uninjectableSymbol.Identifier}的注入器";
                case SymbolType.Class:
                    throw new GorgeCompilerException("尝试对类的注入器类型应用，但抛出异常");
                default:
                    return $"类型{uninjectableSymbol.Identifier}没有注入器";
            }
        }

        public ReferenceUninjectableTypeInjectorException(Symbol<TSymbolIdentifier> uninjectableSymbol,
            params CodeLocation[] positions) : base(GenerateErrorMessage(uninjectableSymbol),
            positions)
        {
        }
    }
}