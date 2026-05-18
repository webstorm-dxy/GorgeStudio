using System.Collections.Generic;
using System.Linq;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeCompiler.Expression.Tools;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.PrimaryLevel
{
    public class NamespaceReferenceExpression : ReferenceExpression<NamespaceSymbol>
    {
        public NamespaceReferenceExpression(NamespaceSymbol symbol, CodeLocation expressionLocation) : base(symbol,
            expressionLocation)
        {
        }
    }

    public class LocalReferenceExpression : ReferenceExpression<LocalSymbol>, IGorgeValueExpression
    {
        public LocalReferenceExpression(LocalSymbol symbol, CodeLocation expressionLocation) : base(symbol,
            expressionLocation)
        {
            ValueType = symbol.Address.Type;
        }

        public SymbolicGorgeType ValueType { get; }
        public bool IsCompileConstant => false;
        public object CompileConstantValue => null;

        public SymbolicAddress AppendCodes(CodeBlockScope codeBlockScope, List<IntermediateCode> existCodes)
        {
            return Symbol.Address;
        }
    }

    public class EnumValueReferenceExpression : ReferenceExpression<EnumValueSymbol>, IGorgeValueExpression
    {
        public EnumValueReferenceExpression(EnumValueSymbol symbol, CodeLocation expressionLocation) : base(symbol,
            expressionLocation)
        {
            ValueType = symbol.EnumScope.EnumSymbol.Type;
            CompileConstantValue = symbol.IntValue;
        }

        public SymbolicGorgeType ValueType { get; }
        public bool IsCompileConstant => true;
        public object CompileConstantValue { get; }

        public SymbolicAddress AppendCodes(CodeBlockScope codeBlockScope, List<IntermediateCode> existCodes)
        {
            var resultAddress = codeBlockScope.AddTempVariable(ValueType);
            existCodes.Add(IntermediateCode.LocalIntAssign(resultAddress, (int) CompileConstantValue));
            return resultAddress;
        }
    }

    /// <summary>
    /// 字段引用表达式
    /// </summary>
    public class FieldReferenceExpression : ReferenceExpression<IFieldSymbol>, IGorgeValueExpression
    {
        public IGorgeValueExpression Receiver { get; }
        public SymbolicGorgeType ValueType { get; }
        public bool IsCompileConstant => false;
        public object CompileConstantValue => null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="receiver">为null则认为是this</param>
        /// <param name="symbol"></param>
        /// <param name="expressionLocation"></param>
        public FieldReferenceExpression(IGorgeValueExpression receiver, IFieldSymbol symbol,
            CodeLocation expressionLocation) : base(symbol, expressionLocation)
        {
            Receiver = receiver;
            ValueType = symbol.Type;
        }

        public SymbolicAddress AppendCodes(CodeBlockScope codeBlockScope, List<IntermediateCode> existCodes)
        {
            Address receiverAddress;
            if (Receiver != null)
            {
                receiverAddress = Receiver.AppendCodes(codeBlockScope, existCodes);
            }
            else
            {
                var thisAddress = codeBlockScope.AddTempVariable(Symbol.DeclaringType);
                existCodes.Add(IntermediateCode.LoadThis(thisAddress));
                receiverAddress = thisAddress;
            }

            var fieldAddress = codeBlockScope.AddTempVariable(ValueType);
            existCodes.Add(IntermediateCode.LoadField(ValueType, fieldAddress, receiverAddress, Symbol.Index));
            return fieldAddress;
        }
    }


    /// <summary>
    /// 方法组访问
    /// </summary>
    public class MethodGroupReferenceExpression : ReferenceExpression<MethodGroupSymbol>
    {
        /// <summary>
        /// 调用接受者表达式
        /// </summary>
        public IGorgeValueExpression? Receiver { get; }

        /// <summary>
        /// 调用接收者的类型
        /// </summary>
        public SymbolicGorgeType ReceiverType { get; }

        /// <summary>
        /// 是否为仅静态方法调用
        /// </summary>
        public bool IsStaticMethod { get; }

        public CodeLocation MethodNameLocation { get; }

        public MethodGroupReferenceExpression(IGorgeValueExpression? receiver, MethodGroupSymbol symbol,
            bool isStaticMethod, CodeLocation expressionLocation, CodeLocation methodNameLocation) : base(symbol,
            expressionLocation)
        {
            // TODO 目前静态方法调用和this的直接调用receiver都是null，但这两种情况都可能需要传递泛型信息
            Receiver = receiver;
            // TODO 这里的逻辑实际上不对，应该向上取到方法所在那一层，并且注入泛型
            if (receiver != null)
            {
                ReceiverType = receiver.ValueType;
            }
            else
            {
                ReceiverType = symbol.MethodGroupScope.ParentScope.Type;
            }

            IsStaticMethod = isStaticMethod;
            MethodNameLocation = methodNameLocation;
        }
    }

    /// <summary>
    /// 方法组调用
    /// </summary>
    public class MethodInvocationExpression : ReferenceExpression<MethodSymbol>, IGorgeValueExpression
    {
        private IGorgeValueExpression[] _parameterExpressions;

        private MethodGroupReferenceExpression _methodInvocationExpression;

        private Dictionary<SymbolicGorgeType, SymbolicGorgeType> _genericsInstances;

        public MethodInvocationExpression(MethodGroupReferenceExpression methodGroupReferenceExpression,
            IGorgeValueExpression[] parameterExpressions, CodeLocation expressionLocation) : base(
            methodGroupReferenceExpression.Symbol.MethodGroupScope.GetMethodByArgumentTypes(parameterExpressions
                    .Select(s => s.ValueType).ToArray(), methodGroupReferenceExpression.Receiver?.ValueType
                    is ClassType classType
                    ? classType.GenericsInstanceTypes
                    : null, methodGroupReferenceExpression.MethodNameLocation,
                expressionLocation, methodGroupReferenceExpression.IsStaticMethod), expressionLocation)
        {
            _methodInvocationExpression = methodGroupReferenceExpression;
            _parameterExpressions = parameterExpressions;

            // TODO 暂时不考虑缺少调用接收者情况下的泛型
            _genericsInstances = methodGroupReferenceExpression.Receiver?.ValueType is ClassType classType1
                ? classType1.GenericsInstanceTypesMap
                : new Dictionary<SymbolicGorgeType, SymbolicGorgeType>();

            if (Symbol.ReturnType is GenericsType)
            {
                if (_genericsInstances.TryGetValue(Symbol.ReturnType, out var instanceType))
                {
                    ValueType = instanceType;
                }
                else
                {
                    throw new GorgeCompilerException($"缺少泛型{Symbol.ReturnType.ToGorgeType()}的实参");
                }
                //
                // ValueType = methodGroupReferenceExpression.ReceiverType
                //     .Assert<ClassType>(methodGroupReferenceExpression.ExpressionLocation).GenericsInstanceTypes
                //     .First();
            }
            else
            {
                ValueType = Symbol.ReturnType;
            }
        }

        public SymbolicGorgeType ValueType { get; }
        public bool IsCompileConstant => false;
        public object CompileConstantValue => null;

        public SymbolicAddress AppendCodes(CodeBlockScope codeBlockScope, List<IntermediateCode> existCodes)
        {
            Address receiverAddress;
            if (_methodInvocationExpression.Receiver != null)
            {
                receiverAddress = _methodInvocationExpression.Receiver.AppendCodes(codeBlockScope, existCodes);
                CommonImmediateCodes.SetInvocationArguments(codeBlockScope,
                    Symbol.MethodScope.ParameterSymbols.ToArray(),
                    _genericsInstances, _parameterExpressions, existCodes);
                switch (_methodInvocationExpression.Receiver.ValueType)
                {
                    case ClassType classType:
                        existCodes.Add(IntermediateCode.InvokeMethod(receiverAddress, Symbol.Id));
                        break;
                    case InterfaceType interfaceType:
                        existCodes.Add(IntermediateCode.InvokeInterfaceMethod(receiverAddress,
                            interfaceType.Symbol.FullName, Symbol.Id));
                        break;
                    default:
                        throw new GorgeCompilerException("非预期调用接收者");
                }
            }
            // 接收者为null的情况视为从this调用
            else
            {
                CommonImmediateCodes.SetInvocationArguments(codeBlockScope,
                    Symbol.MethodScope.ParameterSymbols.ToArray(),
                    new Dictionary<SymbolicGorgeType, SymbolicGorgeType>(),
                    _parameterExpressions, existCodes);
                if (_methodInvocationExpression.IsStaticMethod)
                {
                    existCodes.Add(IntermediateCode.InvokeStaticMethod(
                        Symbol.DeclaringType.Assert<ClassType>(null).Symbol.FullName, Symbol.Id));
                }
                else
                {
                    var thisAddress = codeBlockScope.AddTempVariable(Symbol.DeclaringType);
                    existCodes.Add(IntermediateCode.LoadThis(thisAddress));
                    receiverAddress = thisAddress;
                    existCodes.Add(IntermediateCode.InvokeMethod(receiverAddress, Symbol.Id));
                }
            }

            if (ValueType is VoidType)
            {
                return default;
            }

            var returnAddress = codeBlockScope.AddTempVariable(ValueType);
            existCodes.Add(IntermediateCode.GetReturn(returnAddress, ValueType));
            return returnAddress;
        }
    }
}