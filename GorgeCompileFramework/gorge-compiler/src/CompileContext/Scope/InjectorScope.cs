using System.Collections.Generic;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Exceptions.CompileException;

namespace Gorge.GorgeCompiler.CompileContext.Scope
{
    /// <summary>
    /// 注入器符号域，用于存储类的注入器的符号信息。
    /// 包含注入器字段
    /// </summary>
    public class InjectorScope : MemberContainerScope
    {
        /// <summary>
        /// 类内字段
        /// </summary>
        public readonly Dictionary<InjectorFieldSymbol, InjectorFieldScope> InjectorFields = new();

        public override HashSet<SymbolType> AllowedSymbolTypes { get; } = new()
        {
            SymbolType.Field
        };

        public readonly ClassScope ParentClassScope;

        public InjectorScope(ClassScope parent) : base(parent)
        {
            ParentClassScope = parent;
        }

        /// <summary>
        /// 在注入器中声明注入器字段
        /// </summary>
        /// <param name="fieldType">字段的类型</param>
        /// <param name="identifier">字段的标识符</param>
        /// <param name="hasDefaultValue">是否有默认值</param>
        /// <param name="definitionToken">定义该字段的词法Token</param>
        /// <param name="definitionRange">注入器字段的定义位置</param>
        /// <returns>字段符号域</returns>
        public InjectorFieldScope DeclareInjectorField(SymbolicGorgeType fieldType, string identifier,
            bool hasDefaultValue, CodeLocation definitionToken, CodeLocation definitionRange)
        {
            EnsureDeclarationNotFreeze();
            ParentClassScope.MemberCounter.CountInjectorField(fieldType, out var id, out var index);
            int? defaultValueIndex = null;
            if (hasDefaultValue)
            {
                ParentClassScope.MemberCounter.CountInjectorFieldDefaultValue(fieldType, out var dValueIndex);
                defaultValueIndex = dValueIndex;
            }

            var injectorFieldSymbol = new InjectorFieldSymbol(this, fieldType, identifier, id, index, defaultValueIndex,
                definitionToken, definitionRange.CodeRange);
            AddSymbol(injectorFieldSymbol);
            var injectorFieldScope = injectorFieldSymbol.InjectorFieldScope;
            InjectorFields.Add(injectorFieldSymbol, injectorFieldScope);
            return injectorFieldScope;
        }

        public InjectorFieldSymbol GetInjectorFieldByName(string fieldName, CodeLocation fieldCodeLocation)
        {
            if (TryGetSymbol(fieldName, out var symbol, null, false, false))
            {
                if (symbol is InjectorFieldSymbol injectorFieldSymbol)
                {
                    injectorFieldSymbol.AddReferenceToken(fieldCodeLocation);
                    return injectorFieldSymbol;
                }

                throw new GorgeCompileException($"{fieldName}不是注入器字段", fieldCodeLocation);
            }

            if (ParentClassScope.SuperClass != null)
            {
                return ParentClassScope.SuperClass.ClassScope.InjectorScope.GetInjectorFieldByName(fieldName,
                    fieldCodeLocation);
            }

            throw new GorgeCompileException($"注入器字段{fieldName}不存在", fieldCodeLocation);
        }
    }
}