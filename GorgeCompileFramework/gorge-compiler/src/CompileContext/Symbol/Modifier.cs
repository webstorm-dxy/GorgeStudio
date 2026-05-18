using Antlr4.Runtime;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    public interface IModifier
    {
        /// <summary>
        /// 符号修饰符类型
        /// </summary>
        public ModifierType ModifierType { get; }

        /// <summary>
        /// 修饰符词法单元
        /// </summary>
        public IToken DefinitionToken { get; }
    }

    /// <summary>
    /// 符号修饰符.
    /// 用于描述符号的特性，如是否为静态、是否为原生等。
    /// </summary>
    /// <typeparam name="TSymbolIdentifier">所修饰符号的标识符类型</typeparam>
    public class Modifier<TSymbolIdentifier> : IModifier
    {
        /// <summary>
        /// 本修饰符修饰的符号
        /// </summary>
        public readonly Symbol<TSymbolIdentifier> Symbol;

        /// <summary>
        /// 修饰符词法单元
        /// </summary>
        public IToken DefinitionToken { get; }

        /// <summary>
        /// 符号修饰符类型
        /// </summary>
        public ModifierType ModifierType { get; }

        /// <summary>
        /// 符号修饰符.
        /// 用于描述符号的特性，如是否为静态、是否为原生等。
        /// </summary>
        /// <param name="symbol">本修饰符修饰的符号</param>
        /// <param name="definitionToken">修饰符词法单元</param>
        /// <param name="modifierType">符号修饰符类型</param>
        public Modifier(Symbol<TSymbolIdentifier> symbol, IToken definitionToken, ModifierType modifierType)
        {
            Symbol = symbol;
            DefinitionToken = definitionToken;
            ModifierType = modifierType;
        }
    }
}