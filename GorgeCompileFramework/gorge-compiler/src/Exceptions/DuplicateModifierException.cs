using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;

namespace Gorge.GorgeCompiler.Exceptions
{
    public class DuplicateModifierException : GorgeCompilerException
    {
        /// <summary>
        /// 生成错误信息
        /// </summary>
        /// <returns>格式化的错误信息</returns>
        private static string GenerateMessage(IModifier existModifier, IToken newModifierToken)
        {
            return $"标识符{existModifier.ModifierType.DisplayName()}重复";
        }

        public DuplicateModifierException(IModifier existModifier, IToken newModifierToken) : base(
            GenerateMessage(existModifier, newModifierToken), existModifier.DefinitionToken.CodeLocation(),
            newModifierToken.CodeLocation())
        {
        }
    }
}