using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeCompiler.CompileContext.Scope
{
    public class AnnotationScope : StringSymbolScope, IMetadataScopeContainer
    {
        public override HashSet<SymbolType> AllowedSymbolTypes { get; } = new()
        {
            SymbolType.Field
        };

        /// <summary>
        /// 注解标识符
        /// </summary>
        public readonly string AnnotationIdentifier;

        /// <summary>
        /// 注解泛型实例
        /// </summary>
        public readonly SymbolicGorgeType GenericType;

        /// <summary>
        /// 定义位置
        /// </summary>
        public readonly CodeLocation DefinitionToken;

        /// <summary>
        /// 定义范围
        /// </summary>
        public readonly CodeLocation DefinitionRange;

        public AnnotationScope(SymbolScope<string> scope, string annotationIdentifier, SymbolicGorgeType genericType,
            CodeLocation definitionToken, CodeLocation definitionRange) : base(scope)
        {
            AnnotationIdentifier = annotationIdentifier;
            GenericType = genericType;
            DefinitionToken = definitionToken;
            DefinitionRange = definitionRange;
            MetadataScope = new MetadataScope(this);
        }

        public Annotation ToAnnotation()
        {
            var annotation = new Annotation(AnnotationIdentifier, GenericType);
            foreach (var (parameterName, annotationParameterSymbol) in Symbols)
            {
                if (annotationParameterSymbol is not AnnotationParameterSymbol parameterSymbol) continue;
                annotation.TryAddParameter(parameterSymbol.Identifier, parameterSymbol.Value);
            }

            foreach (var (metadataEntryName, metadataEntrySymbol) in MetadataScope.Symbols)
            {
                if (metadataEntrySymbol is not MetadataEntrySymbol entrySymbol) continue;
                annotation.TryAddMetadata(entrySymbol.Metadata);
            }

            return annotation;
        }

        /// <summary>
        /// 声明一个注解参数
        /// </summary>
        /// <param name="parameterName">参数名</param>
        /// <param name="parameterValue">参数值</param>
        /// <param name="definitionToken">参数定义词法单元</param>
        /// <param name="definitionRange">参数定义范围</param>
        public void DeclareAnnotationParameter(string parameterName, object parameterValue, IToken definitionToken,
            CodeRange definitionRange)
        {
            var parameterSymbol =
                new AnnotationParameterSymbol(this, parameterName, parameterValue, definitionToken.CodeLocation(), definitionRange);
            AddSymbol(parameterSymbol);
        }

        /// <summary>
        /// 查找注解参数
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="parameterSymbol"></param>
        /// <returns></returns>
        /// <exception cref="GorgeCompilerException"></exception>
        public bool TryGetAnnotationParameter(string parameterName, out AnnotationParameterSymbol parameterSymbol)
        {
            if (TryGetSymbol(parameterName, out var symbol, null, false))
            {
                if (symbol is AnnotationParameterSymbol pSymbol)
                {
                    parameterSymbol = pSymbol;
                    return true;
                }

                throw new GorgeCompilerException($"注解符号域内名为{parameterName}的符号不是注解字段类型", DefinitionToken);
            }

            parameterSymbol = null;
            return false;
        }

        public MetadataScope MetadataScope { get; }
    }

    public interface IMetadataScopeContainer
    {
        public MetadataScope MetadataScope { get; }
    }
}