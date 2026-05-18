using System.Collections.Generic;
using System.Linq;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeCompiler.Exceptions.CompilerException;
using Gorge.GorgeLanguage.Objective;
using GorgeCompiler.AntlrGen;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    /// <summary>
    /// 枚举的符号定义
    /// </summary>
    public class EnumSymbol : TypeSymbol
    {
        /// <summary>
        /// 本枚举对应的符号域
        /// </summary>
        public readonly EnumScope EnumScope;

        public readonly NamespaceScope NamespaceScope;

        public EnumSymbol(NamespaceScope scope, string identifier, CodeLocation definitionToken,
            GorgeParser.EnumDeclarationContext parserTree, GorgeParser.ExpressionContext[] usingsParserTree) : base(
            scope, identifier, definitionToken, parserTree)
        {
            NamespaceScope = scope;
            EnumScope = new EnumScope(scope, this, parserTree, usingsParserTree);
        }

        /// <summary>
        /// 本类是否为native类
        /// </summary>
        public bool IsNative => Modifiers.ContainsKey(ModifierType.Native);

        public override SymbolType SymbolType => SymbolType.Enum;
        public override SymbolicGorgeType Type => SymbolicGorgeType.Enum(this);

        public string FullName => Type.ToGorgeType().FullName;

        public CompiledEnum ToEnum()
        {
            var values = new List<string>();
            var displayNames = new List<string>();
            foreach (var enumValueSymbol in EnumScope.EnumValues)
            {
                values.Add(enumValueSymbol.Identifier);
                var displayNameAnnotation = enumValueSymbol.EnumValueScope.Annotations.FirstOrDefault(a =>
                    a.AnnotationIdentifier == "DisplayName");
                if (displayNameAnnotation == null)
                {
                    displayNames.Add(enumValueSymbol.Identifier);
                }
                else
                {
                    displayNameAnnotation.TryGetSymbol("name", out var nameParameter, null, false);
                    if (nameParameter == null)
                    {
                        throw new GorgeCompileException($"枚举值的DisplayName注解必须有name字段",
                            displayNameAnnotation.DefinitionToken);
                    }
                    else
                    {
                        if (nameParameter is not AnnotationParameterSymbol annotationParameterSymbol)
                        {
                            // TODO 修改为正确的Position
                            throw new UnexpectedSymbolTypeCompilerException(nameParameter.DefinitionToken,
                                nameParameter.SymbolType, SymbolType.Field);
                        }

                        if (annotationParameterSymbol.Value is not string stringValue)
                        {
                            // TODO 修改为正确的Position
                            throw new GorgeCompileException("枚举值的DisplayName注解的name字段必须为string类型",
                                nameParameter.DefinitionToken);
                        }

                        displayNames.Add(stringValue);
                    }
                }
            }

            return new CompiledEnum(Type, Modifiers.ContainsKey(ModifierType.Native), values.ToArray(),
                displayNames.ToArray());
        }
    }
}