#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompilerException;

namespace Gorge.GorgeCompiler.CompileContext.Scope
{
    public interface ISymbolScope
    {
        /// <summary>
        /// 查找域内符号
        /// </summary>
        /// <param name="identifier">标识符</param>
        /// <param name="symbol">查找结果符号</param>
        /// <param name="referenceLocation">符号引用位置，如果为null则视为无引用</param>
        /// <param name="searchParentScope">是否在父符号域搜索</param>
        /// <param name="searchUsings">是否在导入符号域中搜索</param>
        /// <returns>是否查找成功</returns>
        public bool TryGetSymbol(object identifier, out ISymbol symbol, CodeLocation? referenceLocation = null,
            bool searchParentScope = true, bool searchUsings = true);

        /// <summary>
        /// 引入的符号域
        /// </summary>
        public List<NamespaceScope> Usings { get; }

        /// <summary>
        /// 下属符号域。
        /// 只标识符号域的存储关系，而不是Parent的反向。
        /// </summary>
        public List<ISymbolScope> SubScopes { get; }

        /// <summary>
        /// 下属符号
        /// </summary>
        public List<ISymbol> SubSymbols { get; }
    }

    /// <summary>
    /// 符号域
    /// 记录符号的定义和引用
    /// </summary>
    /// <typeparam name="TSymbolIdentifierType">下属符号的标识符类型</typeparam>
    public abstract class SymbolScope<TSymbolIdentifierType> : ISymbolScope
    {
        /// <summary>
        /// 所属父符号域
        /// </summary>
        public ISymbolScope? Parent { get; protected set; }

        public List<ISymbolScope> SubScopes { get; } = new();

        /// <summary>
        /// 引入的符号域
        /// </summary>
        public List<NamespaceScope> Usings { get; }

        /// <summary>
        /// 符号表
        /// </summary>
        public Dictionary<TSymbolIdentifierType, Symbol<TSymbolIdentifierType>> Symbols { get; } = new();

        public List<ISymbol> SubSymbols => Symbols.Values.Cast<ISymbol>().ToList();

        /// <summary>
        /// 允许添加的符号类型
        /// </summary>
        public abstract HashSet<SymbolType> AllowedSymbolTypes { get; }

        /// <summary>
        /// 表示用于存储和管理符号的域
        /// 提供符号的添加、查找等功能
        /// </summary>
        /// <param name="parent">所属父符号域</param>
        public SymbolScope(ISymbolScope? parent = null)
        {
            Parent = parent;
            Usings = Parent != null ? new List<NamespaceScope>(Parent.Usings) : new List<NamespaceScope>();
            if (Parent != null)
            {
                Parent.SubScopes.Add(this);
            }
        }

        public bool TryGetSymbol(object identifier, out ISymbol symbol, CodeLocation? referenceLocation = null,
            bool searchParentScope = true, bool searchUsings = true)
        {
            // 判断本级符号域所存符号是否为目标类型
            // 如是则在本级搜索
            if (identifier is TSymbolIdentifierType t)
            {
                var result = TryGetSymbol(t, out var symbolT, referenceLocation, searchParentScope);
                symbol = symbolT;
                return result;
            }

            // 否则越过本级，在上级搜索
            if (searchParentScope && Parent != null)
            {
                return Parent.TryGetSymbol(identifier, out symbol, referenceLocation, true);
            }

            // 否则在导入符号域中搜索
            // 对导入符号域的搜索不再考虑导入的导入
            if (searchUsings)
            {
                foreach (var usingScope in Usings)
                {
                    if (usingScope.TryGetSymbol(identifier, out symbol, referenceLocation, true, false))
                    {
                        return true;
                    }
                }
            }

            // 如果不搜索上级或没有可搜索的上级，则查找失败
            symbol = null;
            return false;
        }

