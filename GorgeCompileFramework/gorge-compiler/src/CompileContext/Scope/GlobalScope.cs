namespace Gorge.GorgeCompiler.CompileContext.Scope
{
    /// <summary>
    /// 全局符号域，在global命名空间的基础上额外存储全局编译信息
    /// </summary>
    public class GlobalScope : NamespaceScope
    {
        public override string FullName => null;

        public GlobalScope() : base(null, "global")
        {
        }

        public override string GetSubTypeFullName(string typeIdentifier)
        {
            return typeIdentifier;
        }
    }
}