#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompileException;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    public interface ISymbol
    {
        /// <summary>
        /// 符号类型
        /// 表示符号的类型，例如命名空间、类、枚举或接口。
        /// </summary>
        public SymbolType SymbolType { get; }
        
        /// <summary>
        /// 符号定义词法单元
        /// 记录一个符号的定义位置，用于在编译期间跟踪符号在源代码中的具体位置。
        /// </summary>
        public List<CodeLocation> DefinitionToken { get; }
        
        /// <summary>
        /// 引用符号的标记集合。
        /// 记录此符号在源代码中所有被引用的位置，用于在编译过程中进行符号引用的解析和跟踪。
        /// </summary>
        public List<CodeLocation> ReferenceTokens { get; }
    }

    public static class SymbolExtension
    {
        /// <summary>
        /// 断言一个符号为目标类型，如果失败则抛出异常
        /// </summary>
        /// <param name="type"></param>
        /// <typeparam name="TExpected"></typeparam>
        /// <returns></returns>
        /// <exception cref="UnexpectedSymbolTypeException"></exception>
        public static TExpected Assert<TExpected>(this ISymbol type) where TExpected : ISymbol
        {
            if (type is TExpected expected)
            {
                return expected;
            }

            throw new UnexpectedSymbolTypeException(typeof(TExpected), type.GetType(), null);
        }
    }

    /// <summary>
    /// 符号.
    /// 表示编译过程中定义的命名实体，包含标识符、定义位置、符号类型及其引用关系。
    /// </summary>
    /// <typeparam name="TIdentifier">符号标识符类型</typeparam>
    public abstract class Symbol<TIdentifier> : ISymbol
    {
        /// <summary>
        /// 符号所属的符号域。
        /// 表示该符号所处的作用域，用于追踪符号定义的上下文环境。
        /// </summary>
        public readonly SymbolScope<TIdentifier> Scope;

        /// <summary>
        /// 符号标识符
        /// 表示一个符号的唯一名称，用于区分不同的命名实体。
        /// </summary>
        public readonly TIdentifier Identifier;

        /// <summary>
        /// 符号定义词法单元
        /// 记录一个符号的定义位置，用于在编译期间跟踪符号在源代码中的具体位置。
        /// </summary>
        public List<CodeLocation> DefinitionToken { get; }

        /// <summary>
        /// 符号定义代码范围。
        /// 记录整个符号定义的范围
        /// </summary>
        public readonly CodeRange DefinitionRange;

        /// <summary>
        /// 符号类型
        /// 表示符号的类型，例如命名空间、类、枚举或接口。
        /// </summary>
        public abstract SymbolType SymbolType { get; }

        /// <summary>
        /// 引用符号的标记集合。
        /// 记录此符号在源代码中所有被引用的位置，用于在编译过程中进行符号引用的解析和跟踪。
        /// </summary>
        public List<CodeLocation> ReferenceTokens { get; } = new();

        /// <summary>
        /// 符号修饰符
        /// </summary>
        public readonly Dictionary<ModifierType, Modifier<TIdentifier>> Modifiers = new();

        /// <summary>
        /// 允许的符号修饰符类型集合
        /// </summary>
        public abstract HashSet<ModifierType> AllowedModifierTypes { get; }

        /// <summary>
        /// 符号.
        /// 表示编译过程中定义的命名实体，包含标识符、定义位置、符号类型及其引用关系。
        /// </summary>
        /// <param name="scope">符号所在的符号域</param>
        /// <param name="identifier">符号标识符</param>
        /// <param name="definitionToken">符号定义位置标记</param>
        /// <param name="definitionRange">符号定义代码范围</param>
        public Symbol(SymbolScope<TIdentifier> scope, TIdentifier identifier, List<CodeLocation> definitionToken,
            CodeRange definitionRange)
        {
            Scope = scope;
            Identifier = identifier;
            DefinitionToken = definitionToken;
            DefinitionRange = definitionRange;
        }
        
        /// <summary>
        /// 符号.
        /// 表示编译过程中定义的命名实体，包含标识符、定义位置、符号类型及其引用关系。
        /// </summary>
        /// <param name="scope">符号所在的符号域</param>
        /// <param name="identifier">符号标识符</param>
        /// <param name="definitionToken">符号定义位置标记</param>
        /// <param name="definitionRange">符号定义代码范围</param>
        public Symbol(SymbolScope<TIdentifier> scope, TIdentifier identifier, CodeLocation definitionToken,
            CodeRange definitionRange) : this(scope, identifier, new List<CodeLocation> {definitionToken}, definitionRange)
        {
        }

        /// <summary>
        /// 添加符号修饰符。
        /// </summary>
        /// <param name="modifierType">要添加的修饰符类型</param>
        /// <param name="modifierToken">修饰符对应的定义位置标记</param>
        /// <exception cref="UnexpectedModifierTypeException">
        /// 当试图添加类型不在允许范围内的修饰符时抛出该异常。
        /// 异常消息中包含实际类型、允许的修饰符类型列表以及发生冲突的位置信息。
        /// </exception>
        public void AddModifier(ModifierType modifierType, IToken modifierToken)
        {
            if (!AllowedModifierTypes.Contains(modifierType))
            {
                throw new UnexpectedModifierTypeException(modifierToken.CodeLocation(), modifierType,
                    AllowedModifierTypes.ToArray());
            }

            CheckModifierConflict(modifierType, modifierToken);
            var newModifier = new Modifier<TIdentifier>(this, modifierToken, modifierType);
            Modifiers.Add(modifierType, newModifier);
        }

        /// <summary>
        /// 检查符号修饰符是否冲突.
        /// 用于验证符号的修饰符是否与现有修饰符存在冲突。
        /// 存在冲突时抛出编译异常
        /// </summary>
        /// <param name="modifierType">要检查的符号修饰符类型</param>
        /// <param name="modifierToken">修饰符的词法单元</param>
        public abstract void CheckModifierConflict(ModifierType modifierType, IToken modifierToken);

        /// <summary>
        /// 增加引用
        /// </summary>
        /// <param name="token"></param>
        public void AddReferenceToken(CodeLocation token)
        {
            ReferenceTokens.Add(token);
        }

        public bool Equals(Symbol<TIdentifier>? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Identifier, other.Identifier);
        }

        public override bool Equals(object? obj)
        {
            return obj is Symbol<TIdentifier> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Scope, Identifier);
        }
    }
}