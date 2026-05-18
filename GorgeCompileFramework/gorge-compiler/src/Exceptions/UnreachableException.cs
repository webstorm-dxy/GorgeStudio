namespace Gorge.GorgeCompiler.Exceptions
{
    /// <summary>
    /// 无法到达的异常
    /// 表示代码执行到不应该被执行的区域
    /// </summary>
    public class UnreachableException : GorgeCompilerException
    {
        public UnreachableException() : base("编译器代码执行至无法到达的位置")
        {
        }
    }
}