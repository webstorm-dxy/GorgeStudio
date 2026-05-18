using Gorge.GorgeCompiler.Exceptions;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    /// <summary>
    /// 扩展方法类，用于为符号类型枚举提供额外功能。
    /// </summary>
    public static class SymbolTypeExtension
    {
        /// <summary>
        /// 获取符号类型的显示名称。
        /// 返回与符号类型枚举值对应的用户友好型字符串。
        /// </summary>
        /// <param name="symbolType">符号的类型</param>
        /// <returns>符号类型的显示名称字符串</returns>
        public static string DisplayName(this SymbolType symbolType)
        {
            return symbolType switch
            {
                SymbolType.Namespace => "命名空间",
                SymbolType.Class => "类",
                SymbolType.Enum => "枚举",
                SymbolType.Interface => "接口",
                _ => throw new UnexpectedSwitchConditionException(symbolType)
            };
        }
    }
}