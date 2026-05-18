using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Expression.Tools;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.UnaryLeftAssociativityLevel
{
    /// <summary>
    /// 构造方法调用，直接从Injector构造的情况
    /// 如果所有参数和Injector均为编译时常量，则本表达式为编译时常量，值为对应的ClassCreator
    /// </summary>
    public class InjectorConstructorInvocationExpression : DynamicValueExpression
    {
        private readonly IGorgeValueExpression _injector;
        private readonly IGorgeValueExpression[] _argumentExpressions;
        private readonly ConstructorSymbol _constructor;
        private readonly Dictionary<SymbolicGorgeType, SymbolicGorgeType> _baseObjectGenericsTypes;

        /// <summary>
        /// 判断调用依据是否为注入器构造方法
        /// </summary>
        private readonly bool _isInjectorConstructor;

        public InjectorConstructorInvocationExpression(IGorgeValueExpression injector,
            IGorgeValueExpression[] argumentExpressions,
            CodeBlockScope block, ParserRuleContext expressionLocation) : base(block, expressionLocation)
        {
            var classSymbolType = injector.ValueType.Assert<InjectorType>(injector.ExpressionLocation).BaseType
                .Assert<ClassType>(injector.ExpressionLocation);

            _injector = injector;
            _argumentExpressions = argumentExpressions;

            var argumentTypes = argumentExpressions.Select(e => e.ValueType).ToArray();
            var classSymbol = classSymbolType.Symbol;
            // var classDeclaration = classSymbol.ClassScope.Declaration;

            // _baseObjectGenericsTypes = classDeclaration.GenericsArguments(objectType);

            if (classSymbolType.GenericsInstanceTypes == null)
            {
                _baseObjectGenericsTypes = new Dictionary<SymbolicGorgeType, SymbolicGorgeType>();
            }
            else
            {
                _baseObjectGenericsTypes = classSymbolType.Symbol.ClassScope.GenericsSymbols
                    .Zip(classSymbolType.GenericsInstanceTypes, (g, i) => new {g, i})
                    .ToDictionary(p => p.g.Type, p => p.i);
            }


            _isInjectorConstructor =
                classSymbol.ClassScope.TryGetInjectorConstructorByArgumentTypes(argumentTypes,
                    out var constructorSymbol);
            if (!_isInjectorConstructor)
            {
                constructorSymbol =
                    classSymbol.ClassScope.ConstructorGroupScope.GetConstructorByArgumentTypes(argumentTypes, null,
                        _injector.ExpressionLocation);
            }

            _constructor = constructorSymbol;

            ValueType = classSymbolType;
        }

        public override SymbolicGorgeType ValueType { get; }

        public override SymbolicAddress AppendCodes(CodeBlockScope codeBlockScope, List<IntermediateCode> existCodes)
        {
            // 设置参数
            CommonImmediateCodes.SetInvocationArguments(Block, _constructor.ConstructorScope.ParameterSymbols.ToArray(),
                _baseObjectGenericsTypes,
                _argumentExpressions, existCodes);

            var injectorAddress = _injector.AppendCodes(Block, existCodes);
            existCodes.Add(IntermediateCode.SetInjector((Address) injectorAddress));

            var newObjectAddress = Block.AddTempVariable(ValueType);
            existCodes.Add(_isInjectorConstructor
                ? IntermediateCode.InvokeInjectorConstructor(_constructor.Id)
                : IntermediateCode.InvokeConstructor(_constructor.Id));
            existCodes.Add(IntermediateCode.GetReturn(newObjectAddress, ValueType));

            return newObjectAddress;
        }
    }
}