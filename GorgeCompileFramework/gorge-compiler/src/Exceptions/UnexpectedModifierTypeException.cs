using Gorge.GorgeCompiler.CompileContext;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;

namespace Gorge.GorgeCompiler.Exceptions
{
    /// <summary>
    /// 修饰符类型不符合期望的异常
    /// </summary>
    public class UnexpectedModifierTypeException : GorgeCompilerException
    {
        private static string GenerateMessage(ModifierType actualType, ModifierType[] expectedTypes)
        {
            return $"修饰符不符合期望，期望{string.Join(",", expectedTypes)}，实为{actualType}";
        }

        public UnexpectedModifierTypeException(CodeLocation position, ModifierType actualType,
            params ModifierType[] expectedTypes) : base(GenerateMessage(actualType, expectedTypes), position)
        {
        }
    }
}