namespace Gorge.GorgeCompiler.Exceptions.CompileException
{
    /// <summary>
    /// 参数数量错误异常
    /// </summary>
    public class UnexpectedParameterCountException : GorgeCompileException
    {
        /// <summary>
        /// 生成错误信息
        /// </summary>
        /// <returns>格式化的错误信息</returns>
        private static string GenerateMessage(int expectedParameterCount, int actualParameterCount)
        {
            return $"参数数量错误，期望{expectedParameterCount}个，实际{actualParameterCount}个";
        }

        public UnexpectedParameterCountException(int expectedParameterCount, int actualParameterCount,
            params CodeLocation[] positions) : base(
            GenerateMessage(expectedParameterCount, actualParameterCount), positions)
        {
        }
    }
}