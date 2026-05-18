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
    public class MethodScope : CodeBlockScope, IAnnotationContainer
    {
        public override HashSet<SymbolType> AllowedSymbolTypes { get; } = new HashSet<SymbolType>()
        {
            SymbolType.Parameter
        };

        /// <summary>
        /// 本域的字段符号
        /// </summary>
        public readonly MethodSymbol MethodSymbol;

        /// <summary>
        /// 本域所属的类域
        /// </summary>
        public readonly MethodGroupScope ParentMethodGroupScope;

        /// <summary>
        /// 本方法的注解
        /// </summary>
        public readonly List<AnnotationScope> Annotations = new();

        public MethodInformation MethodInformation { get; private set; }

        public CompiledMethodImplementation Implementation { get; private set; }

        public TypeCount ParameterCount { get; }

        public Dictionary<string, ParameterSymbol> Parameters { get; } = new();

        public List<ParameterSymbol> ParameterSymbols { get; } = new();

        public override BlockContextType ContextType =>
            MethodSymbol.IsStatic ? BlockContextType.StaticMethod : BlockContextType.Instance;

        public MethodScope(MethodGroupScope parentScope, MethodSymbol methodSymbol) : base(
            BlockContextType.Instance, parentScope.ParentScope is ClassScope c ? c.ClassSymbol : null,
            methodSymbol.ReturnType, parentScope)
        {
            ParentMethodGroupScope = parentScope;
            MethodSymbol = methodSymbol;
            ParameterCount = new TypeCount();
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
            var annotations = Annotations.Select(a => a.ToAnnotation()).ToArray();
            MethodInformation = new MethodInformation(MethodSymbol.Id, MethodSymbol.MethodName, MethodSymbol.ReturnType,
                MethodSymbol.Identifier.ParameterInformation, annotations);
        }

        public void Implement(CompiledMethodImplementation methodImplementation)
        {
            Implementation = methodImplementation;
        }

        public CodeBlockScope GetCodeScope()
        {
            return new CodeBlockScope(ContextType, ClassSymbol, ReturnType, this);
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