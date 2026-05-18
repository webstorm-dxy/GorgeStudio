using System;
using System.Collections.Generic;
using System.Linq;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeLanguage.Objective
{
    public class GorgeDelegateDeclaration : InvokableMemberInformation
    {
        public GorgeDelegateDeclaration(GorgeType returnType, ParameterInformation[] parameterDeclarations) : base(
            parameterDeclarations)
        {
            DelegateType = GorgeType.Delegate(returnType, parameterDeclarations.Select(p => p.Type).ToArray());
        }

        public GorgeType DelegateType { get; }
    }

    public class GorgeDelegateImplementation : InvokableMemberInformation, IVirtualMachineExecutable
    {
        /// <summary>
        /// 内部lambda表达式实现
        /// </summary>
        public GorgeDelegateImplementation[] DelegateImplementations { get; }

        /// <summary>
        /// 外部传入值计数
        /// </summary>
        public TypeCount OuterValueCount { get; }

        public GorgeDelegateImplementation(ParameterInformation[] parameters, GorgeType returnType,
            TypeCount outerValueCount, TypeCount localVariableCount,
            List<IntermediateCode> code, GorgeType type,
            GorgeDelegateImplementation[] delegateImplementations) : base(parameters)
        {
            DelegateImplementations = delegateImplementations;
            OuterValueCount = outerValueCount;
            ReturnType = returnType;
            LocalVariableCount = localVariableCount;
            Code = code.ToArray();
            Type = type;
        }

        public GorgeType Type { get; }

        public GorgeType ReturnType { get; }
        public IntermediateCode[] Code { get; }
        public TypeCount LocalVariableCount { get; }
        private Dictionary<string, GorgeClass> _classCache = new();

        public GorgeClass GetClass(string className)
        {
            if (_classCache.TryGetValue(className, out var @class))
            {
                return @class;
            }

            @class = GorgeLanguageRuntime.Instance.GetClass(className);
            _classCache.Add(className, @class);
            return @class;
        }

        public string DebugName => "Delegate";
    }

    public abstract class GorgeDelegate : GorgeObject
    {
        public abstract GorgeType Type { get; }

        /// <summary>
        /// 直接调用
        /// 无参数布局过程
        /// </summary>
        public abstract void Invoke();

        /// <summary>
        /// 反射调用
        /// 含参数布局过程
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public abstract object Invoke(params object[] args);

        public override int GetIntField(string fieldName)
        {
            throw new Exception("无法对delegate访问字段");
        }

        public override float GetFloatField(string fieldName)
        {
            throw new Exception("无法对delegate访问字段");
        }

        public override bool GetBoolField(string fieldName)
        {
            throw new Exception("无法对delegate访问字段");
        }

        public override string GetStringField(string fieldName)
        {
            throw new Exception("无法对delegate访问字段");
        }

        public override GorgeObject GetObjectField(string fieldName)
        {
            throw new Exception("无法对delegate访问字段");
        }

        public override void Set(string fieldName, int value)
        {
            throw new Exception("无法对delegate访问字段");
        }

        public override void Set(string fieldName, float value)
        {
            throw new Exception("无法对delegate访问字段");
        }

        public override void Set(string fieldName, bool value)
        {
            throw new Exception("无法对delegate访问字段");
        }

        public override void Set(string fieldName, string value)
        {
            throw new Exception("无法对delegate访问字段");
        }

        public override void Set(string fieldName, GorgeObject value)
        {
            throw new Exception("无法对delegate访问字段");
        }

        public override object InvokeMethod(string methodName, GorgeType[] argumentTypes,
            Dictionary<GorgeType, GorgeType> genericsArguments, object[] argument)
        {
            throw new Exception("无法对delegate调用方法");
        }

        public override GorgeClass GorgeClass { get; } = null;
        public override GorgeObject RealObject => this;

        public override void InvokeMethod(int methodIndex)
        {
            throw new Exception("无法对delegate调用方法");
        }
    }

    public class NativeGorgeDelegate : GorgeDelegate
    {
        private readonly Func<object[], object> _invoke;

        public NativeGorgeDelegate(GorgeType type, Func<object[], object> invoke)
        {
            _invoke = invoke;
            Type = type;
        }

        public NativeGorgeDelegate(GorgeType type, Action<object[]> invoke)
        {
            _invoke = parameter =>
            {
                invoke(parameter);
                return null;
            };
            Type = type;
        }

        public override GorgeType Type { get; }

        public override void Invoke()
        {
            var parameterTypes = Type.SubTypes.Skip(1).ToArray();
            var returnType = Type.SubTypes[0];

            var parameters = new object[parameterTypes.Length];
            var parameterCount = new TypeCount();

            for (var i = 0; i < parameters.Length; i++)
            {
                switch (parameterTypes[i].BasicType)
                {
                    case BasicType.Int:
                    case BasicType.Enum:
                        parameters[i] = InvokeParameterPool.Int[parameterCount.Count(parameterTypes[i].BasicType)];
                        break;
                    case BasicType.Float:
                        parameters[i] = InvokeParameterPool.Float[parameterCount.Count(parameterTypes[i].BasicType)];
                        break;
                    case BasicType.Bool:
                        parameters[i] = InvokeParameterPool.Bool[parameterCount.Count(parameterTypes[i].BasicType)];
                        break;
                    case BasicType.String:
                        parameters[i] = InvokeParameterPool.String[parameterCount.Count(parameterTypes[i].BasicType)];
                        break;
                    case BasicType.Object:
                        parameters[i] = InvokeParameterPool.Object[parameterCount.Count(parameterTypes[i].BasicType)];
                        break;
                    default:
                        throw new Exception("不支持该类型");
                }
            }

            var result = _invoke(parameters);

            switch (returnType.BasicType)
            {
                case BasicType.Int:
                case BasicType.Enum:
                    InvokeParameterPool.IntReturn = (int) result;
                    break;
                case BasicType.Float:
                    InvokeParameterPool.FloatReturn = (float) result;
                    break;
                case BasicType.Bool:
                    InvokeParameterPool.BoolReturn = (bool) result;
                    break;
                case BasicType.String:
                    InvokeParameterPool.StringReturn = (string) result;
                    break;
                case BasicType.Object:
                    InvokeParameterPool.ObjectReturn = (GorgeObject) result;
                    break;
                default:
                    throw new Exception("不支持该类型");
            }
        }

        public override object Invoke(params object[] args)
        {
            return _invoke(args);
        }
    }

    public class CompiledGorgeDelegate : GorgeDelegate
    {
        /// <summary>
        /// 外部值参数池
        /// </summary>
        private readonly FixedFieldValuePool _outerValuePool;

        public GorgeDelegateImplementation Implementation { get; }

        public CompiledGorgeDelegate(GorgeDelegateImplementation implementation)
        {
            Implementation = implementation;
            _outerValuePool = new FixedFieldValuePool(implementation.OuterValueCount);
        }

        public override GorgeType Type => Implementation.Type;

        public override void Invoke()
        {
            GorgeLanguageRuntime.Instance.Vm.InvokeMethod(Implementation, this);
        }

        public override object Invoke(params object[] args)
        {
            var parameterTypes = Type.SubTypes.Skip(1).ToArray();
            var returnType = Type.SubTypes[0];

            if (args.Length != parameterTypes.Length)
                throw new Exception("目标delegate参数数量不正确，应为" + parameterTypes.Length + "，实为" +
                                    args.Length);

            for (var i = 0; i < args.Length; i++)
            {
                bool typeCheckSuccess;
                var arg = args[i];
                if (arg == null)
                {
                    typeCheckSuccess = parameterTypes[i].BasicType is BasicType.Object or BasicType.Interface or BasicType.Delegate or BasicType.String;
                }
                else
                {
                    var argType = arg.GetType();
                    switch (parameterTypes[i].BasicType)
                    {
                        case BasicType.Int:
                        case BasicType.Enum:
                            typeCheckSuccess = argType == typeof(int);
                            break;
                        case BasicType.Float:
                            typeCheckSuccess = argType == typeof(float);
                            break;
                        case BasicType.Bool:
                            typeCheckSuccess = argType == typeof(bool);
                            break;
                        case BasicType.String:
                            typeCheckSuccess = argType == typeof(string);
                            break;
                        case BasicType.Object:
                        case BasicType.Interface:
                            typeCheckSuccess = typeof(GorgeObject).IsAssignableFrom(argType);
                            break;
                        default:
                            throw new Exception($"不支持该类型：{parameterTypes[i]}");
                    }
                }

                if (!typeCheckSuccess)
                {
                    throw new Exception("目标delegate参数" + (i + 1) + "类型不正确，应为" + parameterTypes[i] +
                                        "，实为" + (args[i]?.GetType().ToString() ?? "null"));
                }
            }

            var argumentCount = new TypeCount();

            // 参数布置
            for (var i = 0; i < args.Length; i++)
            {
                switch (parameterTypes[i].BasicType)
                {
                    case BasicType.Int:
                    case BasicType.Enum:
                        InvokeParameterPool.Int[argumentCount.Count(parameterTypes[i].BasicType)] = (int) args[i];
                        break;
                    case BasicType.Float:
                        InvokeParameterPool.Float[argumentCount.Count(parameterTypes[i].BasicType)] = (float) args[i];
                        break;
                    case BasicType.Bool:
                        InvokeParameterPool.Bool[argumentCount.Count(parameterTypes[i].BasicType)] = (bool) args[i];
                        break;
                    case BasicType.String:
                        InvokeParameterPool.String[argumentCount.Count(parameterTypes[i].BasicType)] = (string) args[i];
                        break;
                    case BasicType.Object:
                    case BasicType.Interface:
                        InvokeParameterPool.Object[argumentCount.Count(parameterTypes[i].BasicType)] =
                            (GorgeObject) args[i];
                        break;
                    default:
                        throw new Exception("不支持该类型");
                }
            }

            // 调用
            Invoke();

            // 取出返回值
            if (returnType == null)
            {
                return null;
            }

            return returnType.BasicType switch
            {
                BasicType.Int or BasicType.Enum => InvokeParameterPool.IntReturn,
                BasicType.Float => InvokeParameterPool.FloatReturn,
                BasicType.Bool => InvokeParameterPool.BoolReturn,
                BasicType.String => InvokeParameterPool.StringReturn,
                BasicType.Object or BasicType.Interface => InvokeParameterPool.ObjectReturn,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public override int GetIntField(int fieldIndex)
        {
            return _outerValuePool.Int[fieldIndex];
        }

        public override float GetFloatField(int fieldIndex)
        {
            return _outerValuePool.Float[fieldIndex];
        }

        public override bool GetBoolField(int fieldIndex)
        {
            return _outerValuePool.Bool[fieldIndex];
        }

        public override string GetStringField(int fieldIndex)
        {
            return _outerValuePool.String[fieldIndex];
        }

        public override GorgeObject GetObjectField(int fieldIndex)
        {
            return _outerValuePool.Object[fieldIndex];
        }

        public override void SetIntField(int fieldIndex, int value)
        {
            _outerValuePool.Int[fieldIndex] = value;
        }

        public override void SetFloatField(int fieldIndex, float value)
        {
            _outerValuePool.Float[fieldIndex] = value;
        }

        public override void SetBoolField(int fieldIndex, bool value)
        {
            _outerValuePool.Bool[fieldIndex] = value;
        }

        public override void SetStringField(int fieldIndex, string value)
        {
            _outerValuePool.String[fieldIndex] = value;
        }

        public override void SetObjectField(int fieldIndex, GorgeObject value)
        {
            _outerValuePool.Object[fieldIndex] = value;
        }
    }
}