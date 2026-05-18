using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    /// <summary>
    /// 代理字段符号
    /// 用于存储代理内部使用的变量或字段在代理对象构造时使用的值
    /// </summary>
    public class DelegateFieldSymbol : Symbol<string>, IFieldSymbol
    {
        public Symbol<string> BaseSymbol { get; }

        public override SymbolType SymbolType => SymbolType.Field;
        public override HashSet<ModifierType> AllowedModifierTypes { get; } = new();

        public int Index { get; }
        public SymbolicGorgeType Type { get; }
        public SymbolicGorgeType DeclaringType { get; }

        public DelegateFieldSymbol(DelegateScope scope, Symbol<string> baseSymbol, int index) : base(scope,
            baseSymbol.Identifier, baseSymbol.DefinitionToken, baseSymbol.DefinitionRange)
        {
            BaseSymbol = baseSymbol;
            switch (baseSymbol)
            {
                case FieldSymbol fieldSymbol:
                    Type = fieldSymbol.Type;
                    break;
                case ParameterSymbol parameterSymbol:
                    Type = parameterSymbol.Type;
                    break;
                case VariableSymbol variableSymbol:
                    Type = variableSymbol.Address.Type;
                    break;
                case DelegateFieldSymbol fieldSymbol:
                    Type = fieldSymbol.Type;
                    break;
                default:
                    throw new GorgeCompilerException("代理域内不能引用目标类型外部符号");
            }

            DeclaringType = scope.DelegateType;

            Index = index;
        }

        public override void CheckModifierConflict(ModifierType modifierType, IToken modifierToken)
        {
            throw new UnreachableException();
        }
    }
}