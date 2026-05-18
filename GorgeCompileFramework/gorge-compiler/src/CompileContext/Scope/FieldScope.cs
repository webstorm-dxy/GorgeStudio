using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Block;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeCompiler.CompileContext.Scope
{
    /// <summary>
    /// 字段符号域
    /// </summary>
    public class FieldScope : StringSymbolScope, IAnnotationContainer
    {
        public override HashSet<SymbolType> AllowedSymbolTypes => new();

        /// <summary>
        /// 本域的字段符号
        /// </summary>
        public readonly FieldSymbol FieldSymbol;

        /// <summary>
        /// 本域所属的类域
        /// </summary>
        public readonly ClassScope ParentClassScope;

        /// <summary>
        /// 本字段的注解
        /// </summary>
        public readonly List<AnnotationScope> Annotations = new();

        public FieldInformation FieldInformation { get; private set; }

        public CompiledFieldInitializerImplementation InitializerImplementation { get; private set; }

        public FieldScope(ClassScope parentScope, FieldSymbol fieldSymbol) : base(parentScope)
        {
            ParentClassScope = parentScope;
            FieldSymbol = fieldSymbol;
        }

        public AnnotationScope DeclareAnnotation(string annotationIdentifier, SymbolicGorgeType genericType,
            CodeLocation definitionToken, CodeLocation definitionRange)
        {
            var annotation =
                new AnnotationScope(this, annotationIdentifier, genericType, definitionToken, definitionRange);
            Annotations.Add(annotation);

            return annotation;
        }

        /// <summary>
        /// 转换为Gorge的字段声明信息
        /// </summary>
        /// <returns></returns>
        public void FreezeDeclaration()
        {
            var annotations = Annotations.Select(a => a.ToAnnotation()).ToArray();
            FieldInformation = new FieldInformation(FieldSymbol.Id, FieldSymbol.Identifier, FieldSymbol.Type,
                FieldSymbol.Index, annotations);
        }

        public void ImplementInitializer(CompiledFieldInitializerImplementation fieldInitializerImplementation)
        {
            InitializerImplementation = fieldInitializerImplementation;
        }

        /// <summary>
        /// 创建初始化语句块
        /// </summary>
        /// <returns></returns>
        public CodeBlockScope GetInitializerCodeScope()
        {
            return new CodeBlockScope(BlockContextType.Instance, ParentClassScope.ClassSymbol, null, this);
        }
    }

    public interface IAnnotationContainer
    {
        /// <summary>
        /// 声明一个注解
        /// </summary>
        /// <param name="annotationIdentifier"></param>
        /// <param name="genericType"></param>
        /// <param name="definitionToken"></param>
        /// <param name="definitionRange"></param>
        public AnnotationScope DeclareAnnotation(string annotationIdentifier, SymbolicGorgeType genericType,
            CodeLocation definitionToken, CodeLocation definitionRange);
    }
}