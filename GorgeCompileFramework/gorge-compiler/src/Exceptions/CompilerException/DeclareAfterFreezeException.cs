namespace Gorge.GorgeCompiler.Exceptions.CompilerException
{
    public class DeclareAfterFreezeException : GorgeCompilerException
    {
        public DeclareAfterFreezeException(params CodeLocation[] positions) : base("在冻结后声明新要素", positions)
        {
        }
    }
    
    public class DeclareBeforeFreezeException : GorgeCompilerException
    {
        public DeclareBeforeFreezeException(params CodeLocation[] positions) : base("在前置条件冻结前声明新要素", positions)
        {
        }
    }
}