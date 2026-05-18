using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeCompiler.CompileContext.Scope
{
    /// <summary>
    /// 委托存储点
    /// </summary>
    public interface IDelegateImplementationContainer
    {
        /// <summary>
        /// 下一委托的编号
        /// </summary>
        public int NextDelegateIndex { get; }

        /// <summary>
        /// 注册委托
        /// </summary>
        /// <param name="delegateImplementation"></param>
        public void RegisterDelegate(GorgeDelegateImplementation delegateImplementation);
    }
}