namespace Gorge.GorgeCompiler.Exceptions
{
    public class UnexpectedSwitchConditionException : GorgeCompilerException
    {
        /// <summary>
        /// 生成错误信息
        /// </summary>
        /// <returns>格式化的错误信息</returns>
        private static string GenerateMessage(object condition)
        {
            return $"switch出现非预期情况：{condition}";
        }

        public UnexpectedSwitchConditionException(object condition, params CodeLocation[] positions) :
            base(GenerateMessage(condition), positions)
        {
        }
    }
}