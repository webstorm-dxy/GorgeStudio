#nullable enable
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.CompileContext.Scope
{
    /// <summary>
    /// 命名空间符号域，内含多个类、枚举、接口和下级命名空间
    /// </summary>
    public class NamespaceScope : StringSymbolScope
    {
        public override HashSet<SymbolType> AllowedSymbolTypes { get; } = new()
        {
            SymbolType.Namespace,
            SymbolType.Class,
            SymbolType.Enum,
            SymbolType.Interface
        };

        /// <summary>
        /// namespace名字
        /// </summary>
        public readonly string NamespaceName;

        /// <summary>
        /// 下级命名空间符号域
        /// </summary>
        public readonly Dictionary<NamespaceSymbol, NamespaceScope> SubNamespaces = new();

        /// <summary>
        /// 类符号域
        /// </summary>
        public readonly Dictionary<ClassSymbol, ClassScope> Classes = new();

        /// <summary>
        /// 枚举符号域
        /// </summary>
        public readonly Dictionary<EnumSymbol, EnumScope> Enums = new();

        /// <summary>
        /// 接口符号域
        /// </summary>
        public readonly Dictionary<InterfaceSymbol, InterfaceScope> Interfaces = new();

        public virtual string FullName { get; }

        /// <summary>
        /// namespace符号域，内含多个子namespace、类、枚举和接口
        /// </summary>
        /// <param name="parentNamespace"></param>
        /// <param name="namespaceName">namespace的名字</param>
        public NamespaceScope(NamespaceScope? parentNamespace, string namespaceName) : base(parentNamespace)
        {
            FullName = parentNamespace is null ? namespaceName : parentNamespace.GetSubTypeFullName(namespaceName);

            // TODO 验证名字合法性
            NamespaceName = namespaceName;
        }

        /// <summary>
        /// 获取子类型的全名
        /// </summary>
        /// <param name="typeIdentifier"></param>
        /// <returns></returns>
        public virtual string GetSubTypeFullName(string typeIdentifier)
        {
            return FullName + "." + typeIdentifier;
        }

        public NamespaceScope DeclareNamespace(string identifier, IToken definitionToken, CodeRange definitionRange)
        {
            var namespaceSymbol = new NamespaceSymbol(this, identifier, definitionToken.CodeLocation(), definitionRange);
            AddSymbol(namespaceSymbol);
            var namespaceScope = namespaceSymbol.NamespaceScope;
            SubNamespaces.Add(namespaceSymbol, namespaceScope);
            return namespaceScope;
        }

        /// <summary>
        /// 在命名空间中添加一个类声明
        /// </summary>
        /// <param name="identifier">类的标识符</param>
        /// <param name="definitionToken">定义该类的词法Token</param>
        /// <param name="parserTree">类的语法树</param>
        /// <param name="usingsParserTree"></param>
        public ClassScope DeclareClass(string identifier, IToken definitionToken,
            GorgeParser.ClassDeclarationContext parserTree, GorgeParser.ExpressionContext[] usingsParserTree)
        {
            var classSymbol = new ClassSymbol(this, identifier, definitionToken.CodeLocation(), parserTree, usingsParserTree);
            AddSymbol(classSymbol);
            var classScope = classSymbol.ClassScope;
            Classes.Add(classSymbol, classScope);
            return classScope;
        }

        /// <summary>
        /// 在命名空间中添加一个枚举声明
        /// </summary>
        /// <param name="identifier">枚举的标识符</param>
        /// <param name="definitionToken">定义该枚举的词法Token</param>
        /// <param name="parserTree">枚举的语法树</param>
        /// <param name="usingsParserTree"></param>
        public EnumScope DeclareEnum(string identifier, IToken definitionToken,
            GorgeParser.EnumDeclarationContext parserTree, GorgeParser.ExpressionContext[] usingsParserTree)
        {
            var enumSymbol = new EnumSymbol(this, identifier, definitionToken.CodeLocation(), parserTree, usingsParserTree);
            AddSymbol(enumSymbol);
            var enumScope = enumSymbol.EnumScope;
            Enums.Add(enumSymbol, enumScope);
            return enumScope;
        }

        /// <summary>
        /// 在命名空间中添加一个枚举声明
        /// </summary>
        /// <param name="identifier">枚举的标识符</param>
        /// <param name="definitionToken">定义该枚举的词法Token</param>
        /// <param name="parserTree">枚举的语法树</param>
        /// <param name="usingsParserTree"></param>
        public InterfaceScope DeclareInterface(string identifier, IToken definitionToken,
            GorgeParser.InterfaceDeclarationContext parserTree, GorgeParser.ExpressionContext[] usingsParserTree)
        {
            var interfaceSymbol = new InterfaceSymbol(this, identifier, definitionToken.CodeLocation(), parserTree, usingsParserTree);
            AddSymbol(interfaceSymbol);
            var interfaceScope = interfaceSymbol.InterfaceScope;
            Interfaces.Add(interfaceSymbol, interfaceScope);
            return interfaceScope;
        }
        //
        // /// <summary>
        // /// 根据标识符获取对应的类符号，获取失败抛出异常
        // /// </summary>
        // /// <param name="identifier">待查标识符词法节点</param>
        // /// <param name="compileException">如果为true，则抛出编译异常，否则抛出编译器异常</param>
        // /// <returns>与标识符匹配的类符号</returns>
        // public ClassSymbol GetClassSymbol(ITerminalNode identifier, bool compileException = false)
        // {
        //     return GetSymbol<ClassSymbol>(identifier, compileException, SymbolType.Class);
        // }
        //
        // /// <summary>
        // /// 根据标识符获取对应的类符号，获取失败抛出异常
        // /// </summary>
        // /// <param name="identifier">待查标识符</param>
        // /// <param name="position">待查标识符代码位置</param>
        // /// <param name="compileException">如果为true，则抛出编译异常，否则抛出编译器异常</param>
        // /// <returns>与标识符匹配的类符号</returns>
        // public ClassSymbol GetClassSymbol(string identifier, CodeLocation position,
        //     bool compileException = false)
        // {
        //     return GetSymbol<ClassSymbol>(identifier, position, compileException, SymbolType.Class);
        // }
        //
        // /// <summary>
        // /// 根据标识符获取对应的枚举符号，获取失败抛出异常
        // /// </summary>
        // /// <param name="identifier">待查标识符词法节点</param>
        // /// <param name="compileException">如果为true，则抛出编译异常，否则抛出编译器异常</param>
        // /// <returns>与标识符匹配的枚举符号</returns>
        // public EnumSymbol GetEnumSymbol(ITerminalNode identifier, bool compileException = false)
        // {
        //     return GetSymbol<EnumSymbol>(identifier, compileException, SymbolType.Enum);
        // }
        //
        // /// <summary>
        // /// 根据标识符获取对应的枚举符号，获取失败抛出异常
        // /// </summary>
        // /// <param name="identifier">待查标识符</param>
        // /// <param name="position">待查标识符代码位置</param>
        // /// <param name="compileException">如果为true，则抛出编译异常，否则抛出编译器异常</param>
        // /// <returns>与标识符匹配的类符号</returns>
        // public EnumSymbol GetEnumSymbol(string identifier, CodeLocation position,
        //     bool compileException = false)
        // {
        //     return GetSymbol<EnumSymbol>(identifier, position, compileException, SymbolType.Enum);
        // }
        //
        // /// <summary>
        // /// 根据标识符获取对应的接口符号，获取失败抛出异常
        // /// </summary>
        // /// <param name="identifier">待查标识符词法节点</param>
        // /// <param name="compileException">如果为true，则抛出编译异常，否则抛出编译器异常</param>
        // /// <returns>与标识符匹配的接口符号</returns>
        // public InterfaceSymbol GetInterfaceSymbol(ITerminalNode identifier, bool compileException = false)
        // {
        //     return GetSymbol<InterfaceSymbol>(identifier, compileException, SymbolType.Interface);
        // }
        //
        // /// <summary>
        // /// 根据标识符获取对应的接口符号，获取失败抛出异常
        // /// </summary>
        // /// <param name="identifier">待查标识符</param>
        // /// <param name="position">待查标识符代码位置</param>
        // /// <param name="compileException">如果为true，则抛出编译异常，否则抛出编译器异常</param>
        // /// <returns>与标识符匹配的类符号</returns>
        // public InterfaceSymbol GetInterfaceSymbol(string identifier, CodeLocation position,
        //     bool compileException = false)
        // {
        //     return GetSymbol<InterfaceSymbol>(identifier, position, compileException, SymbolType.Interface);
        // }
        //
        // /// <summary>
        // /// 根据标识符获取对应的类型符号，获取失败抛出异常
        // /// </summary>
        // /// <param name="identifier">待查标识符</param>
        // /// <param name="position">待查标识符代码位置</param>
        // /// <param name="compileException">如果为true，则抛出编译异常，否则抛出编译器异常</param>
        // /// <returns>与标识符匹配的类符号</returns>
        // public TypeSymbol GetTypeSymbol(string identifier, CodeLocation position,
        //     bool compileException = false)
        // {
        //     return GetSymbol<TypeSymbol>(identifier, position, compileException, SymbolType.Class, SymbolType.Enum,
        //         SymbolType.Interface);
        // }

        public override string ToString()
        {
            if (Parent != null)
            {
                return $"{Parent}.{NamespaceName}";
            }

            return NamespaceName;
        }
    }
}