        /// <summary>
        /// 尝试获取符号域中的符号
        /// </summary>
        /// <param name="identifier">要查找的符号标识符</param>
        /// <param name="symbol">输出参数，如果找到符号则返回该符号；否则为 null</param>
        /// <param name="referenceLocation">符号引用位置，如果为null则视为无引用</param>
        /// <param name="searchParentScope">是否在父符号域中查找</param>
        /// <param name="searchUsings">是否在导入符号域中搜索</param>
        /// <returns>如果符号存在，则返回 true；否则返回 false</returns>
        public virtual bool TryGetSymbol(TSymbolIdentifierType identifier, out Symbol<TSymbolIdentifierType> symbol,
            CodeLocation? referenceLocation = null, bool searchParentScope = true, bool searchUsings = true)
        {
            if (Symbols.TryGetValue(identifier, out symbol))
            {
                if (referenceLocation != null)
                {
                    symbol.AddReferenceToken(referenceLocation);
                }

                return true;
            }

            if (searchParentScope && Parent != null)
            {
                if (Parent.TryGetSymbol(identifier, out var iSymbol, referenceLocation, true, false))
                {
                    if (iSymbol is Symbol<TSymbolIdentifierType> s)
                    {
                        symbol = s;
                        return true;
                    }

                    throw new GorgeCompilerException("符号查找结果的类型错误");
                }
            }

            if (searchUsings)
            {
                foreach (var usingScope in Usings)
                {
                    if (usingScope.TryGetSymbol(identifier, out var iSymbol, referenceLocation, true, false))
                    {
                        if (iSymbol is Symbol<TSymbolIdentifierType> s)
                        {
                            symbol = s;
                            return true;
                        }

                        throw new GorgeCompilerException("符号查找结果的类型错误");
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 根据标识符获取对应的类型符号，获取失败抛出异常
        /// </summary>
        /// <param name="identifier">要检索的类型标识符</param>
        /// <param name="position">当前编译时异常的位置上下文，包含源文件名及代码范围</param>
        /// <param name="isReference">是否添加符号引用</param>
        /// <param name="compileException">如果为true，则抛出编译异常，否则抛出编译器异常</param>
        /// <param name="symbolTypes">过滤的符号类型，如果为空则不过滤</param>
        /// <returns>与标识符匹配的类型符号</returns>
        public Symbol<TSymbolIdentifierType> GetSymbol(TSymbolIdentifierType identifier, CodeLocation position,
            bool isReference = false, bool compileException = false, params SymbolType[] symbolTypes)
        {
            if (TryGetSymbol(identifier, out var typeSymbol, isReference ? position : null))
            {
                if (symbolTypes.Length != 0 && !symbolTypes.Contains(typeSymbol.SymbolType))
                {
                    if (compileException)
                    {
                        throw new UnexpectedSymbolTypeCompileException(new List<CodeLocation>() {position},
                            typeSymbol.SymbolType, symbolTypes);
                    }
                    else
                    {
                        throw new UnexpectedSymbolTypeCompilerException(new List<CodeLocation>() {position},
                            typeSymbol.SymbolType, symbolTypes);
                    }
                }

                return typeSymbol;
            }

            if (compileException)
            {
                throw new SymbolExistenceCompileException<TSymbolIdentifierType>(this, identifier,
                    new List<CodeLocation>() {position});
            }
            else
            {
                throw new SymbolExistenceCompilerException<TSymbolIdentifierType>(this, identifier,
                    new List<CodeLocation>() {position});
            }
        }

        /// <summary>
        /// 获取符号域中的符号，并根据提供的类型进行验证
        /// 如果符号不符合期望的类型，将抛出异常
        /// </summary>
        /// <param name="identifier">要检索的类型标识符</param>
        /// <param name="position">当前编译时异常的位置上下文，包含源文件名及代码范围</param>
        /// <param name="isReference">是否为符号添加引用</param>
        /// <param name="compileException">如果为true，则抛出编译异常，否则抛出编译器异常</param>
        /// <param name="symbolTypes">过滤的符号类型，如果为空则不过滤</param>
        /// <typeparam name="TSymbol">期望的符号类型</typeparam>
        /// <returns>与标识符匹配的类型符号</returns>
        /// <exception cref="UnexpectedSymbolTypeCompilerException">当符号类型与期望的不匹配时抛出</exception>
        public TSymbol GetSymbol<TSymbol>(TSymbolIdentifierType identifier, CodeLocation position,
            bool isReference = false,
            bool compileException = false, params SymbolType[] symbolTypes)
            where TSymbol : Symbol<TSymbolIdentifierType>
        {
            var symbol = GetSymbol(identifier, position, isReference, compileException, symbolTypes);
            if (symbol is TSymbol tSymbol)
            {
                return tSymbol;
            }

            if (compileException)
            {
                throw new UnexpectedSymbolTypeCompileException(position, symbol.GetType(), typeof(TSymbol));
            }
            else
            {
                throw new UnexpectedSymbolTypeCompilerException(position, symbol.GetType(), typeof(TSymbol));
            }
        }

        /// <summary>
        /// 添加符号
        /// 在当前的符号域中注册一个新的符号，并执行类型检查
        /// </summary>
        /// <param name="symbol"></param>
        protected void AddSymbol(Symbol<TSymbolIdentifierType> symbol)
        {
            if (!AllowedSymbolTypes.Contains(symbol.SymbolType))
            {
                throw new UnexpectedSymbolTypeCompilerException(symbol.DefinitionToken,
                    symbol.SymbolType, AllowedSymbolTypes.ToArray());
            }

            if (TryGetSymbol(symbol.Identifier, out var existSymbol, null, false, false))
            {
                throw new DuplicateSymbolDeclarationException<TSymbolIdentifierType>(this, existSymbol, symbol);
            }

            Symbols[symbol.Identifier] = symbol;
        }
    }
}