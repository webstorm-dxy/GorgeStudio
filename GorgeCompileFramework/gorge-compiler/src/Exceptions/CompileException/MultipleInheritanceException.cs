namespace Gorge.GorgeCompiler.Exceptions.CompileException
{
    /// <summary>
    /// 继承多个类的异常
    /// </summary>
    public class MultipleInheritanceException : GorgeCompilerException
    {
        /// <summary>
        /// 生成错误信息
        /// </summary>
        /// <returns>格式化的错误信息</returns>
        private static string GenerateMessage()
        {
            return $"不能同时继承多个类";
        }

        public MultipleInheritanceException(params CodeLocation[] positions) : base(GenerateMessage(),
            positions)
        {
        }
    }
}