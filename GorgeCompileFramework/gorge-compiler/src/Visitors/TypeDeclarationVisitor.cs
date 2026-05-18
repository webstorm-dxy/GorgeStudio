using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.CompileContext.Task;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeCompiler.Expression;
using Gorge.GorgeCompiler.Expression.PrimaryLevel.Type;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.Visitors
{
    /// <summary>
    /// 三轮编译，收集类和类成员的声明信息。
    /// Visit方法返回四轮编译任务表，各编译任务互相独立，可以以任意顺序完成其中的编译任务
    /// </summary>
    public class TypeDeclarationVisitor : GorgePanicableVisitor<HashSet<IImplementationCompileTask>>
    {
        private readonly HashSet<ISymbol> _visitedSymbols = new();

        public TypeDeclarationVisitor(bool panicMode = false) : base(panicMode)
        {
        }

        protected override HashSet<IImplementationCompileTask> DefaultResult => new();

        protected override HashSet<IImplementationCompileTask> AggregateResult(
            HashSet<IImplementationCompileTask> aggregate, HashSet<IImplementationCompileTask> nextResult)
        {
            aggregate.UnionWith(nextResult);
            return aggregate;
        }


        /// <summary>
        /// 编译目标全局符号域
        /// </summary>
        /// <param name="namespaceScope"></param>
        public HashSet<IImplementationCompileTask> CompileNamespace(NamespaceScope namespaceScope)
        {
            var implementationTasks = new HashSet<IImplementationCompileTask>();
            foreach (var typeSymbol in namespaceScope.Symbols.Values)
            {
                implementationTasks.UnionWith(Visit(typeSymbol));
            }

            return implementationTasks;
        }

        private TypeSymbol? _nowTypeSymbol;

        /// <summary>
        /// 对一个符号实施三轮编译
        /// </summary>
        /// <param name="symbol">待访问符号</param>
        private HashSet<IImplementationCompileTask> Visit(Symbol<string>? symbol)
        {
            // 如果目标符号已被访问，则不再访问
            if (_visitedSymbols.Contains(symbol))
            {
                return new HashSet<IImplementationCompileTask>();
            }

            _visitedSymbols.Add(symbol);

            var implementationTasks = new HashSet<IImplementationCompileTask>();

            switch (symbol)
            {
                case ClassSymbol classSymbol:
                    classSymbol.ClassScope.FreezeInheritance();
                    // 需要先访问该类的超类和实现接口
                    if (classSymbol.ClassScope.SuperClass != null)
                    {
                        implementationTasks.UnionWith(Visit(classSymbol.ClassScope.SuperClass));
                    }

                    foreach (var implementedInterface in classSymbol.ClassScope.ImplementedInterfaces.Keys)
                    {
                        implementationTasks.UnionWith(Visit(implementedInterface));
                    }

                    _nowTypeSymbol = classSymbol;
                    implementationTasks.UnionWith(Visit(classSymbol.ClassScope.ParserTree));
                    _nowTypeSymbol = null;
                    break;
                case InterfaceSymbol interfaceSymbol:
                    _nowTypeSymbol = interfaceSymbol;
                    implementationTasks.UnionWith(Visit(interfaceSymbol.InterfaceScope.ParserTree));
                    _nowTypeSymbol = null;
                    break;
                case EnumSymbol enumSymbol:
                    _nowTypeSymbol = enumSymbol;
                    implementationTasks.UnionWith(Visit(enumSymbol.EnumScope.ParserTree));
                    _nowTypeSymbol = null;
                    break;
                case NamespaceSymbol namespaceSymbol:
                    implementationTasks.UnionWith(CompileNamespace(namespaceSymbol.NamespaceScope));
                    break;
                default:
                    break;
            }

            return implementationTasks;
        }

        private ExpressionVisitor _nowTypeVisitor;
        private SymbolicGorgeType _nowClassType;
        private ClassSymbol? _nowClassSymbol;
        private MethodContainerScope _nowMethodContainerScope;
        private ExpressionVisitor _nowExpressionVisitor;

        public override HashSet<IImplementationCompileTask> VisitClassDeclaration(
            GorgeParser.ClassDeclarationContext context)
        {
            if (_nowTypeSymbol == null)
            {
                throw new VisitorTemporaryContextException(nameof(_nowTypeSymbol), context);
            }

            var implementationTasks = new HashSet<IImplementationCompileTask>();
            _nowClassSymbol = _nowTypeSymbol.Assert<ClassSymbol>();
            _nowMethodContainerScope = _nowClassSymbol.ClassScope;
            _nowClassType = _nowClassSymbol.Type;

            _nowAnnotationContainer = _nowClassSymbol.ClassScope;
            _nowExpressionVisitor = new ExpressionVisitor(_nowClassSymbol.ClassScope,PanicMode);
            _nowExpressionVisitor.AutoType = _nowClassType;
            foreach (var annotation in context.annotation())
            {
                implementationTasks.UnionWith(Visit(annotation));
            }


            _nowAnnotationContainer = null;

            // 解析方法体中的方法成员
            _nowTypeVisitor = new ExpressionVisitor(_nowClassSymbol.ClassScope,PanicMode);
            implementationTasks.UnionWith(Visit(context.classBody()));

            _nowClassSymbol.ClassScope.FreezeDeclaration();

            _nowClassSymbol = null;
            _nowMethodContainerScope = null;
            _nowExpressionVisitor = null;
            return implementationTasks;
        }

        #region 字段声明

        private IAnnotationContainer _nowAnnotationContainer;

        public override HashSet<IImplementationCompileTask> VisitFieldDeclaration(
            GorgeParser.FieldDeclarationContext context)
        {
            if (_nowClassSymbol == null)
            {
                throw new VisitorTemporaryContextException(nameof(_nowClassSymbol), context);
            }

            var implementationTasks = new HashSet<IImplementationCompileTask>();

            // 解析字段类型
            var type = _nowTypeVisitor.Visit(context.expression()[0]).Assert<IGorgeTypeExpression>().Type;

            var fieldScope = _nowClassSymbol.ClassScope.DeclareField(type, context.Identifier().GetText(),
                context.Identifier().Symbol, context);

            _nowAnnotationContainer = fieldScope;
            _nowExpressionVisitor.AutoType = type;
            foreach (var annotation in context.annotation())
            {
                implementationTasks.UnionWith(Visit(annotation));
            }

            _nowExpressionVisitor.AutoType = null;
            _nowAnnotationContainer = null;

            if (!_nowClassSymbol.IsNative && context.expression().Length == 2)
            {
                implementationTasks.Add(
                    new FieldInitializerImplementationCompileTask(fieldScope.FieldSymbol, context.expression()[1]));
            }

            return implementationTasks;
        }

        #endregion

        #region 注解声明

        private AnnotationScope _nowAnnotationScope;

        public override HashSet<IImplementationCompileTask> VisitAnnotation(GorgeParser.AnnotationContext context)
        {
            if (_nowAnnotationContainer == null)
            {
                throw new VisitorTemporaryContextException(nameof(_nowAnnotationContainer), context);
            }

            var implementationTasks = new HashSet<IImplementationCompileTask>();

            var annotationName = context.annotationIdentifier().Identifier().GetText();

            SymbolicGorgeType nowGenericType = null;
            if (context.genericType() != null)
            {
                nowGenericType = _nowExpressionVisitor.Visit(context.genericType().expression())
                    .Assert<IGorgeTypeExpression>().Type;
            }
            
            _nowAnnotationScope = _nowAnnotationContainer.DeclareAnnotation(annotationName, nowGenericType,
                context.annotationIdentifier(), context);

            if (context.annotationParameters() != null)
            {
                implementationTasks.UnionWith(Visit(context.annotationParameters()));
            }

            if (context.metadata() != null)
            {
                implementationTasks.UnionWith(Visit(context.metadata()));
            }

            _nowAnnotationScope = null;

            return implementationTasks;
        }

        public override HashSet<IImplementationCompileTask> VisitAnnotationParameter(
            GorgeParser.AnnotationParameterContext context)
        {
            if (_nowAnnotationScope == null)
            {
                throw new VisitorTemporaryContextException(nameof(_nowAnnotationScope), context);
            }

            var parameterName = context.Identifier().GetText();
            var parameterValue = _nowExpressionVisitor.Visit(context.expression()).Assert<IGorgeValueExpression>();
            if (!parameterValue.IsCompileConstant)
            {
                throw new GorgeCompilerException("注解参数值必须是编译时常量");
            }

            _nowAnnotationScope.DeclareAnnotationParameter(parameterName, parameterValue.CompileConstantValue,
                context.Identifier().Symbol, context);
            return new HashSet<IImplementationCompileTask>();
        }

        public override HashSet<IImplementationCompileTask> VisitMetadataEntry(GorgeParser.MetadataEntryContext context)
        {
            if (_nowAnnotationScope == null)
            {
                throw new VisitorTemporaryContextException(nameof(_nowAnnotationScope), context);
            }

            var metadataName = context.Identifier().GetText();
            var metadataType = _nowExpressionVisitor.Visit(context.expression()[0]).Assert<IGorgeTypeExpression>()
                .Type;
            var entrySymbol = _nowAnnotationScope.MetadataScope.DeclareEntry(metadataType, metadataName,
                context.Identifier().Symbol, context);

            if (_nowClassSymbol.IsNative)
            {
                return new HashSet<IImplementationCompileTask>();
            }
            else
            {
                if (context.expression().Length == 2)
                {
                    return new HashSet<IImplementationCompileTask>()
                    {
                        new MetadataEntryImplementationCompileTask(entrySymbol, _nowClassSymbol,
                            context.expression()[1])
                    };
                }

                throw new GorgeCompileException("非Native类的元数据必须有值");
            }
        }

        #endregion

        #region 方法声明

        private List<ParameterDeclaration2> _nowParameters;

        public override HashSet<IImplementationCompileTask> VisitMethodDeclaration(
            GorgeParser.MethodDeclarationContext context)
        {
            if (_nowMethodContainerScope == null)
            {
                throw new VisitorTemporaryContextException(nameof(_nowMethodContainerScope), context);
            }

            var implementationTasks = new HashSet<IImplementationCompileTask>();

            // 解析是否为static
            // TODO 暂时只有static一种修饰符，所以目前认为存在即为static
            var isStatic = context.methodModifier().Length > 0;

            // 解析方法名
            var methodName = context.Identifier().GetText();
            
            // 解析方法返回类型
            var returnType = _nowTypeVisitor.Visit(context.expression()).Assert<IGorgeTypeExpression>().Type;

            _nowParameters = new List<ParameterDeclaration2>();
            implementationTasks.UnionWith(Visit(context.parameterList()));


            var modifiers = new Dictionary<ModifierType, IToken>();

            if (isStatic)
            {
                modifiers.Add(ModifierType.Static, context.methodModifier().First().Static().Symbol);
            }
            
            var methodScope = _nowMethodContainerScope.DeclareMethod(methodName, returnType, new ParameterList(
                _nowParameters.Select(p => new Tuple<SymbolicGorgeType, string>(p.Type, p.Name))
                    .ToList(),_nowMethodContainerScope.TypeGenericsSymbols), modifiers, context.Identifier().Symbol, context);

            foreach (var parameter in _nowParameters)
            {
                methodScope.AddParameter(parameter.Name, parameter.Type, parameter.Token, parameter.CodeRange);
            }

            _nowAnnotationContainer = methodScope;
            _nowExpressionVisitor.AutoType = _nowClassType;
            foreach (var annotation in context.annotation())
            {
                implementationTasks.UnionWith(Visit(annotation));
            }

            _nowExpressionVisitor.AutoType = null;
            _nowAnnotationContainer = null;

            if (!_nowMethodContainerScope.IsNative && _nowMethodContainerScope is ClassScope)
            {
                var blockList = context.codeBlockList();
                if (blockList == null)
                {
                    throw new GorgeCompileException("非Native类的方法必须有方法体");
                }

                implementationTasks.Add(new MethodImplementationCompileTask(methodScope.MethodSymbol, blockList));
            }


            return implementationTasks;
        }

        public override HashSet<IImplementationCompileTask> VisitParameter(GorgeParser.ParameterContext context)
        {
            var parameterType = _nowTypeVisitor.Visit(context.expression()).Assert<IGorgeTypeExpression>().Type;
            var parameterName = context.Identifier().GetText();

            _nowParameters.Add(new ParameterDeclaration2(parameterName, parameterType, context.Identifier().Symbol,
                context));
            return new HashSet<IImplementationCompileTask>();
        }

        #endregion

        #region 构造方法声明

        public override HashSet<IImplementationCompileTask> VisitConstructorDeclaration(
            GorgeParser.ConstructorDeclarationContext context)
        {
            if (_nowClassSymbol == null)
            {
                throw new VisitorTemporaryContextException(nameof(_nowClassSymbol), context);
            }
            
            var implementationTasks = new HashSet<IImplementationCompileTask>();

            // 解析是否为injector
            // TODO 暂时只有injector一种修饰符，所以目前认为存在即为injector
            var isInjector = context.constructorModifier().Length > 0;

            _nowParameters = new List<ParameterDeclaration2>();
            implementationTasks.UnionWith(Visit(context.parameterList()));

            var modifiers = new Dictionary<ModifierType, IToken>();

            if (isInjector)
            {
                modifiers.Add(ModifierType.Injector, context.constructorModifier().First().Injector().Symbol);
            }

            var constructorScope = _nowClassSymbol.ClassScope.ConstructorGroupScope.DeclareConstructor(
                new ParameterList(_nowParameters.Select(p => new Tuple<SymbolicGorgeType, string>(p.Type, p.Name))
                    .ToList(),_nowClassSymbol.ClassScope.GenericsSymbols), modifiers, context.Identifier().Symbol, context);

            foreach (var parameter in _nowParameters)
            {
                constructorScope.AddParameter(parameter.Name, parameter.Type, parameter.Token, parameter.CodeRange);
            }

            _nowAnnotationContainer = constructorScope;
            _nowExpressionVisitor.AutoType = _nowClassType;
            foreach (var annotation in context.annotation())
            {
                implementationTasks.UnionWith(Visit(annotation));
            }

            _nowExpressionVisitor.AutoType = null;
            _nowAnnotationContainer = null;

            if (!_nowMethodContainerScope.IsNative)
            {
                var codeBlockList = context.codeBlockList();
                if (codeBlockList == null)
                {
                    throw new GorgeCompileException("非Native类的构造方法必须有方法体");
                }

                implementationTasks.Add(
                    new ConstructorImplementationCompileTask(constructorScope.ConstructorSymbol,context,
                        context.superClassConstructor(), codeBlockList));
            }

            return implementationTasks;
        }

        #endregion

        #region 枚举

        private EnumSymbol _nowEnum;

        public override HashSet<IImplementationCompileTask> VisitEnumDeclaration(
            GorgeParser.EnumDeclarationContext context)
        {
            if (_nowTypeSymbol == null)
            {
                throw new VisitorTemporaryContextException(nameof(_nowTypeSymbol), context);
            }

            var implementationTasks = new HashSet<IImplementationCompileTask>();
            var enumSymbol = _nowTypeSymbol.Assert<EnumSymbol>();
            _nowEnum = enumSymbol;

            foreach (var enumConstant in context.enumConstant())
            {
                implementationTasks.UnionWith(Visit(enumConstant));
            }

            enumSymbol.EnumScope.Enum = enumSymbol.ToEnum();

            return implementationTasks;
        }

        public override HashSet<IImplementationCompileTask> VisitEnumConstant(GorgeParser.EnumConstantContext context)
        {
            if (_nowEnum == null)
            {
                throw new VisitorTemporaryContextException(nameof(_nowEnum), context);
            }

            var implementationTasks = new HashSet<IImplementationCompileTask>();

            var enumValueIdentifier = context.Identifier().GetText();
            var enumValueSymbol = _nowEnum.EnumScope.GetSymbol<EnumValueSymbol>(context.Identifier(),false);
            // var enumValueScope =
            //     _nowEnum.EnumScope.DeclareEnumValue(enumValueIdentifier, context.Identifier().Symbol, context);
            var enumValueScope = enumValueSymbol.EnumValueScope;

            _nowAnnotationContainer = enumValueScope;
            _nowExpressionVisitor = new ExpressionVisitor(enumValueScope,PanicMode);
            _nowExpressionVisitor.AutoType = null;
            foreach (var annotation in context.annotation())
            {
                implementationTasks.UnionWith(Visit(annotation));
            }

            _nowExpressionVisitor = null;
            _nowAnnotationContainer = null;
            return implementationTasks;
        }

        #endregion

        #region 接口

        public override HashSet<IImplementationCompileTask> VisitInterfaceDeclaration(
            GorgeParser.InterfaceDeclarationContext context)
        {
            if (_nowTypeSymbol == null)
            {
                throw new VisitorTemporaryContextException(nameof(_nowTypeSymbol), context);
            }

            var implementationTasks = new HashSet<IImplementationCompileTask>();

            var interfaceSymbol = _nowTypeSymbol.Assert<InterfaceSymbol>();
            _nowMethodContainerScope = interfaceSymbol.InterfaceScope;

            _nowExpressionVisitor = new ExpressionVisitor(_nowMethodContainerScope,PanicMode);
            _nowTypeVisitor = _nowExpressionVisitor;

            // 解析接口方法
            foreach (var method in context.methodDeclaration())
            {
                implementationTasks.UnionWith(Visit(method));
            }

            _nowExpressionVisitor = null;
            _nowMethodContainerScope = null;

            interfaceSymbol.InterfaceScope.FreezeDeclaration();
            return implementationTasks;
        }

        #endregion
    }


    public class ParameterDeclaration2
    {
        public ParameterDeclaration2(string name, SymbolicGorgeType type, IToken token, CodeRange codeRange)
        {
            Name = name;
            Type = type;
            Token = token;
            CodeRange = codeRange;
        }

        public string Name { get; }
        public SymbolicGorgeType Type { get; }
        public IToken Token { get; }
        public CodeRange CodeRange { get; }
    }
}