using Antlr4.Runtime.Tree;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;

namespace Gorge.GorgeCompiler.CompileContext.Scope
{
    public abstract class StringSymbolScope : SymbolScope<string>
    {
        public StringSymbolScope(ISymbolScope? parent = null) : base(parent)
        {
        }

        /// <summary>
        /// 根据标识符获取对应的类型符号，获取失败抛出异常
        /// </summary>
        /// <param name="identifier">待查标识符词法节点</param>
        /// <param name="isReference">是否添加符号引用</param>
        /// <param name="compileException">如果为true，则抛出编译异常，否则抛出编译器异常</param>
        /// <param name="symbolTypes">过滤的符号类型，如果为空则不过滤</param>
        /// <returns>与标识符匹配的类型符号</returns>
        public Symbol<string> GetSymbol(ITerminalNode identifier, bool isReference = false,
            bool compileException = false, params SymbolType[] symbolTypes)
        {
            return GetSymbol(identifier.GetText(), identifier.Symbol.CodeLocation(), isReference, compileException,
                symbolTypes);
        }

        /// <summary>
        /// 根据标识符获取对应的类型符号，获取失败抛出异常
        /// </summary>
        /// <param name="identifier">待查标识符词法节点</param>
        /// <param name="isReference">是否为符号添加引用</param>
        /// <param name="compileException">如果为true，则抛出编译异常，否则抛出编译器异常</param>
        /// <param name="symbolTypes">过滤的符号类型，如果为空则不过滤</param>
        /// <typeparam name="TSymbol">期望的符号类型</typeparam>
        /// <returns>与标识符匹配的类型符号</returns>
        public TSymbol GetSymbol<TSymbol>(ITerminalNode identifier,bool isReference = false, bool compileException = false,
            params SymbolType[] symbolTypes) where TSymbol : Symbol<string>
        {
            return GetSymbol<TSymbol>(identifier.GetText(), identifier.Symbol.CodeLocation(),isReference, compileException,
                symbolTypes);
        }
    }
}