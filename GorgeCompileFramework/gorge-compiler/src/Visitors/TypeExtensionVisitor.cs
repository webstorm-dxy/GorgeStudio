#nullable enable
using System.Diagnostics.CodeAnalysis;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Expression;
using Gorge.GorgeCompiler.Expression.PrimaryLevel;
using Gorge.GorgeCompiler.Expression.PrimaryLevel.Type;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.Visitors
{
    /// <summary>
    /// 二轮编译，按源文件无序遍历，完成类型关系信息收集。
    /// 收集源文件的命名空间导入信息，存储至类符号域。
    /// 收集类的继承和接口实现信息，构造继承图，存储于全局符号域。
    /// </summary>
    public class TypeExtensionVisitor : GorgePanicableVisitor<int>
    {
        // TODO 这里还需要再想，分析超类的时候就已经是class的Scope了
        // TODO Global->命名空间->类
        // TODO 文件似乎不应该有自己的scope，似乎只是容纳using信息，这应该最终被纳入类的scope中参与分析
        // TODO 一轮编译似乎可以收集完整的namespace信息和using信息，因为不涉及继承
        // TODO 一轮编译显然不能完成using，因为包含对namespace的引用
        // TODO superclass包含对using的使用，所以需要在using后
        // TODO 泛型是自身引入的新类名，所以也在第一轮编译中完成收集

        public TypeExtensionVisitor(bool panicMode = false) : base(panicMode)
        {
        }

        /// <summary>
        /// 编译目标全局符号域
        /// </summary>
        /// <param name="namespaceScope"></param>
        public void CompileNamespace(NamespaceScope namespaceScope)
        {
            foreach (var typeSymbol in namespaceScope.Symbols.Values)
            {
                Visit(typeSymbol);
            }
        }

        private TypeSymbol? _nowTypeSymbol;

        /// <summary>
        /// 对一个符号实施二轮编译
        /// </summary>
        /// <param name="symbol">待访问符号</param>
        private void Visit([AllowNull] Symbol<string> symbol)
        {
            switch (symbol)
            {
                case ClassSymbol classSymbol:
                    foreach (var usingExpression in classSymbol.ClassScope.UsingsParserTree)
                    {
                        classSymbol.ClassScope.Usings.Add(new ExpressionVisitor(classSymbol.ClassScope, PanicMode)
                            .Visit(usingExpression).Assert<NamespaceReferenceExpression>().Symbol.NamespaceScope);
                    }

                    _nowTypeSymbol = classSymbol;
                    Visit(classSymbol.ClassScope.ParserTree);
                    _nowTypeSymbol = null;
                    break;
                case InterfaceSymbol interfaceSymbol:
                    foreach (var usingExpression in interfaceSymbol.InterfaceScope.UsingsParserTree)
                    {
                        interfaceSymbol.InterfaceScope.Usings.Add(
                            new ExpressionVisitor(interfaceSymbol.InterfaceScope, PanicMode)
                                .Visit(usingExpression).Assert<NamespaceReferenceExpression>().Symbol.NamespaceScope);
                    }

                    _nowTypeSymbol = interfaceSymbol;
                    Visit(interfaceSymbol.InterfaceScope.ParserTree);
                    _nowTypeSymbol = null;
                    break;
                case EnumSymbol enumSymbol:
                    foreach (var usingExpression in enumSymbol.EnumScope.UsingsParserTree)
                    {
                        enumSymbol.EnumScope.Usings.Add(new ExpressionVisitor(enumSymbol.EnumScope, PanicMode)
                            .Visit(usingExpression).Assert<NamespaceReferenceExpression>().Symbol.NamespaceScope);
                    }

                    _nowTypeSymbol = enumSymbol;
                    Visit(enumSymbol.EnumScope.ParserTree);
                    _nowTypeSymbol = null;
                    break;
                case NamespaceSymbol namespaceSymbol:
                    CompileNamespace(namespaceSymbol.NamespaceScope);
                    break;
                default:
                    break;
            }
        }

        public override int VisitClassDeclaration(GorgeParser.ClassDeclarationContext context)
        {
            if (_nowTypeSymbol == null)
            {
                throw new VisitorTemporaryContextException(nameof(_nowTypeSymbol), context);
            }

            var thisClassSymbol = _nowTypeSymbol.Assert<ClassSymbol>();

            if (context.superclass() != null)
            {
                var superClassExpression = new ExpressionVisitor(thisClassSymbol.ClassScope, PanicMode)
                    .Visit(context.superclass())
                    .Assert<IGorgeTypeExpression>();

                var superClassSymbol = superClassExpression.Type
                    .Assert<ClassType>(superClassExpression.ExpressionLocation).Symbol;

                thisClassSymbol.ClassScope.DeclareInheritance(superClassSymbol);
            }

            if (context.superInterfaces() != null)
            {
                foreach (var interfaceContext in context.superInterfaces().expression())
                {
                    var superInterfaceExpression = new ExpressionVisitor(thisClassSymbol.ClassScope, PanicMode)
                        .Visit(interfaceContext).Assert<IGorgeTypeExpression>();
                    var superInterfaceSymbol = superInterfaceExpression.Type
                        .Assert<InterfaceType>(superInterfaceExpression.ExpressionLocation).Symbol;
                    thisClassSymbol.ClassScope.DeclareImplementation(superInterfaceSymbol);
                }
            }

            return 0;
        }

        private EnumSymbol _nowEnum;

        public override int VisitEnumDeclaration(GorgeParser.EnumDeclarationContext context)
        {
            if (_nowTypeSymbol == null)
            {
                throw new VisitorTemporaryContextException(nameof(_nowTypeSymbol), context);
            }

            var enumSymbol = _nowTypeSymbol.Assert<EnumSymbol>();
            _nowEnum = enumSymbol;

            foreach (var enumConstant in context.enumConstant())
            {
                Visit(enumConstant);
            }

            return 0;
        }

        public override int VisitEnumConstant(GorgeParser.EnumConstantContext context)
        {
            if (_nowEnum == null)
            {
                throw new VisitorTemporaryContextException(nameof(_nowEnum), context);
            }

            var enumValueIdentifier = context.Identifier().GetText();
            var enumValueScope =
                _nowEnum.EnumScope.DeclareEnumValue(enumValueIdentifier, context.Identifier().Symbol, context);

            return 0;
        }
    }
}