using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeLanguage.Objective;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.CompileContext.Scope
{
    public class EnumScope : StringSymbolScope
    {
        public override HashSet<SymbolType> AllowedSymbolTypes { get; } = new()
        {
            SymbolType.EnumValue
        };

        /// <summary>
        /// 本域的枚举符号
        /// </summary>
        public readonly EnumSymbol EnumSymbol;

        /// <summary>
        /// Using表达式
        /// </summary>
        public readonly GorgeParser.ExpressionContext[] UsingsParserTree;
        
        /// <summary>
        /// 本枚举的语法树
        /// </summary>
        public readonly GorgeParser.EnumDeclarationContext ParserTree;

        /// <summary>
        /// 类的声明信息
        /// </summary>
        public CompiledEnum Enum { get; set; }

        /// <summary>
        /// 枚举内枚举值
        /// </summary>
        public readonly List<EnumValueSymbol> EnumValues = new();

        /// <summary>
        /// 类所在的源文件地址
        /// </summary>
        public readonly string SourceFilePath;

        public EnumScope(NamespaceScope parentNamespace, EnumSymbol enumSymbol,
            GorgeParser.EnumDeclarationContext parserTree, GorgeParser.ExpressionContext[] usingsParserTree) : base(null)
        {
            EnumSymbol = enumSymbol;
            ParserTree = parserTree;
            UsingsParserTree = usingsParserTree;
            SourceFilePath = parserTree.Start.TokenSource.SourceName;
            // 由于自身为根，需要补充在namespace中添加下属
            parentNamespace.SubScopes.Add(this);
        }

        public EnumValueScope DeclareEnumValue(string identifier, IToken definitionToken, CodeRange definitionRange)
        {
            var enumValueSymbol =
                new EnumValueSymbol(this, identifier, EnumValues.Count, definitionToken.CodeLocation(), definitionRange);
            AddSymbol(enumValueSymbol);
            EnumValues.Add(enumValueSymbol);
            return enumValueSymbol.EnumValueScope;
        }
    }

    public class EnumValueScope : StringSymbolScope, IAnnotationContainer
    {
        public override HashSet<SymbolType> AllowedSymbolTypes { get; } = new();

        /// <summary>
        /// 本注解值的注解
        /// </summary>
        public readonly List<AnnotationScope> Annotations = new();

        public readonly EnumValueSymbol EnumValueSymbol;

        public EnumValueScope(EnumScope parentEnum, EnumValueSymbol enumValueSymbol) : base(parentEnum)
        {
            EnumValueSymbol = enumValueSymbol;
        }

        public AnnotationScope DeclareAnnotation(string annotationIdentifier, SymbolicGorgeType genericType,
            CodeLocation definitionToken, CodeLocation definitionRange)
        {
            var annotation =
                new AnnotationScope(this, annotationIdentifier, genericType, definitionToken, definitionRange);
            Annotations.Add(annotation);
            return annotation;
        }
    }
}