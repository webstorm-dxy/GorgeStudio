using System.Collections.Generic;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.Visitors
{
    /// <summary>
    /// 一轮编译，按源文件无序遍历，完成类型基本信息收集。
    /// 收集命名空间标识符，构建符号域。
    /// 收集using的语法树
    /// 收集类、方法和接口的标识符、语法树、声明点位置、修饰符和泛型信息，构建符号域。
    ///
    /// TODO 接口泛型尚未实现
    /// </summary>
    public class TypeIdentifierVisitor : GorgePanicableVisitor<int>
    {
        private readonly GlobalScope _globalScope;

        /// <summary>
        /// 用于处理类型标识的访问者类。在编译期间负责第一轮类型标识工作。
        /// </summary>
        /// <param name="globalScope">全局符号域</param>
        /// <param name="panicMode">恐慌模式，如果为true则不会立刻抛出编译错误，而是保存在PanicExceptions中，并尝试继续编译</param>
        public TypeIdentifierVisitor(GlobalScope globalScope, bool panicMode = false) : base(panicMode)
        {
            _globalScope = globalScope;
        }

        private NamespaceScope? _nowNamespaceScope;

        private List<GorgeParser.ExpressionContext> _nowUsings = new();

        public override int VisitSourceFile(GorgeParser.SourceFileContext context)
        {
            // 进入源文件时，重置当前命名空间为全局空间
            _nowNamespaceScope = _globalScope;
            _nowUsings.Clear();
            base.VisitSourceFile(context);
            _nowNamespaceScope = null;
            return 0;
        }

        public override int VisitUsingStatement(GorgeParser.UsingStatementContext context)
        {
            _nowUsings.Add(context.expression());
            return 0;
        }

        public override int VisitNamespaceStatement(GorgeParser.NamespaceStatementContext context)
        {
            NamespaceScope nowNamespace = _globalScope;
            foreach (var identifier in context.Identifier())
            {
                if (nowNamespace.TryGetSymbol(identifier.GetText(), out var symbol,null,
                        false, false))
                {
                    if (symbol is NamespaceSymbol namespaceSymbol)
                    {
                        nowNamespace = namespaceSymbol.NamespaceScope;
                        namespaceSymbol.ReferenceTokens.Add(identifier.Symbol.CodeLocation());
                    }
                    else
                    {
                        throw new GorgeCompileException($"已经存在名为{identifier}的{symbol.GetType()}");
                    }
                }
                else
                {
                    nowNamespace = nowNamespace.DeclareNamespace(identifier.GetText(), identifier.Symbol,
                        identifier.Symbol.CodeLocation().CodeRange);
                }
            }

            _nowNamespaceScope = nowNamespace;

            return 0;
        }

        /// <summary>
        /// 当前正在处理的符号
        /// </summary>
        private Symbol<string>? _nowSymbol;

        /// <summary>
        /// 当前正在处理的类符号域
        /// </summary>
        private ClassScope? _nowClassScope;

        public override int VisitClassDeclaration(GorgeParser.ClassDeclarationContext context)
        {
            if (_nowNamespaceScope == null)
            {
                throw new VisitorTemporaryContextException(nameof(_nowNamespaceScope));
            }

            var name = context.Identifier().GetText();
            var classScope =
                _nowNamespaceScope.DeclareClass(name, context.Identifier().Symbol, context, _nowUsings.ToArray());
            classScope.Usings.Add(_nowNamespaceScope);
            _nowClassScope = classScope;
            _nowSymbol = classScope.ClassSymbol;

            #region 系统类型识别和注册（临时方案）

            switch (name)
            {
                case "Injector":
                    CompileTempStatic.Injector = classScope.ClassSymbol;
                    break;
                case "IntArray":
                    CompileTempStatic.IntArray = classScope.ClassSymbol;
                    break;
                case "FloatArray":
                    CompileTempStatic.FloatArray = classScope.ClassSymbol;
                    break;
                case "BoolArray":
                    CompileTempStatic.BoolArray = classScope.ClassSymbol;
                    break;
                case "StringArray":
                    CompileTempStatic.StringArray = classScope.ClassSymbol;
                    break;
                case "ObjectArray":
                    CompileTempStatic.ObjectArray = classScope.ClassSymbol;
                    break;
                case "IntList":
                    CompileTempStatic.IntList = classScope.ClassSymbol;
                    break;
                case "FloatList":
                    CompileTempStatic.FloatList = classScope.ClassSymbol;
                    break;
                case "BoolList":
                    CompileTempStatic.BoolList = classScope.ClassSymbol;
                    break;
                case "StringList":
                    CompileTempStatic.StringList = classScope.ClassSymbol;
                    break;
                case "ObjectList":
                    CompileTempStatic.ObjectList = classScope.ClassSymbol;
                    break;
                default:
                    break;
            }

            #endregion

            // 收集类的修饰符信息
            foreach (var classModifierContext in context.classModifier())
            {
                Visit(classModifierContext);
            }

            // 收集类的泛型信息
            if (context.genericsDeclaration() is { } genericsDeclaration)
            {
                Visit(genericsDeclaration);
            }

            _nowClassScope = null;
            _nowSymbol = null;

            return default;
        }

        public override int VisitClassModifier(GorgeParser.ClassModifierContext context)
        {
            if (_nowSymbol == null)
            {
                throw new VisitorTemporaryContextException(nameof(_nowSymbol));
            }

            // TODO 目前只有native可用，所以出现就必然是Native
            _nowSymbol.AddModifier(ModifierType.Native, context.Native().Symbol);

            return default;
        }

        public override int VisitGenericsDeclaration(GorgeParser.GenericsDeclarationContext context)
        {
            if (_nowClassScope == null)
            {
                throw new VisitorTemporaryContextException(nameof(_nowClassScope));
            }

            foreach (var identifier in context.Identifier())
            {
                _nowClassScope.DeclarationGenerics(identifier.GetText(), identifier.Symbol);
            }

            return default;
        }

        public override int VisitEnumDeclaration(GorgeParser.EnumDeclarationContext context)
        {
            if (_nowNamespaceScope == null)
            {
                throw new VisitorTemporaryContextException(nameof(_nowNamespaceScope));
            }

            var name = context.Identifier().GetText();
            var enumScope =
                _nowNamespaceScope.DeclareEnum(name, context.Identifier().Symbol, context, _nowUsings.ToArray());
            enumScope.Usings.Add(_nowNamespaceScope);
            _nowSymbol = enumScope.EnumSymbol;

            // 收集枚举的修饰符信息
            foreach (var classModifierContext in context.classModifier())
            {
                Visit(classModifierContext);
            }

            _nowSymbol = null;
            return default;
        }

        public override int VisitInterfaceDeclaration(GorgeParser.InterfaceDeclarationContext context)
        {
            if (_nowNamespaceScope == null)
            {
                throw new VisitorTemporaryContextException(nameof(_nowNamespaceScope));
            }

            var name = context.Identifier().GetText();
            var interfaceScope =
                _nowNamespaceScope.DeclareInterface(name, context.Identifier().Symbol, context, _nowUsings.ToArray());
            interfaceScope.Usings.Add(_nowNamespaceScope);
            _nowSymbol = interfaceScope.InterfaceSymbol;

            // 收集枚举的修饰符信息
            foreach (var classModifierContext in context.classModifier())
            {
                Visit(classModifierContext);
            }

            _nowSymbol = null;
            return default;
        }
    }
}