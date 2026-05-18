using System.Collections.Generic;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeLanguage.Objective;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    /// <summary>
    /// 类的符号定义
    /// </summary>
    public class ClassSymbol : TypeSymbol
    {
        /// <summary>
        /// 本类对应的符号域
        /// </summary>
        public readonly ClassScope ClassScope;

        public readonly NamespaceScope NamespaceScope;

        public ClassSymbol(NamespaceScope scope, string identifier, CodeLocation definitionToken,
            GorgeParser.ClassDeclarationContext parseTree, GorgeParser.ExpressionContext[] usingsParserTree) : base(
            scope, identifier, definitionToken, parseTree)
        {
            NamespaceScope = scope;
            ClassScope = new ClassScope(scope, this, parseTree, usingsParserTree);
            Type = SymbolicGorgeType.Object(this);
        }

        public override SymbolType SymbolType => SymbolType.Class;

        public override SymbolicGorgeType Type { get; }

        public string FullName => Type.ToGorgeType().FullName;

        /// <summary>
        /// 本类是否为native类
        /// </summary>
        public bool IsNative => Modifiers.ContainsKey(ModifierType.Native);

        /// <summary>
        /// 获取泛型参数实例化的Gorge类型
        /// </summary>
        /// <param name="genericsInstances">泛型参数实例类型</param>
        /// <param name="positions">泛型参数所在位置</param>
        /// <returns></returns>
        public SymbolicGorgeType GenericsInstanceGorgeType(SymbolicGorgeType[] genericsInstances,
            params CodeLocation[] positions)
        {
            // TODO 目前只验证了泛型参数数量
            if (genericsInstances.Length != ClassScope.GenericsSymbols.Count)
            {
                throw new UnexpectedParameterCountException(ClassScope.GenericsSymbols.Count, genericsInstances.Length,
                    positions);
            }

            return new ClassType(this, genericsInstances);
        }

        public CompiledGorgeClass ToGorgeClass()
        {
            if (IsNative)
            {
                throw new GorgeCompilerException("Native类无法编译成实现");
            }

            var methods = new List<CompiledMethodImplementation>();
            var staticMethods = new List<CompiledMethodImplementation>();
            foreach (var (_, group) in ClassScope.MethodGroups)
            {
                foreach (var (methodSymbol, methodScope) in group.Methods)
                {
                    if (methodSymbol.IsStatic)
                    {
                        staticMethods.Add(methodScope.Implementation);
                    }
                    else
                    {
                        methods.Add(methodScope.Implementation);
                    }
                }
            }

            var constructors = new List<CompiledConstructorImplementation>();
            foreach (var (_, constructorScope) in ClassScope.ConstructorGroupScope.Constructors)
            {
                constructors.Add(constructorScope.Implementation);
            }

            var fieldInitializers = new List<CompiledFieldInitializerImplementation>();
            foreach (var (_, fieldScope) in ClassScope.Fields)
            {
                var initializeImplementation = fieldScope.InitializerImplementation;
                if (initializeImplementation != null)
                {
                    fieldInitializers.Add(initializeImplementation);
                }
            }

            return new CompiledGorgeClass(ClassScope.Declaration, methods, staticMethods, constructors,
                fieldInitializers, ClassScope.Delegates);
        }

        public override string ToString()
        {
            return Identifier;
        }
    }
}