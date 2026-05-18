using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeCompiler.Exceptions.CompilerException;
using Gorge.GorgeLanguage.Objective;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.CompileContext.Scope
{
    /// <summary>
    /// 类符号域，用于存储类相关的符号信息。
    /// 包含方法、字段等符号。
    /// </summary>
    public class ClassScope : MethodContainerScope, IAnnotationContainer, IDelegateImplementationContainer
    {
        public override HashSet<SymbolType> AllowedSymbolTypes { get; } = new()
        {
            SymbolType.Field,
            SymbolType.MethodGroup,
            SymbolType.Generics
        };

        /// <summary>
        /// 本域的类符号
        /// </summary>
        public readonly ClassSymbol ClassSymbol;

        /// <summary>
        /// 泛型符号表。
        /// 按定义顺序
        /// </summary>
        public readonly List<GenericsSymbol> GenericsSymbols = new();

        /// <summary>
        /// Using表达式
        /// </summary>
        public readonly GorgeParser.ExpressionContext[] UsingsParserTree;

        /// <summary>
        /// 本类的语法树
        /// </summary>
        public readonly GorgeParser.ClassDeclarationContext ParserTree;

        /// <summary>
        /// 继承的类
        /// </summary>
        public ClassSymbol? SuperClass { get; private set; }

        /// <summary>
        /// 实现的接口。
        /// key是接口符号，value是实现该接口的方法表，其中数组索引是接口中的方法编号，数组值是对应实现的接口编号（null为未确定实现）
        /// </summary>
        public readonly Dictionary<InterfaceSymbol, Lazy<int?[]>> ImplementedInterfaces = new();

        /// <summary>
        /// 实现的接口。
        /// key是接口符号，value是实现该接口的方法表，其中数组索引是接口中的方法编号，数组值是对应实现的接口编号
        /// </summary>
        public readonly Dictionary<string, int[]> CheckedImplementedInterfaces = new();

        /// <summary>
        /// 类的声明信息
        /// </summary>
        public ClassDeclaration Declaration { get; set; }

        /// <summary>
        /// 类内字段
        /// </summary>
        public readonly Dictionary<FieldSymbol, FieldScope> Fields = new();

        /// <summary>
        /// 构造方法组
        /// </summary>
        public readonly ConstructorGroupScope ConstructorGroupScope;

        /// <summary>
        /// 类所在的源文件地址
        /// </summary>
        public readonly string SourceFilePath;

        /// <summary>
        /// 本字段的注解
        /// </summary>
        public readonly List<AnnotationScope> Annotations = new();

        /// <summary>
        /// 方法重写表
        /// key是被重写方法编号，value是重写方法编号
        /// </summary>
        public readonly Dictionary<int, int> MethodOverrides = new();

        /// <summary>
        /// 注入器构造方法表
        /// key是注入器构造方法编号，value是构造方法符号
        /// </summary>
        public readonly Dictionary<int, ConstructorSymbol> InjectorConstructors = new();

        /// <summary>
        /// 注入器构造方法对应实现的构造方法编号。
        /// 数组索引是注入器构造方法的编号，数组值是对应实现的构造方法编号。
        /// </summary>
        public int[] InjectorConstructorImplementationId { get; private set; }

        /// <summary>
        /// 成员计数器
        /// </summary>
        public ClassMemberCounter MemberCounter
        {
            get
            {
                if (_memberCounter == null)
                {
                    EnsureInheritanceFreeze();
                    if (SuperClass == null)
                    {
                        _memberCounter = new ClassMemberCounter();
                    }
                    else
                    {
                        SuperClass.ClassScope.EnsureDeclarationFreeze();
                        _memberCounter = SuperClass.ClassScope.MemberCounter.GetChildCounter();
                    }
                }

                return _memberCounter;
            }
        }

        private ClassMemberCounter _memberCounter;

        /// <summary>
        /// 注入器符号域
        /// </summary>
        public readonly InjectorScope InjectorScope;

        public ClassScope(NamespaceScope parentNamespace, ClassSymbol classSymbol,
            GorgeParser.ClassDeclarationContext parserTree,
            GorgeParser.ExpressionContext[] usingsParserTree) : base(null)
        {
            Usings.Add(parentNamespace);
            ClassSymbol = classSymbol;
            ParserTree = parserTree;
            UsingsParserTree = usingsParserTree;
            SourceFilePath = parserTree.Start.TokenSource.SourceName;
            ConstructorGroupScope = new ConstructorGroupScope(this);
            InjectorScope = new InjectorScope(this);
            // 由于自身为根，需要补充在namespace中添加下属
            parentNamespace.SubScopes.Add(this);
        }

        /// <summary>
        /// 在类中声明泛型符号
        /// </summary>
        /// <param name="identifier">泛型标识符</param>
        /// <param name="definitionToken">定义符号的词法单元</param>
        /// <returns>泛型符号</returns>
        public GenericsSymbol DeclarationGenerics(string identifier, IToken definitionToken)
        {
            EnsureDeclarationNotFreeze();
            var genericSymbol = new GenericsSymbol(this, identifier, definitionToken.CodeLocation());
            AddSymbol(genericSymbol);
            GenericsSymbols.Add(genericSymbol);
            return genericSymbol;
        }

        // public GenericsSymbol GetGenericsSymbol(string identifier, CodeLocation position,
        //     bool compileException = false)
        // {
        //     return GetSymbol<GenericsSymbol>(identifier, position, compileException, SymbolType.Generics);
        // }

        #region 继承和实现

        /// <summary>
        /// 继承和实现关系是否被冻结。
        /// 如果被冻结，则不能再声明继承或实现。
        /// </summary>
        public bool InheritanceFrozen { get; private set; }

        /// <summary>
        /// 冻结继承和实现
        /// </summary>
        public void FreezeInheritance()
        {
            InheritanceFrozen = true;
        }

        /// <summary>
        /// 确保继承关系尚未被冻结，如果冻结则抛出异常
        /// </summary>
        /// <exception cref="DeclareAfterFreezeException"></exception>
        protected void EnsureInheritanceNotFreeze()
        {
            if (InheritanceFrozen)
            {
                throw new DeclareAfterFreezeException();
            }
        }

        /// <summary>
        /// 确保继承关系已被冻结，如果尚未冻结则抛出异常
        /// </summary>
        /// <exception cref="DeclareBeforeFreezeException"></exception>
        protected void EnsureInheritanceFreeze()
        {
            if (!InheritanceFrozen)
            {
                throw new DeclareBeforeFreezeException();
            }
        }

        /// <summary>
        /// 在类中声明继承的类
        /// </summary>
        /// <param name="inheritedClass">继承的类</param>
        public void DeclareInheritance(ClassSymbol inheritedClass)
        {
            EnsureInheritanceNotFreeze();
            if (SuperClass != null)
            {
                throw new MultipleInheritanceException();
            }

            if (!CheckCircularInheritance(inheritedClass, out var inheritanceCycle))
            {
                throw new CircularInheritanceException(inheritanceCycle);
            }

            SuperClass = inheritedClass;
            Parent = inheritedClass.ClassScope;
        }

        /// <summary>
        /// 检查循环继承
        /// </summary>
        /// <param name="classToInherit">拟继承的类</param>
        /// <param name="inheritanceCycle">如果有循环继承，则表示继承循环</param>
        /// <returns>如果没有循环继承则返回true</returns>
        private bool CheckCircularInheritance(ClassSymbol classToInherit, out List<ClassSymbol> inheritanceCycle)
        {
            // 待继承类的各级父类都能被本类继承，则不存在循环

            // 如果待继承的类和本类相同，则发现循环继承，创建继承环
            if (classToInherit.Equals(ClassSymbol))
            {
                inheritanceCycle = new List<ClassSymbol> {classToInherit};
                return false;
            }

            // 如果待继承的类没有父类，则检查停止
            var inheritSuperClass = classToInherit.ClassScope.SuperClass;
            if (inheritSuperClass == null)
            {
                inheritanceCycle = null;
                return true;
            }

            // 否则检查待继承类的父类能否被本类继承
            if (CheckCircularInheritance(inheritSuperClass, out inheritanceCycle))
            {
                return true;
            }

            inheritanceCycle.Add(classToInherit);
            return false;
        }

        /// <summary>
        /// 在类中声明实现的接口
        /// </summary>
        /// <param name="implementedInterface">实现的接口</param>
        public void DeclareImplementation(InterfaceSymbol implementedInterface)
        {
            EnsureInheritanceNotFreeze();
            if (ImplementedInterfaces.ContainsKey(implementedInterface))
            {
                throw new DuplicateImplementationException(implementedInterface);
            }

            ImplementedInterfaces.Add(implementedInterface, new Lazy<int?[]>(() =>
            {
                implementedInterface.InterfaceScope.EnsureDeclarationFreeze();
                return new int?[implementedInterface.InterfaceScope.MethodCount];
            }));
        }

        #endregion

        public override void FreezeDeclaration()
        {
            base.FreezeDeclaration();
            foreach (var (_, fieldScope) in Fields)
            {
                fieldScope.FreezeDeclaration();
            }

            ConstructorGroupScope.FreezeDeclaration();

            foreach (var (_, methodGroupScope) in MethodGroups)
            {
                methodGroupScope.FreezeDeclaration();
            }

            MemberCounter.Freeze();
            // 检查是否有接口方法未被实现
            foreach (var (interfaceSymbol, implementationMethodIds) in ImplementedInterfaces)
            {
                var implementMethods = new int[implementationMethodIds.Value.Length];
                for (var i = 0; i < implementationMethodIds.Value.Length; i++)
                {
                    if (implementationMethodIds.Value[i] == null)
                    {
                        var method = interfaceSymbol.InterfaceScope.Methods[i];
                        throw new GorgeCompileException(
                            $"没有实现{interfaceSymbol.Identifier}接口的{method.MethodScope.ParentMethodGroupScope.MethodGroupSymbol.Identifier}({method.Identifier})方法");
                    }

                    implementMethods[i] = implementationMethodIds.Value[i].Value;
                }

                CheckedImplementedInterfaces.Add(interfaceSymbol.Type.ToGorgeType().FullName, implementMethods);
            }

            // 为注入器构造方法寻找实现，检查是否有注入器构造方法未被实现
            InjectorConstructorImplementationId = new int[MemberCounter.InjectorConstructorId];
            for (var i = 0; i < MemberCounter.InjectorConstructorId; i++)
            {
                var injectorConstructor = GetInjectorConstructor(i);
                if (!ConstructorGroupScope.TryGetConstructor(injectorConstructor.Identifier, out var constructorSymbol))
                {
                    throw new GorgeCompileException($"没有实现参数表为{injectorConstructor.Identifier}的构造方法");
                }

                InjectorConstructorImplementationId[i] = constructorSymbol.Id;
            }

            // TODO 可能可以提前到字段声明冻结时

            #region 为具备Inject注解的字段添加Injector字段

            foreach (var (fieldSymbol, fieldScope) in Fields)
            {
                var injectAnnotation = fieldScope.Annotations.FirstOrDefault(a => a.AnnotationIdentifier == "Inject");
                if (injectAnnotation == null)
                {
                    continue;
                }

                string injectorFieldName;
                if (injectAnnotation.TryGetAnnotationParameter("name", out var nameParameter))
                {
                    if (nameParameter.Value is not string stringName)
                    {
                        throw new GorgeCompileException("Inject注解的name字段类型必须是字符串");
                    }

                    injectorFieldName = stringName;
                }
                else
                {
                    injectorFieldName = fieldSymbol.Identifier;
                }

                var injectorFieldType = injectAnnotation.GenericType ?? fieldSymbol.Type;
                var hasDefaultValue = injectAnnotation.MetadataScope.TryGetSymbol("defaultValue", out _, null, false);

                InjectorScope.DeclareInjectorField(injectorFieldType, injectorFieldName, hasDefaultValue,
                    injectAnnotation.DefinitionToken, injectAnnotation.DefinitionRange);
            }

            #endregion

            Declaration = ToClassDeclaration();
        }

        private ClassDeclaration ToClassDeclaration()
        {
            return new ClassDeclaration(ClassSymbol.Type, ClassSymbol.Modifiers.ContainsKey(ModifierType.Native),
                SuperClass?.ClassScope?.Declaration,
                ImplementedInterfaces.Select(i => i.Key.InterfaceScope.Interface).ToArray<GorgeInterface>(),
                Fields.Values.Select(f => f.FieldInformation).ToArray(),
                (from methodGroup in MethodGroups
                    from method in methodGroup.Value.Methods
                    where !method.Key.Modifiers.ContainsKey(ModifierType.Static)
                    select method.Value.MethodInformation).ToArray(),
                (from methodGroup in MethodGroups
                    from method in methodGroup.Value.Methods
                    where method.Key.Modifiers.ContainsKey(ModifierType.Static)
                    select method.Value.MethodInformation).ToArray(),
                (from constructor in ConstructorGroupScope.Constructors
                    select constructor.Value.ConstructorInformation).ToArray(),
                // 目前Constructor表中包含Injector Constructor
                (from constructor in ConstructorGroupScope.Constructors
                    where constructor.Key.Modifiers.ContainsKey(ModifierType.Injector)
                    select constructor.Value.ConstructorInformation).ToArray(),
                (from injectorField in InjectorScope.InjectorFields
                    select injectorField.Key.ToInjectorFieldInformation()).ToArray(),
                Annotations.Select(a => a.ToAnnotation()).ToArray(),
                MemberCounter.FieldIndex,
                MemberCounter.MethodId,
                MethodOverrides,
                CheckedImplementedInterfaces,
                MemberCounter.StaticMethodId,
                MemberCounter.ConstructorId,
                MemberCounter.InjectorConstructorId,
                InjectorConstructorImplementationId,
                MemberCounter.InjectorFieldIndex,
                MemberCounter.InjectorFieldDefaultValueIndex,
                MemberCounter.InjectorFieldId
            );
        }

        /// <summary>
        /// 在类中声明字段
        /// </summary>
        /// <param name="fieldType">字段的类型</param>
        /// <param name="identifier">字段的标识符</param>
        /// <param name="definitionToken">定义该字段的词法Token</param>
        /// <param name="parserTree">字段的语法树</param>
        /// <returns>字段符号域</returns>
        public FieldScope DeclareField(SymbolicGorgeType fieldType, string identifier, IToken definitionToken,
            GorgeParser.FieldDeclarationContext parserTree)
        {
            EnsureDeclarationNotFreeze();
            MemberCounter.CountField(fieldType, out var id, out var index);
            var fieldSymbol = new FieldSymbol(this, fieldType, identifier, id, index, definitionToken.CodeLocation(),
                parserTree);
            AddSymbol(fieldSymbol);
            var fieldScope = fieldSymbol.FieldScope;
            Fields.Add(fieldSymbol, fieldScope);
            return fieldScope;
        }

        public override bool IsNative => ClassSymbol.IsNative;

        public override SymbolicGorgeType Type => ClassSymbol.Type;
        public override IReadOnlyList<GenericsSymbol> TypeGenericsSymbols => GenericsSymbols;

        public override MethodScope DeclareMethod(string name, SymbolicGorgeType returnType,
            ParameterList parameterList,
            Dictionary<ModifierType, IToken> modifiers, IToken definitionToken,
            GorgeParser.MethodDeclarationContext parserTree)
        {
            EnsureDeclarationNotFreeze();
            MethodGroupScope methodGroups;
            if (TryGetSymbol(name, out var symbol, null, false, false))
            {
                if (symbol is not MethodGroupSymbol methodGroupSymbol)
                {
                    throw new UnexpectedSymbolTypeCompilerException(
                        new List<CodeLocation>() {definitionToken.CodeLocation()},
                        symbol.SymbolType, SymbolType.MethodGroup);
                }

                methodGroups = methodGroupSymbol.MethodGroupScope;
            }
            else
            {
                methodGroups = DeclareMethodGroup(name, definitionToken, parserTree);
            }

            var isStatic = modifiers.ContainsKey(ModifierType.Static);
            int id;
            if (isStatic)
            {
                MemberCounter.CountStaticMethod(out id);
            }
            else
            {
                MemberCounter.CountMethod(out id);
            }

            var method = methodGroups.DeclareMethod(returnType, parameterList, id, definitionToken, parserTree);
            foreach (var (modifier, token) in modifiers)
            {
                method.MethodSymbol.AddModifier(modifier, token);
            }

            // TODO 此处应当检测static方法不和接口或超类方法签名冲突
            // TODO 可能合并到某种MethodGroup统一管理

            if (!isStatic)
            {
                // 检查方法是否实现了接口方法
                foreach (var (interfaceSymbol, implementMethodIds) in ImplementedInterfaces)
                {
                    if (interfaceSymbol.InterfaceScope.TryGetMethod(name, parameterList, out var interfaceMethodSymbol))
                    {
                        if (!Equals(interfaceMethodSymbol.ReturnType, returnType))
                        {
                            throw new GorgeCompileException("实现接口方法必须使用相同的返回类型", definitionToken.CodeLocation());
                        }

                        implementMethodIds.Value[interfaceMethodSymbol.Id] = id;
                    }
                }

                // 检查方法是否重写了超类方法
                var nowSuperClass = SuperClass;
                while (nowSuperClass != null)
                {
                    if (nowSuperClass.ClassScope.TryGetMethod(name, parameterList, out var overrideMethodSymbol))
                    {
                        if (!Equals(overrideMethodSymbol.ReturnType, returnType))
                        {
                            throw new GorgeCompileException("重写超类方法必须使用相同的返回类型", definitionToken.CodeLocation());
                        }

                        MethodOverrides[overrideMethodSymbol.Id] = id;
                    }

                    nowSuperClass = nowSuperClass.ClassScope.SuperClass;
                }
            }

            return method;
        }

        /// <summary>
        /// 在类中声明方法组
        /// </summary>
        /// <param name="identifier">方法组标识符，即组内方法类名</param>
        /// <param name="definitionToken">定义该方法组首方法的词法Token</param>
        /// <param name="parserTree">定义该方法组首方法的语法树</param>
        /// <returns>方法组符号域</returns>
        private MethodGroupScope DeclareMethodGroup(string identifier, IToken definitionToken,
            GorgeParser.MethodDeclarationContext parserTree)
        {
            EnsureDeclarationNotFreeze();
            MethodGroupScope superMethodGroupScope = null;
            if (SuperClass != null)
            {
                if (SuperClass.ClassScope.TryGetSymbol(identifier, out var symbol, null, true, false))
                {
                    if (symbol is MethodGroupSymbol mgSymbol)
                    {
                        superMethodGroupScope = mgSymbol.MethodGroupScope;
                    }
                    else
                    {
                        throw new GorgeCompileException($"名为{identifier}的成员已被定义且不是方法组名，是{symbol.GetType()}");
                    }
                }
            }

            var methodGroupSymbol = new MethodGroupSymbol(this, superMethodGroupScope, identifier,
                definitionToken.CodeLocation(),
                parserTree, AllowedMethodModifierTypes);
            AddSymbol(methodGroupSymbol);
            var methodGroupScope = methodGroupSymbol.MethodGroupScope;
            MethodGroups.Add(methodGroupSymbol, methodGroupScope);
            return methodGroupScope;
        }
        //
        // /// <summary>
        // /// 根据标识符获取对应的字段符号，获取失败抛出异常
        // /// </summary>
        // /// <param name="identifier">待查标识符词法节点</param>
        // /// <param name="compileException">如果为true，则抛出编译异常，否则抛出编译器异常</param>
        // /// <returns>与标识符匹配的字段符号</returns>
        // public FieldSymbol GetFieldSymbol(ITerminalNode identifier, bool compileException = false)
        // {
        //     return GetSymbol<FieldSymbol>(identifier, compileException, SymbolType.Field);
        // }

        public ConstructorSymbol GetInjectorConstructor(int id)
        {
            if (InjectorConstructors.TryGetValue(id, out var constructorSymbol))
            {
                return constructorSymbol;
            }

            if (SuperClass != null)
            {
                return SuperClass.ClassScope.GetInjectorConstructor(id);
            }

            throw new GorgeCompilerException($"尝试获取编号为{id}的注入器构造方法，但实际不存在");
        }

        /// <summary>
        /// 尝试获取注入器构造方法，如果没找到则返回false，找到一个返回true，找到多个报错
        /// </summary>
        /// <param name="argumentTypes"></param>
        /// <param name="constructorSymbol"></param>
        /// <returns></returns>
        /// <exception cref="GorgeCompileException"></exception>
        public bool TryGetInjectorConstructorByArgumentTypes(SymbolicGorgeType[] argumentTypes,
            out ConstructorSymbol constructorSymbol)
        {
            // 符合调用参数的重载
            var selectedConstructors = new List<ConstructorSymbol>();
            for (var i = 0; i < MemberCounter.InjectorConstructorId; i++)
            {
                constructorSymbol = GetInjectorConstructor(i);
                // TODO 泛型
                var matchResult = constructorSymbol.Identifier.MatchArguments(argumentTypes,null);
                if (matchResult is ParameterList.ArgumentMatchResult.CompletelyEqual)
                {
                    return true;
                }
                else if (matchResult is ParameterList.ArgumentMatchResult.CanCast)
                {
                    selectedConstructors.Add(constructorSymbol);
                }
            }


            if (selectedConstructors.Count == 1)
            {
                constructorSymbol = selectedConstructors[0];
                return true;
            }

            if (selectedConstructors.Count == 0)
            {
                constructorSymbol = null;
                return false;
            }

            throw new GorgeCompileException("有多个候选");
        }

        public override HashSet<ModifierType> AllowedMethodModifierTypes { get; } = new()
        {
            ModifierType.Static
        };

        public AnnotationScope DeclareAnnotation(string annotationIdentifier, SymbolicGorgeType genericType,
            CodeLocation definitionToken, CodeLocation definitionRange)
        {
            var annotation =
                new AnnotationScope(this, annotationIdentifier, genericType, definitionToken, definitionRange);
            Annotations.Add(annotation);
            return annotation;
        }


        #region 内部委托管理

        private readonly List<GorgeDelegateImplementation> _delegateImplementations = new();

        public int NextDelegateIndex => _delegateImplementations.Count;

        public void RegisterDelegate(GorgeDelegateImplementation delegateImplementation)
        {
            _delegateImplementations.Add(delegateImplementation);
        }

        public GorgeDelegateImplementation[] Delegates => _delegateImplementations.ToArray();

        #endregion
    }
}