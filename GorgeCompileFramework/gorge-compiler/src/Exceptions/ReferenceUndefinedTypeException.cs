using Gorge.GorgeCompiler.Exceptions.CompileException;

namespace Gorge.GorgeCompiler.Exceptions
{
    /// <summary>
    /// 引用未被定义的类型的异常
    /// </summary>
    public class ReferenceUndefinedTypeException : GorgeCompileException
    {
        /// <summary>
        /// 生成错误信息
        /// </summary>
        /// <returns>格式化的错误信息</returns>
        private static string GenerateErrorMessage(string typeIdentifier)
        {
            return $"类型{typeIdentifier}未被定义";
        }

        public ReferenceUndefinedTypeException(string typeIdentifier, params CodeLocation[] positions)
            : base(GenerateErrorMessage(typeIdentifier), positions)
        {
        }
    }
}