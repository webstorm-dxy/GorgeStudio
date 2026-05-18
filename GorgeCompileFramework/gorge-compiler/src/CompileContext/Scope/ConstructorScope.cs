using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Block;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.CompileContext.Scope
{
    public class ConstructorScope : CodeBlockScope, IAnnotationContainer
    {
        public override HashSet<SymbolType> AllowedSymbolTypes { get; } = new HashSet<SymbolType>()
        {
            SymbolType.Parameter
        };

        /// <summary>
        /// 本域的构造方法符号
        /// </summary>
        public readonly ConstructorSymbol ConstructorSymbol;

        /// <summary>
        /// 本域所属的类域
        /// </summary>
        public readonly ConstructorGroupScope ParentConstructorGroupScope;

        /// <summary>
        /// 本构造方法的注解
        /// </summary>
        public readonly List<AnnotationScope> Annotations = new();

        public ConstructorInformation ConstructorInformation { get; private set; }

        public CompiledConstructorImplementation Implementation { get; private set; }

        public TypeCount ParameterCount { get; }

        public Dictionary<string, ParameterSymbol> Parameters { get; } = new();
        
        public List<ParameterSymbol> ParameterSymbols { get; } = new();

        /// <summary>
        /// 构造用注入器的加载地址。
        /// Object0
        /// </summary>
        public Address InjectorAddress { get; }

        public ConstructorScope(ConstructorGroupScope parentScope, ConstructorSymbol constructorSymbol) : base(
            BlockContextType.Instance, parentScope.ParentScope.ClassSymbol,
            parentScope.ParentScope.ClassSymbol.Type, parentScope)
        {
            ParentConstructorGroupScope = parentScope;
            ConstructorSymbol = constructorSymbol;
            ParameterCount = new TypeCount();

            #region 预分配保留地址

            InjectorAddress =
                AddTempVariable(SymbolicGorgeType.Injector(parentScope.ParentScope.ClassSymbol.Type));

            #endregion
        }

        public AnnotationScope DeclareAnnotation(string annotationIdentifier, SymbolicGorgeType genericType,
            CodeLocation definitionToken, CodeLocation definitionRange)
        {
            var annotation =
                new AnnotationScope(this, annotationIdentifier, genericType, definitionToken, definitionRange);
            Annotations.Add(annotation);
            return annotation;
        }

        public void FreezeDeclaration()
        {
            ConstructorInformation = ToConstructorInformation();
        }
        
        private ConstructorInformation ToConstructorInformation()
        {
            var annotations = Annotations.Select(a => a.ToAnnotation()).ToArray();
            return new ConstructorInformation(ConstructorSymbol.Id, ConstructorSymbol.Identifier.ParameterInformation,
                annotations);
        }

        public void Implement(CompiledConstructorImplementation constructorImplementation)
        {
            Implementation = constructorImplementation;
        }

        public CodeBlockScope GetCodeScope()
        {
            return new CodeBlockScope(BlockContextType.Instance, ParentConstructorGroupScope.ParentScope.ClassSymbol,
                ParentConstructorGroupScope.ParentScope.ClassSymbol.Type, this);
        }
        


        /// <summary>
        /// 添加参数，并且分配卸载的本地变量地址
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="definitionToken"></param>
        /// <param name="definitionRange"></param>
        /// <returns></returns>
        public Address AddParameter(string name, SymbolicGorgeType type, IToken definitionToken,
            CodeRange definitionRange)
        {
            var index = ParameterCount.Count(type.BasicType);
            var address = AddTempVariable(type);
            var parameterSymbol =
                new ParameterSymbol(this, type, name, index, address, definitionToken.CodeLocation(), definitionRange);
            AddSymbol(parameterSymbol);
            Parameters.Add(name, parameterSymbol);
            ParameterSymbols.Add(parameterSymbol);
            return address;
        }
    }
}