using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeCompiler.CompileContext.Symbol
{
    public class MethodSymbol : Symbol<ParameterList>
    {
        /// <summary>
        /// 方法符号域
        /// </summary>
        public readonly MethodScope MethodScope;

        /// <summary>
        /// 方法名
        /// </summary>
        public readonly string MethodName;

        /// <summary>
        /// 方法返回值
        /// </summary>
        public readonly SymbolicGorgeType? ReturnType;

        public readonly int Id;
        
        public SymbolicGorgeType DeclaringType { get; }

        public MethodSymbol(MethodGroupScope scope, string methodName, SymbolicGorgeType? returnType,
            ParameterList parameterList, int id, CodeLocation definitionToken, CodeRange definitionRange,
            HashSet<ModifierType> allowedModifierTypes) : base(scope, parameterList, definitionToken, definitionRange)
        {
            Id = id;
            MethodName = methodName;
            ReturnType = returnType;
            MethodScope = new MethodScope(scope, this);
            AllowedModifierTypes = allowedModifierTypes;
            DeclaringType = scope.ParentScope.Type;
        }

        public override SymbolType SymbolType => SymbolType.Method;
        
        public bool IsStatic => Modifiers.ContainsKey(ModifierType.Static);

        public override HashSet<ModifierType> AllowedModifierTypes { get; }

        public override void CheckModifierConflict(ModifierType modifierType, IToken modifierToken)
        {
            if (Modifiers.TryGetValue(modifierType, out var existModifier))
            {
                throw new DuplicateModifierException(existModifier, modifierToken);
            }
        }
    }
}