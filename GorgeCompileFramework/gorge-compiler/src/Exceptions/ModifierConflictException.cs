using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;

namespace Gorge.GorgeCompiler.Exceptions
{
    public class ModifierConflictException<TSymbolIdentifier> : GorgeCompilerException
    {
        /// <summary>
        /// 生成错误信息
        /// </summary>
        /// <returns>格式化的错误信息</returns>
        private static string GenerateMessage(Modifier<TSymbolIdentifier> existModifier, ModifierType newModifierType,
            IToken newModifierToken)
        {
            return $"标识符{existModifier.ModifierType.DisplayName()}与{newModifierType.DisplayName()}互相冲突";
        }

        public ModifierConflictException(Modifier<TSymbolIdentifier> existModifier, ModifierType newModifierType, IToken newModifierToken)
            : base(GenerateMessage(existModifier, newModifierType, newModifierToken),
                existModifier.DefinitionToken.CodeLocation(), newModifierToken.CodeLocation())
        {
        }
    }
}