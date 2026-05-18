using System.Collections.Generic;
using System.Linq;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Expression.Tools;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.UnaryLeftAssociativityLevel
{
    /// <summary>
    /// 构造方法调用，从类名构造的情况
    /// 如果所有参数和Injector均为编译时常量，则本表达式为编译时常量，值为对应的ClassCreator
    /// </summary>
    public class ConstructorInvocationExpression : DynamicValueExpression
    {
        private readonly SymbolicGorgeType _objectType;
        private readonly IGorgeValueExpression _injector;
        private readonly IGorgeValueExpression[] _argumentExpressions;
        private readonly ConstructorSymbol _constructor;
        private readonly Dictionary<SymbolicGorgeType, SymbolicGorgeType> _baseObjectGenericsTypes;

        public ConstructorInvocationExpression(ClassType objectType, IGorgeValueExpression injector,
            IGorgeValueExpression[] argumentExpressions, CodeBlockScope block, CodeLocation expressionLocation) : base(
            block, expressionLocation)
        {
            _objectType = objectType;
            _injector = injector;
            _argumentExpressions = argumentExpressions;

            var argumentTypes = argumentExpressions.Select(e => e.ValueType).ToArray();
            var classSymbol = objectType.Symbol;

            _constructor = classSymbol.ClassScope.ConstructorGroupScope.GetConstructorByArgumentTypes(argumentTypes,
                null, expressionLocation);

            if (objectType.GenericsInstanceTypes == null)
            {
                _baseObjectGenericsTypes = new Dictionary<SymbolicGorgeType, SymbolicGorgeType>();
            }
            else
            {
                _baseObjectGenericsTypes = objectType.Symbol.ClassScope.GenericsSymbols
                    .Zip(objectType.GenericsInstanceTypes, (g, i) => new {g, i})
                    .ToDictionary(p => p.g.Type, p => p.i);
            }

            ValueType = objectType;
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

            var newObjectAddress = Block.AddTempVariable(_objectType);
            existCodes.Add(IntermediateCode.InvokeConstructor(_constructor.Id));
            existCodes.Add(IntermediateCode.GetReturn(newObjectAddress, _objectType));

            return newObjectAddress;
        }
    }
}