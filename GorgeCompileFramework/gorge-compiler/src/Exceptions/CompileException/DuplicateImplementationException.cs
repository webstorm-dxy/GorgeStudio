using Gorge.GorgeCompiler.CompileContext.Symbol;

namespace Gorge.GorgeCompiler.Exceptions.CompileException
{
    /// <summary>
    /// 重复实现相同接口的异常
    /// </summary>
    public class DuplicateImplementationException : GorgeCompilerException
    {
        /// <summary>
        /// 生成错误信息
        /// </summary>
        /// <returns>格式化的错误信息</returns>
        private static string GenerateMessage(InterfaceSymbol interfaceSymbol)
        {
            return $"多次继承相同接口{interfaceSymbol}";
        }

        public DuplicateImplementationException(InterfaceSymbol interfaceSymbol,
            params CodeLocation[] positions) : base(GenerateMessage(interfaceSymbol), positions)
        {
        }
    }
}