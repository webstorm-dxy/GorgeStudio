namespace Gorge.GorgeCompiler.Exceptions
{
    /// <summary>
    /// Visitor临时上下文传递错误
    /// </summary>
    public class VisitorTemporaryContextException : GorgeCompilerException
    {
        /// <summary>
        /// 生成错误信息
        /// </summary>
        /// <returns>格式化的错误信息</returns>
        private static string GenerateErrorMessage(string contextName)
        {
            return $"Visitor临时上下文{contextName}传递错误";
        }

        public VisitorTemporaryContextException(string contextName, params CodeLocation[] positions) :
            base(GenerateErrorMessage(contextName), positions)
        {
        }
    }
}