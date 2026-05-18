using System;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeLanguage.VirtualMachine
{
    public class IntermediateCode
    {
        public Address Result;
        public IntermediateOperator Operator;
        public IOperand Left;
        public IOperand Right;

        public IntermediateCode()
        {
        }

        public override string ToString()
        {
            return $"{Result} = {Left} {Operator.ToString()} {Right}";
        }

        #region 对象上下文

        public static IntermediateCode LoadThis(Address resultAddress)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LoadThis
            };
        }

        public static IntermediateCode LoadField(GorgeType fieldType, Address resultAddress, IOperand objectOfField,
            IOperand fieldIndex)
        {
            switch (fieldType.BasicType)
            {
                case BasicType.Int:
                case BasicType.Enum:
                    return LoadIntField(resultAddress, objectOfField, fieldIndex);
                case BasicType.Float:
                    return LoadFloatField(resultAddress, objectOfField, fieldIndex);
                case BasicType.Bool:
                    return LoadBoolField(resultAddress, objectOfField, fieldIndex);
                case BasicType.String:
                    return LoadStringField(resultAddress, objectOfField, fieldIndex);
                case BasicType.Object:
                case BasicType.Interface:
                case BasicType.Delegate:
                    return LoadObjectField(resultAddress, objectOfField, fieldIndex);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static IntermediateCode LoadField(GorgeType fieldType, Address resultAddress, IOperand objectOfField,
            int fieldIndex)
        {
            return LoadField(fieldType, resultAddress, objectOfField, fieldIndex.ToImmediate());
        }

        public static IntermediateCode LoadIntField(Address resultAddress, IOperand objectOfField, IOperand fieldIndex)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LoadIntField,
                Left = objectOfField,
                Right = fieldIndex
            };
        }

        public static IntermediateCode LoadIntField(Address resultAddress, IOperand objectOfField, int fieldIndex)
        {
            return LoadIntField(resultAddress, objectOfField, fieldIndex.ToImmediate());
        }

        public static IntermediateCode LoadFloatField(Address resultAddress, IOperand objectOfField,
            IOperand fieldIndex)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LoadFloatField,
                Left = objectOfField,
                Right = fieldIndex
            };
        }

        public static IntermediateCode LoadFloatField(Address resultAddress, IOperand objectOfField, int fieldIndex)
        {
            return LoadFloatField(resultAddress, objectOfField, fieldIndex.ToImmediate());
        }

        public static IntermediateCode LoadBoolField(Address resultAddress, IOperand objectOfField, IOperand fieldIndex)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LoadBoolField,
                Left = objectOfField,
                Right = fieldIndex
            };
        }

        public static IntermediateCode LoadBoolField(Address resultAddress, IOperand objectOfField, int fieldIndex)
        {
            return LoadBoolField(resultAddress, objectOfField, fieldIndex.ToImmediate());
        }

        public static IntermediateCode LoadStringField(Address resultAddress, IOperand objectOfField,
            IOperand fieldIndex)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LoadStringField,
                Left = objectOfField,
                Right = fieldIndex
            };
        }

        public static IntermediateCode LoadStringField(Address resultAddress, IOperand objectOfField, int fieldIndex)
        {
            return LoadStringField(resultAddress, objectOfField, fieldIndex.ToImmediate());
        }

        public static IntermediateCode LoadObjectField(Address resultAddress, IOperand objectOfField,
            IOperand fieldIndex)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LoadObjectField,
                Left = objectOfField,
                Right = fieldIndex
            };
        }

        public static IntermediateCode LoadObjectField(Address resultAddress, IOperand objectOfField, int fieldIndex)
        {
            return LoadObjectField(resultAddress, objectOfField, fieldIndex.ToImmediate());
        }

        public static IntermediateCode SetField(GorgeType fieldType, Address objectOfField, IOperand fieldIndex,
            IOperand valueToAssign)
        {
            switch (fieldType.BasicType)
            {
                case BasicType.Int:
                case BasicType.Enum:
                    return SetIntField(objectOfField, fieldIndex, valueToAssign);
                case BasicType.Float:
                    return SetFloatField(objectOfField, fieldIndex, valueToAssign);
                case BasicType.Bool:
                    return SetBoolField(objectOfField, fieldIndex, valueToAssign);
                case BasicType.String:
                    return SetStringField(objectOfField, fieldIndex, valueToAssign);
                case BasicType.Object:
                case BasicType.Interface:
                case BasicType.Delegate:
                    return SetObjectField(objectOfField, fieldIndex, valueToAssign);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static IntermediateCode SetField(GorgeType fieldType, Address objectOfField, int fieldIndex,
            IOperand valueToAssign)
        {
            return SetField(fieldType, objectOfField, fieldIndex.ToImmediate(), valueToAssign);
        }

        public static IntermediateCode SetIntField(Address objectOfField, IOperand fieldIndex, IOperand valueToAssign)
        {
            return new IntermediateCode()
            {
                Result = objectOfField,
                Operator = IntermediateOperator.SetIntField,
                Left = fieldIndex,
                Right = valueToAssign
            };
        }

        public static IntermediateCode SetIntField(Address objectOfField, int fieldIndex, IOperand valueToAssign)
        {
            return SetIntField(objectOfField, fieldIndex.ToImmediate(), valueToAssign);
        }

        public static IntermediateCode SetFloatField(Address objectOfField, IOperand fieldIndex, IOperand valueToAssign)
        {
            return new IntermediateCode()
            {
                Result = objectOfField,
                Operator = IntermediateOperator.SetFloatField,
                Left = fieldIndex,
                Right = valueToAssign
            };
        }

        public static IntermediateCode SetFloatField(Address objectOfField, int fieldIndex, IOperand valueToAssign)
        {
            return SetFloatField(objectOfField, fieldIndex.ToImmediate(), valueToAssign);
        }

        public static IntermediateCode SetBoolField(Address objectOfField, IOperand fieldIndex, IOperand valueToAssign)
        {
            return new IntermediateCode()
            {
                Result = objectOfField,
                Operator = IntermediateOperator.SetBoolField,
                Left = fieldIndex,
                Right = valueToAssign
            };
        }

        public static IntermediateCode SetBoolField(Address objectOfField, int fieldIndex, IOperand valueToAssign)
        {
            return SetBoolField(objectOfField, fieldIndex.ToImmediate(), valueToAssign);
        }

        public static IntermediateCode SetStringField(Address objectOfField, IOperand fieldIndex,
            IOperand valueToAssign)
        {
            return new IntermediateCode()
            {
                Result = objectOfField,
                Operator = IntermediateOperator.SetStringField,
                Left = fieldIndex,
                Right = valueToAssign
            };
        }

        public static IntermediateCode SetStringField(Address objectOfField, int fieldIndex, IOperand valueToAssign)
        {
            return SetStringField(objectOfField, fieldIndex.ToImmediate(), valueToAssign);
        }

        public static IntermediateCode SetObjectField(Address objectOfField, IOperand fieldIndex,
            IOperand valueToAssign)
        {
            return new IntermediateCode()
            {
                Result = objectOfField,
                Operator = IntermediateOperator.SetObjectField,
                Left = fieldIndex,
                Right = valueToAssign
            };
        }

        public static IntermediateCode SetObjectField(Address objectOfField, int fieldIndex, IOperand valueToAssign)
        {
            return SetObjectField(objectOfField, fieldIndex.ToImmediate(), valueToAssign);
        }

        #endregion

        #region Injector上下文

        public static IntermediateCode LoadInjectorField(Address resultAddress, IOperand injectorObject,
            IOperand fieldIndex)
        {
            switch (resultAddress.Type.BasicType)
            {
                case BasicType.Int:
                case BasicType.Enum:
                    return LoadIntInjectorField(resultAddress, injectorObject, fieldIndex);
                case BasicType.Float:
                    return LoadFloatInjectorField(resultAddress, injectorObject, fieldIndex);
                case BasicType.Bool:
                    return LoadBoolInjectorField(resultAddress, injectorObject, fieldIndex);
                case BasicType.String:
                    return LoadStringInjectorField(resultAddress, injectorObject, fieldIndex);
                case BasicType.Object:
                case BasicType.Interface:
                case BasicType.Delegate:
                    return LoadObjectInjectorField(resultAddress, injectorObject, fieldIndex);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static IntermediateCode LoadInjectorField(Address resultAddress, IOperand injectorObject,
            int fieldIndex)
        {
            return LoadInjectorField(resultAddress, injectorObject, fieldIndex.ToImmediate());
        }

        public static IntermediateCode LoadIntInjectorField(Address resultAddress, IOperand injectorObject,
            IOperand fieldIndex)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LoadIntInjectorField,
                Left = injectorObject,
                Right = fieldIndex
            };
        }

        public static IntermediateCode LoadIntInjectorField(Address resultAddress, IOperand injectorObject,
            int fieldIndex)
        {
            return LoadIntInjectorField(resultAddress, injectorObject, fieldIndex.ToImmediate());
        }

        public static IntermediateCode LoadFloatInjectorField(Address resultAddress, IOperand injectorObject,
            IOperand fieldIndex)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LoadFloatInjectorField,
                Left = injectorObject,
                Right = fieldIndex
            };
        }

        public static IntermediateCode LoadFloatInjectorField(Address resultAddress, IOperand injectorObject,
            int fieldIndex)
        {
            return LoadFloatInjectorField(resultAddress, injectorObject, fieldIndex.ToImmediate());
        }

        public static IntermediateCode LoadBoolInjectorField(Address resultAddress, IOperand injectorObject,
            IOperand fieldIndex)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LoadBoolInjectorField,
                Left = injectorObject,
                Right = fieldIndex
            };
        }

        public static IntermediateCode LoadBoolInjectorField(Address resultAddress, IOperand injectorObject,
            int fieldIndex)
        {
            return LoadBoolInjectorField(resultAddress, injectorObject, fieldIndex.ToImmediate());
        }

        public static IntermediateCode LoadStringInjectorField(Address resultAddress, IOperand injectorObject,
            IOperand fieldIndex)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LoadStringInjectorField,
                Left = injectorObject,
                Right = fieldIndex
            };
        }

        public static IntermediateCode LoadStringInjectorField(Address resultAddress, IOperand injectorObject,
            int fieldIndex)
        {
            return LoadStringInjectorField(resultAddress, injectorObject, fieldIndex.ToImmediate());
        }

        public static IntermediateCode LoadObjectInjectorField(Address resultAddress, IOperand injectorObject,
            IOperand fieldIndex)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LoadObjectInjectorField,
                Left = injectorObject,
                Right = fieldIndex
            };
        }

        public static IntermediateCode LoadObjectInjectorField(Address resultAddress, IOperand injectorObject,
            int fieldIndex)
        {
            return LoadObjectInjectorField(resultAddress, injectorObject, fieldIndex.ToImmediate());
        }

        public static IntermediateCode SetInjectorField(GorgeType fieldType, Address injectorOfField,
            IOperand fieldIndex, IOperand valueToAssign)
        {
            switch (fieldType.BasicType)
            {
                case BasicType.Int:
                case BasicType.Enum:
                    return SetIntInjectorField(injectorOfField, fieldIndex, valueToAssign);
                case BasicType.Float:
                    return SetFloatInjectorField(injectorOfField, fieldIndex, valueToAssign);
                case BasicType.Bool:
                    return SetBoolInjectorField(injectorOfField, fieldIndex, valueToAssign);
                case BasicType.String:
                    return SetStringInjectorField(injectorOfField, fieldIndex, valueToAssign);
                case BasicType.Object:
                case BasicType.Interface:
                case BasicType.Delegate:
                    return SetObjectInjectorField(injectorOfField, fieldIndex, valueToAssign);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static IntermediateCode SetInjectorField(GorgeType fieldType, Address injectorOfField, int fieldIndex,
            IOperand valueToAssign)
        {
            return SetInjectorField(fieldType, injectorOfField, fieldIndex.ToImmediate(), valueToAssign);
        }

        public static IntermediateCode SetIntInjectorField(Address injectorOfField, IOperand fieldIndex,
            IOperand valueToAssign)
        {
            return new IntermediateCode()
            {
                Result = injectorOfField,
                Operator = IntermediateOperator.SetIntInjectorField,
                Left = fieldIndex,
                Right = valueToAssign
            };
        }

        public static IntermediateCode SetIntInjectorField(Address injectorOfField, int fieldIndex
            , IOperand valueToAssign)
        {
            return LoadIntInjectorField(injectorOfField, fieldIndex.ToImmediate(), valueToAssign);
        }

        public static IntermediateCode SetFloatInjectorField(Address injectorOfField, IOperand fieldIndex,
            IOperand valueToAssign)
        {
            var result = new IntermediateCode()
            {
                Result = injectorOfField,
                Operator = IntermediateOperator.SetFloatInjectorField,
                Left = fieldIndex,
                Right = valueToAssign
            };

            return result;
        }

        public static IntermediateCode SetFloatInjectorField(Address injectorOfField, int fieldIndex
            , IOperand valueToAssign)
        {
            return SetFloatInjectorField(injectorOfField, fieldIndex.ToImmediate(), valueToAssign);
        }

        public static IntermediateCode SetBoolInjectorField(Address injectorOfField, IOperand fieldIndex,
            IOperand valueToAssign)
        {
            return new IntermediateCode()
            {
                Result = injectorOfField,
                Operator = IntermediateOperator.SetBoolInjectorField,
                Left = fieldIndex,
                Right = valueToAssign
            };
        }

        public static IntermediateCode SetBoolInjectorField(Address injectorOfField, int fieldIndex
            , IOperand valueToAssign)
        {
            return SetBoolInjectorField(injectorOfField, fieldIndex.ToImmediate(), valueToAssign);
        }

        public static IntermediateCode SetStringInjectorField(Address injectorOfField, IOperand fieldIndex,
            IOperand valueToAssign)
        {
            return new IntermediateCode()
            {
                Result = injectorOfField,
                Operator = IntermediateOperator.SetStringInjectorField,
                Left = fieldIndex,
                Right = valueToAssign
            };
        }

        public static IntermediateCode SetStringInjectorField(Address injectorOfField, int fieldIndex
            , IOperand valueToAssign)
        {
            return SetStringInjectorField(injectorOfField, fieldIndex.ToImmediate(), valueToAssign);
        }

        public static IntermediateCode SetObjectInjectorField(Address injectorOfField, IOperand fieldIndex,
            IOperand valueToAssign)
        {
            return new IntermediateCode()
            {
                Result = injectorOfField,
                Operator = IntermediateOperator.SetObjectInjectorField,
                Left = fieldIndex,
                Right = valueToAssign
            };
        }

        public static IntermediateCode SetObjectInjectorField(Address injectorOfField, int fieldIndex
            , IOperand valueToAssign)
        {
            return SetObjectInjectorField(injectorOfField, fieldIndex.ToImmediate(), valueToAssign);
        }

        #endregion

        #region 调用

        #region 参数存取

        public static IntermediateCode SetIntParameter(IOperand parameterIndex, IOperand parameterValue)
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.SetIntParameter,
                Left = parameterIndex,
                Right = parameterValue
            };
        }

        public static IntermediateCode SetIntParameter(int parameterIndex, IOperand parameterValue)
        {
            return SetIntParameter(parameterIndex.ToImmediate(), parameterValue);
        }

        public static IntermediateCode SetFloatParameter(IOperand parameterIndex, IOperand parameterValue)
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.SetFloatParameter,
                Left = parameterIndex,
                Right = parameterValue
            };
        }

        public static IntermediateCode SetFloatParameter(int parameterIndex, IOperand parameterValue)
        {
            return SetFloatParameter(parameterIndex.ToImmediate(), parameterValue);
        }

        public static IntermediateCode SetBoolParameter(IOperand parameterIndex, IOperand parameterValue)
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.SetBoolParameter,
                Left = parameterIndex,
                Right = parameterValue
            };
        }

        public static IntermediateCode SetBoolParameter(int parameterIndex, IOperand parameterValue)
        {
            return SetBoolParameter(parameterIndex.ToImmediate(), parameterValue);
        }

        public static IntermediateCode SetStringParameter(IOperand parameterIndex, IOperand parameterValue)
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.SetStringParameter,
                Left = parameterIndex,
                Right = parameterValue
            };
        }

        public static IntermediateCode SetStringParameter(int parameterIndex, IOperand parameterValue)
        {
            return SetStringParameter(parameterIndex.ToImmediate(), parameterValue);
        }

        public static IntermediateCode SetObjectParameter(IOperand parameterIndex, IOperand parameterValue)
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.SetObjectParameter,
                Left = parameterIndex,
                Right = parameterValue
            };
        }

        public static IntermediateCode SetObjectParameter(int parameterIndex, IOperand parameterValue)
        {
            return SetObjectParameter(parameterIndex.ToImmediate(), parameterValue);
        }

        public static IntermediateCode SetParameter(GorgeType parameterType, IOperand parameterIndex, IOperand argument)
        {
            switch (parameterType.BasicType)
            {
                case BasicType.Int:
                case BasicType.Enum:
                    return SetIntParameter(parameterIndex, argument);
                case BasicType.Float:
                    return SetFloatParameter(parameterIndex, argument);
                case BasicType.Bool:
                    return SetBoolParameter(parameterIndex, argument);
                case BasicType.String:
                    return SetStringParameter(parameterIndex, argument);
                case BasicType.Object:
                case BasicType.Interface:
                case BasicType.Delegate:
                    return SetObjectParameter(parameterIndex, argument);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static IntermediateCode SetParameter(GorgeType parameterType, int parameterIndex, IOperand argument)
        {
            return SetParameter(parameterType, parameterIndex.ToImmediate(), argument);
        }

        public static IntermediateCode SetInjector(IOperand injector)
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.SetInjector,
                Right = injector,
            };
        }

        public static IntermediateCode LoadIntParameter(Address resultAddress, IOperand parameterIndex)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LoadIntParameter,
                Left = parameterIndex,
            };
        }

        public static IntermediateCode LoadIntParameter(Address resultAddress, int parameterIndex)
        {
            return LoadIntParameter(resultAddress, parameterIndex.ToImmediate());
        }

        public static IntermediateCode LoadFloatParameter(Address resultAddress, IOperand parameterIndex)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LoadFloatParameter,
                Left = parameterIndex,
            };
        }

        public static IntermediateCode LoadFloatParameter(Address resultAddress, int parameterIndex)
        {
            return LoadFloatParameter(resultAddress, parameterIndex.ToImmediate());
        }

        public static IntermediateCode LoadBoolParameter(Address resultAddress, IOperand parameterIndex)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LoadBoolParameter,
                Left = parameterIndex,
            };
        }

        public static IntermediateCode LoadBoolParameter(Address resultAddress, int parameterIndex)
        {
            return LoadBoolParameter(resultAddress, parameterIndex.ToImmediate());
        }

        public static IntermediateCode LoadStringParameter(Address resultAddress, IOperand parameterIndex)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LoadStringParameter,
                Left = parameterIndex,
            };
        }

        public static IntermediateCode LoadStringParameter(Address resultAddress, int parameterIndex)
        {
            return LoadStringParameter(resultAddress, parameterIndex.ToImmediate());
        }

        public static IntermediateCode LoadObjectParameter(Address resultAddress, IOperand parameterIndex)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LoadObjectParameter,
                Left = parameterIndex,
            };
        }

        public static IntermediateCode LoadObjectParameter(Address resultAddress, int parameterIndex)
        {
            return LoadObjectParameter(resultAddress, parameterIndex.ToImmediate());
        }

        public static IntermediateCode LoadParameter(GorgeType parameterType, Address resultAddress,
            IOperand parameterIndex)
        {
            switch (parameterType.BasicType)
            {
                case BasicType.Int:
                case BasicType.Enum:
                    return LoadIntParameter(resultAddress, parameterIndex);
                case BasicType.Float:
                    return LoadFloatParameter(resultAddress, parameterIndex);
                case BasicType.Bool:
                    return LoadBoolParameter(resultAddress, parameterIndex);
                case BasicType.String:
                    return LoadStringParameter(resultAddress, parameterIndex);
                case BasicType.Object:
                case BasicType.Interface:
                case BasicType.Delegate:
                    return LoadObjectParameter(resultAddress, parameterIndex);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static IntermediateCode LoadParameter(GorgeType parameterType, Address resultAddress, int parameterIndex)
        {
            return LoadParameter(parameterType, resultAddress, parameterIndex.ToImmediate());
        }

        public static IntermediateCode LoadInjector(Address resultAddress)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LoadInjector
            };
        }

        #endregion

        #region 方法调用

        public static IntermediateCode InvokeMethod(IOperand objectOfMethod,
            IOperand methodIndex)
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.InvokeMethod,
                Left = objectOfMethod,
                Right = methodIndex
            };
        }

        public static IntermediateCode InvokeMethod(IOperand objectOfMethod,
            int methodIndex)
        {
            return InvokeMethod(objectOfMethod, methodIndex.ToImmediate());
        }

        public static IntermediateCode InvokeStaticMethod(IOperand className, IOperand methodIndex)
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.InvokeStaticMethod,
                Left = className,
                Right = methodIndex
            };
        }

        public static IntermediateCode InvokeStaticMethod(string className, int methodIndex)
        {
            return InvokeStaticMethod(className.ToImmediate(), methodIndex.ToImmediate());
        }

        public static IntermediateCode InvokeDelegate(IOperand delegateInstance)
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.InvokeDelegate,
                Left = delegateInstance
            };
        }

        public static IntermediateCode InvokeInterfaceMethod(Address objectOfMethod, IOperand interfaceName,
            IOperand methodIndex)
        {
            return new IntermediateCode()
            {
                Result = objectOfMethod,
                Operator = IntermediateOperator.InvokeInterfaceMethod,
                Left = interfaceName,
                Right = methodIndex
            };
        }

        public static IntermediateCode InvokeInterfaceMethod(Address objectOfMethod, string interfaceName,
            int methodIndex)
        {
            return InvokeInterfaceMethod(objectOfMethod, interfaceName.ToImmediate(), methodIndex.ToImmediate());
        }

        public static IntermediateCode Return(IOperand returnValue, GorgeType returnType)
        {
            return new IntermediateCode()
            {
                Operator = returnType.BasicType switch
                {
                    BasicType.Int or BasicType.Enum => IntermediateOperator.ReturnInt,
                    BasicType.Float => IntermediateOperator.ReturnFloat,
                    BasicType.Bool => IntermediateOperator.ReturnBool,
                    BasicType.String => IntermediateOperator.ReturnString,
                    BasicType.Object or BasicType.Delegate or BasicType.Interface => IntermediateOperator.ReturnObject,
                    _ => throw new ArgumentOutOfRangeException()
                },
                Left = returnValue
            };
        }

        public static IntermediateCode GetReturn(Address resultAddress, GorgeType returnType)
        {
            return new IntermediateCode()
            {
                Operator = returnType.BasicType switch
                {
                    BasicType.Int or BasicType.Enum => IntermediateOperator.GetReturnInt,
                    BasicType.Float => IntermediateOperator.GetReturnFloat,
                    BasicType.Bool => IntermediateOperator.GetReturnBool,
                    BasicType.String => IntermediateOperator.GetReturnString,
                    BasicType.Object or BasicType.Delegate or BasicType.Interface => IntermediateOperator.GetReturnObject,
                    _ => throw new ArgumentOutOfRangeException()
                },
                Result = resultAddress
            };
        }

        public static IntermediateCode ReturnVoid()
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.ReturnVoid
            };
        }

        public static IntermediateCode InvokeConstructor(IOperand constructorIndex)
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.InvokeConstructor,
                Right = constructorIndex
            };
        }

        public static IntermediateCode InvokeConstructor(int constructorIndex)
        {
            return InvokeConstructor(constructorIndex.ToImmediate());
        }

        public static IntermediateCode InvokeInjectorConstructor(IOperand constructorIndex)
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.InvokeInjectorConstructor,
                Right = constructorIndex
            };
        }

        public static IntermediateCode InvokeInjectorConstructor(int constructorIndex)
        {
            return InvokeInjectorConstructor(constructorIndex.ToImmediate());
        }

        public static IntermediateCode DoConstruct(IOperand constructorIndex)
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.DoConstruct,
                Right = constructorIndex
            };
        }

        public static IntermediateCode DoConstruct(int constructorIndex)
        {
            return DoConstruct(constructorIndex.ToImmediate());
        }

        public static IntermediateCode ConstructDelegate(Address resultAddress, IOperand delegateIndex)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.ConstructDelegate,
                Left = delegateIndex
            };
        }

        public static IntermediateCode ConstructDelegate(Address resultAddress, int delegateIndex)
        {
            return ConstructDelegate(resultAddress, delegateIndex.ToImmediate());
        }

        #endregion

        #region [临时使用]数组构造方法调用

        public static IntermediateCode InvokeIntArrayConstructor(IOperand length, IOperand list)
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.InvokeIntArrayConstructor,
                Left = length,
                Right = list
            };
        }

        public static IntermediateCode InvokeFloatArrayConstructor(IOperand length, IOperand list)
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.InvokeFloatArrayConstructor,
                Left = length,
                Right = list
            };
        }

        public static IntermediateCode InvokeBoolArrayConstructor(IOperand length, IOperand list)
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.InvokeBoolArrayConstructor,
                Left = length,
                Right = list
            };
        }

        public static IntermediateCode InvokeStringArrayConstructor(IOperand length,
            IOperand list)
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.InvokeStringArrayConstructor,
                Left = length,
                Right = list
            };
        }

        public static IntermediateCode InvokeObjectArrayConstructor(IOperand length,
            IOperand list)
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.InvokeObjectArrayConstructor,
                Left = length,
                Right = list
            };
        }

        #endregion

        #endregion

        #region 赋值至临时变量

        public static IntermediateCode LocalAssign(Address resultAddress, IOperand valueToAssign)
        {
            return resultAddress.Type.BasicType switch
            {
                BasicType.Int or BasicType.Enum => LocalIntAssign(resultAddress, valueToAssign),
                BasicType.Float => LocalFloatAssign(resultAddress, valueToAssign),
                BasicType.Bool => LocalBoolAssign(resultAddress, valueToAssign),
                BasicType.String => LocalStringAssign(resultAddress, valueToAssign),
                BasicType.Object or BasicType.Interface or BasicType.Delegate => LocalObjectAssign(resultAddress,
                    valueToAssign),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static IntermediateCode LocalIntAssign(Address resultAddress, IOperand valueToAssign)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LocalIntAssign,
                Left = valueToAssign
            };
        }

        public static IntermediateCode LocalIntAssign(Address resultAddress, int valueToAssign)
        {
            return LocalIntAssign(resultAddress, valueToAssign.ToImmediate());
        }

        public static IntermediateCode LocalFloatAssign(Address resultAddress, IOperand valueToAssign)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LocalFloatAssign,
                Left = valueToAssign
            };
        }

        public static IntermediateCode LocalFloatAssign(Address resultAddress, float valueToAssign)
        {
            return LocalFloatAssign(resultAddress, valueToAssign.ToImmediate());
        }

        public static IntermediateCode LocalBoolAssign(Address resultAddress, IOperand valueToAssign)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LocalBoolAssign,
                Left = valueToAssign
            };
        }

        public static IntermediateCode LocalBoolAssign(Address resultAddress, bool valueToAssign)
        {
            return LocalBoolAssign(resultAddress, valueToAssign.ToImmediate());
        }

        public static IntermediateCode LocalStringAssign(Address resultAddress, IOperand valueToAssign)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LocalStringAssign,
                Left = valueToAssign
            };
        }

        public static IntermediateCode LocalStringAssign(Address resultAddress, string valueToAssign)
        {
            return LocalStringAssign(resultAddress, valueToAssign.ToImmediate());
        }

        public static IntermediateCode LocalObjectAssign(Address resultAddress, IOperand valueToAssign)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LocalObjectAssign,
                Left = valueToAssign
            };
        }

        public static IntermediateCode LocalObjectAssign(Address resultAddress, GorgeObject valueToAssign)
        {
            return LocalObjectAssign(resultAddress,
                valueToAssign.ToImmediate(resultAddress.Type.ClassName, resultAddress.Type.NamespaceName));
        }

        #endregion

        #region 控制流

        public static IntermediateCode Nop()
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.Nop
            };
        }

        /// <summary>
        /// Jump指令
        /// </summary>
        /// <returns></returns>
        public static IntermediateCode Jump(int jumpTarget)
        {
            var jump = UnpatchedJump();
            PatchJump(jumpTarget, jump);
            return jump;
        }

        /// <summary>
        /// 待回填的Jump指令
        /// </summary>
        /// <returns></returns>
        public static IntermediateCode UnpatchedJump()
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.Jump
            };
        }

        public static IntermediateCode UnpatchedJumpIfFalse(IOperand condition)
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.JumpIfFalse,
                Left = condition,
            };
        }

        public static IntermediateCode UnpatchedJumpIfTrue(IOperand condition)
        {
            return new IntermediateCode()
            {
                Operator = IntermediateOperator.JumpIfTrue,
                Left = condition,
            };
        }

        /// <summary>
        /// 回填Jump或JumpIfFalse字段
        /// </summary>
        /// <param name="jumpTarget"></param>
        /// <param name="jumpCode"></param>
        public static void PatchJump(int jumpTarget, IntermediateCode jumpCode)
        {
            jumpCode.Right = jumpTarget.ToImmediate();
        }

        #endregion

        #region 类型转换

        public static IntermediateCode IntCastToFloat(Address resultAddress, IOperand valueToCast)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.IntCastToFloat,
                Left = valueToCast
            };
        }

        public static IntermediateCode IntCastToString(Address resultAddress, IOperand valueToCast)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.IntCastToString,
                Left = valueToCast
            };
        }

        public static IntermediateCode FloatCastToInt(Address resultAddress, IOperand valueToCast)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.FloatCastToInt,
                Left = valueToCast
            };
        }

        public static IntermediateCode FloatCastToString(Address resultAddress, IOperand valueToCast)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.FloatCastToString,
                Left = valueToCast
            };
        }

        public static IntermediateCode BoolCastToString(Address resultAddress, IOperand valueToCast)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.BoolCastToString,
                Left = valueToCast
            };
        }

        public static IntermediateCode ObjectCastToObject(Address resultAddress, IOperand valueToCast)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.ObjectCastToObject,
                Left = valueToCast
            };
        }

        #endregion

        #region 运算

        #region 算术运算

        public static IntermediateCode IntAddition(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.IntAddition,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode FloatAddition(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.FloatAddition,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode StringAddition(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.StringAddition,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode IntSubtraction(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.IntSubtraction,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode FloatSubtraction(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.FloatSubtraction,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode IntMultiplication(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.IntMultiplication,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode FloatMultiplication(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.FloatMultiplication,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode IntDivision(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.IntDivision,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode FloatDivision(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.FloatDivision,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode IntRemainder(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.IntRemainder,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode FloatRemainder(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.FloatRemainder,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode IntOpposite(Address resultAddress, IOperand a)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.IntOpposite,
                Left = a
            };
        }

        public static IntermediateCode FloatOpposite(Address resultAddress, IOperand a)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.FloatOpposite,
                Left = a
            };
        }

        #endregion

        #region 逻辑运算

        public static IntermediateCode LogicalAnd(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LogicalAnd,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode LogicalOr(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LogicalOr,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode LogicalNot(Address resultAddress, IOperand a)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.LogicalNot,
                Left = a
            };
        }

        #endregion

        #region 比较运算

        public static IntermediateCode IntLess(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.IntLess,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode IntGreater(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.IntGreater,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode IntLessEqual(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.IntLessEqual,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode IntGreaterEqual(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.IntGreaterEqual,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode IntEquality(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.IntEquality,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode IntInequality(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.IntInequality,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode FloatLess(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.FloatLess,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode FloatGreater(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.FloatGreater,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode FloatLessEqual(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.FloatLessEqual,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode FloatGreaterEqual(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.FloatGreaterEqual,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode FloatEquality(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.FloatEquality,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode FloatInequality(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.FloatInequality,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode BoolEquality(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.BoolEquality,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode BoolInequality(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.BoolInequality,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode StringEquality(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.StringEquality,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode StringInequality(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.StringInequality,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode ObjectEquality(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.ObjectEquality,
                Left = a,
                Right = b
            };
        }

        public static IntermediateCode ObjectInequality(Address resultAddress, IOperand a, IOperand b)
        {
            return new IntermediateCode()
            {
                Result = resultAddress,
                Operator = IntermediateOperator.ObjectInequality,
                Left = a,
                Right = b
            };
        }

        #endregion

        #endregion

        #region 优化

        /// <summary>
        /// 依赖操作数
        /// 顺序固定为Left、Right、Result
        /// </summary>
        /// <returns></returns>
        public IOperand[] Dependencies()
        {
            switch (Operator)
            {
                case IntermediateOperator.Nop:
                case IntermediateOperator.LoadThis:
                case IntermediateOperator.ReturnVoid:
                case IntermediateOperator.LoadInjector:
                case IntermediateOperator.GetReturnFloat:
                case IntermediateOperator.GetReturnBool:
                case IntermediateOperator.GetReturnString:
                case IntermediateOperator.GetReturnObject:
                case IntermediateOperator.GetReturnInt:
                    return new IOperand[] {null, null, null};
                case IntermediateOperator.LocalIntAssign:
                case IntermediateOperator.LocalFloatAssign:
                case IntermediateOperator.LocalBoolAssign:
                case IntermediateOperator.LocalStringAssign:
                case IntermediateOperator.LocalObjectAssign:
                case IntermediateOperator.IntOpposite:
                case IntermediateOperator.FloatOpposite:
                case IntermediateOperator.LogicalNot:
                case IntermediateOperator.IntCastToFloat:
                case IntermediateOperator.FloatCastToInt:
                case IntermediateOperator.IntCastToString:
                case IntermediateOperator.FloatCastToString:
                case IntermediateOperator.BoolCastToString:
                case IntermediateOperator.ObjectCastToObject:
                case IntermediateOperator.ReturnInt:
                case IntermediateOperator.ReturnFloat:
                case IntermediateOperator.ReturnBool:
                case IntermediateOperator.ReturnString:
                case IntermediateOperator.ReturnObject:
                case IntermediateOperator.LoadIntParameter:
                case IntermediateOperator.LoadFloatParameter:
                case IntermediateOperator.LoadBoolParameter:
                case IntermediateOperator.LoadStringParameter:
                case IntermediateOperator.LoadObjectParameter:
                case IntermediateOperator.InvokeDelegate:
                case IntermediateOperator.ConstructDelegate:
                    return new IOperand[] {Left, null, null};
                case IntermediateOperator.Jump:
                case IntermediateOperator.SetInjector:
                case IntermediateOperator.InvokeConstructor:
                case IntermediateOperator.InvokeInjectorConstructor:
                case IntermediateOperator.DoConstruct:
                    return new IOperand[] {null, Right, null};
                case IntermediateOperator.IntAddition:
                case IntermediateOperator.FloatAddition:
                case IntermediateOperator.StringAddition:
                case IntermediateOperator.IntSubtraction:
                case IntermediateOperator.FloatSubtraction:
                case IntermediateOperator.IntMultiplication:
                case IntermediateOperator.FloatMultiplication:
                case IntermediateOperator.IntDivision:
                case IntermediateOperator.FloatDivision:
                case IntermediateOperator.IntRemainder:
                case IntermediateOperator.FloatRemainder:
                case IntermediateOperator.IntLess:
                case IntermediateOperator.FloatLess:
                case IntermediateOperator.IntGreater:
                case IntermediateOperator.FloatGreater:
                case IntermediateOperator.IntLessEqual:
                case IntermediateOperator.FloatLessEqual:
                case IntermediateOperator.IntGreaterEqual:
                case IntermediateOperator.FloatGreaterEqual:
                case IntermediateOperator.IntEquality:
                case IntermediateOperator.FloatEquality:
                case IntermediateOperator.BoolEquality:
                case IntermediateOperator.StringEquality:
                case IntermediateOperator.IntInequality:
                case IntermediateOperator.FloatInequality:
                case IntermediateOperator.BoolInequality:
                case IntermediateOperator.StringInequality:
                case IntermediateOperator.ObjectEquality:
                case IntermediateOperator.ObjectInequality:
                case IntermediateOperator.LogicalAnd:
                case IntermediateOperator.LogicalOr:
                case IntermediateOperator.LoadIntField:
                case IntermediateOperator.LoadFloatField:
                case IntermediateOperator.LoadBoolField:
                case IntermediateOperator.LoadStringField:
                case IntermediateOperator.LoadObjectField:
                case IntermediateOperator.LoadIntInjectorField:
                case IntermediateOperator.LoadFloatInjectorField:
                case IntermediateOperator.LoadBoolInjectorField:
                case IntermediateOperator.LoadStringInjectorField:
                case IntermediateOperator.LoadObjectInjectorField:
                case IntermediateOperator.JumpIfFalse:
                case IntermediateOperator.JumpIfTrue:
                case IntermediateOperator.SetIntParameter:
                case IntermediateOperator.SetFloatParameter:
                case IntermediateOperator.SetBoolParameter:
                case IntermediateOperator.SetStringParameter:
                case IntermediateOperator.SetObjectParameter:
                case IntermediateOperator.InvokeMethod:
                case IntermediateOperator.InvokeStaticMethod:
                case IntermediateOperator.InvokeIntArrayConstructor:
                case IntermediateOperator.InvokeBoolArrayConstructor:
                case IntermediateOperator.InvokeFloatArrayConstructor:
                case IntermediateOperator.InvokeStringArrayConstructor:
                case IntermediateOperator.InvokeObjectArrayConstructor:
                    return new IOperand[] {Left, Right, null};
                case IntermediateOperator.SetIntField:
                case IntermediateOperator.SetFloatField:
                case IntermediateOperator.SetBoolField:
                case IntermediateOperator.SetStringField:
                case IntermediateOperator.SetObjectField:
                case IntermediateOperator.SetIntInjectorField:
                case IntermediateOperator.SetFloatInjectorField:
                case IntermediateOperator.SetBoolInjectorField:
                case IntermediateOperator.SetStringInjectorField:
                case IntermediateOperator.SetObjectInjectorField:
                case IntermediateOperator.InvokeInterfaceMethod:
                    return new IOperand[] {Left, Right, Result};
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion
    }

    public enum IntermediateOperator
    {
        /// <summary>
        /// 空语句
        /// </summary>
        Nop,

        #region 域内运算

        /// <summary>
        /// 赋值，result = left
        /// </summary>
        LocalIntAssign,

        /// <summary>
        /// 赋值，result = left
        /// </summary>
        LocalFloatAssign,

        /// <summary>
        /// 赋值，result = left
        /// </summary>
        LocalBoolAssign,

        /// <summary>
        /// 赋值，result = left
        /// </summary>
        LocalStringAssign,

        /// <summary>
        /// 赋值，result = left
        /// </summary>
        LocalObjectAssign,

        /// <summary>
        /// int求相反数 result = -left
        /// </summary>
        IntOpposite,

        /// <summary>
        /// float求相反数 result = -left
        /// </summary>
        FloatOpposite,

        /// <summary>
        /// int求和 result = left + right
        /// </summary>
        IntAddition,

        /// <summary>
        /// float求和 result = left + right
        /// </summary>
        FloatAddition,

        /// <summary>
        /// string串接 result = left + right
        /// </summary>
        StringAddition,

        /// <summary>
        /// int求差 result = left - right
        /// </summary>
        IntSubtraction,

        /// <summary>
        /// float求和 result = left - right
        /// </summary>
        FloatSubtraction,

        /// <summary>
        /// int求积 result = left * right
        /// </summary>
        IntMultiplication,

        /// <summary>
        /// float求和 result = left * right
        /// </summary>
        FloatMultiplication,

        /// <summary>
        /// int求商 result = left / right
        /// </summary>
        IntDivision,

        /// <summary>
        /// float求和 result = left / right
        /// </summary>
        FloatDivision,

        /// <summary>
        /// int求余 result = left % right
        /// </summary>
        IntRemainder,

        /// <summary>
        /// float求和 result = left % right
        /// </summary>
        FloatRemainder,

        /// <summary>
        /// int小于 result = left &lt; right
        /// </summary>
        IntLess,

        /// <summary>
        /// float小于 result = left &lt; right
        /// </summary>
        FloatLess,

        /// <summary>
        /// int大于 result = left &gt; right
        /// </summary>
        IntGreater,

        /// <summary>
        /// float大于 result = left &gt; right
        /// </summary>
        FloatGreater,

        /// <summary>
        /// int小于等于 result = left &lt;= right
        /// </summary>
        IntLessEqual,

        /// <summary>
        /// float小于等于 result = left &lt;= right
        /// </summary>
        FloatLessEqual,

        /// <summary>
        /// int大于等于 result = left &gt;= right
        /// </summary>
        IntGreaterEqual,

        /// <summary>
        /// float大于等于 result = left &gt;= right
        /// </summary>
        FloatGreaterEqual,

        /// <summary>
        /// int相等 result = left == right
        /// </summary>
        IntEquality,

        /// <summary>
        /// float相等 result = left == right
        /// </summary>
        FloatEquality,

        /// <summary>
        /// bool相等 result = left == right
        /// </summary>
        BoolEquality,

        /// <summary>
        /// string相等 result = left == right
        /// </summary>
        StringEquality,

        /// <summary>
        /// int不相等 result = left != right
        /// </summary>
        IntInequality,

        /// <summary>
        /// float不相等 result = left != right
        /// </summary>
        FloatInequality,

        /// <summary>
        /// bool不相等 result = left != right
        /// </summary>
        BoolInequality,

        /// <summary>
        /// string不相等 result = left != right
        /// </summary>
        StringInequality,

        /// <summary>
        /// object相等 result = left == right
        /// </summary>
        ObjectEquality,

        /// <summary>
        /// object不相等 result = left != right
        /// </summary>
        ObjectInequality,

        /// <summary>
        /// 逻辑与 result = left && right
        /// </summary>
        LogicalAnd,

        /// <summary>
        /// 逻辑或 result = left || right
        /// </summary>
        LogicalOr,

        /// <summary>
        /// 逻辑非 result = !left
        /// </summary>
        LogicalNot,

        /// <summary>
        /// int强制类型转换为float result = left
        /// </summary>
        IntCastToFloat,

        /// <summary>
        /// float强制类型转换为int result = left
        /// </summary>
        FloatCastToInt,

        /// <summary>
        /// int强制类型转换为string result = left
        /// </summary>
        IntCastToString,

        /// <summary>
        /// float强制类型转换为string result = left
        /// </summary>
        FloatCastToString,

        /// <summary>
        /// bool强制类型转换为string result = left
        /// </summary>
        BoolCastToString,

        /// <summary>
        /// object强制类型转换为object
        /// result = left
        /// 由于object的类型不会改变，所以是域内运算
        /// TODO 目前不会进行任何检查，只是做地址类型转换
        /// </summary>
        ObjectCastToObject,

        /// <summary>
        /// 加载this指代的自身对象
        /// Result = this
        /// this可以认为是域内常量，所以不是对象运算
        /// </summary>
        LoadThis,

        #endregion

        #region 对象运算

        /// <summary>
        /// 赋值，Result.Left = Right
        /// Left为字段索引
        /// </summary>
        SetIntField,

        /// <summary>
        /// 赋值，Result.Left = Right
        /// Left为字段索引
        /// </summary>
        SetFloatField,

        /// <summary>
        /// 赋值，Result.Left = Right
        /// Left为字段索引
        /// </summary>
        SetBoolField,

        /// <summary>
        /// 赋值，Result.Left = Right
        /// Left为字段索引
        /// </summary>
        SetStringField,

        /// <summary>
        /// 赋值，Result.Left = Right
        /// Left为字段索引
        /// </summary>
        SetObjectField,

        /// <summary>
        /// 加载字段
        /// Result = Left.Right
        /// Left为Object，Right为字段索引
        /// </summary>
        LoadIntField,

        /// <summary>
        /// 加载字段
        /// Result = Left.Right
        /// Left为Object，Right为字段索引
        /// </summary>
        LoadFloatField,

        /// <summary>
        /// 加载字段
        /// Result = Left.Right
        /// Left为Object，Right为字段索引
        /// </summary>
        LoadBoolField,

        /// <summary>
        /// 加载字段
        /// Result = Left.Right
        /// Left为Object，Right为字段索引
        /// </summary>
        LoadStringField,

        /// <summary>
        /// 加载字段
        /// Result = Left.Right
        /// Left为Object，Right为字段索引
        /// </summary>
        LoadObjectField,

        /// <summary>
        /// 加载Injector字段
        /// Result = Left.^Right
        /// Left为Injector，Right为字段索引
        /// </summary>
        LoadIntInjectorField,

        /// <summary>
        /// 加载Injector字段
        /// Result = Left.^Right
        /// Left为Injector，Right为字段索引
        /// </summary>
        LoadFloatInjectorField,

        /// <summary>
        /// 加载Injector字段
        /// Result = Left.^Right
        /// Left为Injector，Right为字段索引
        /// </summary>
        LoadBoolInjectorField,

        /// <summary>
        /// 加载Injector字段
        /// Result = Left.^Right
        /// Left为Injector，Right为字段索引
        /// </summary>
        LoadStringInjectorField,

        /// <summary>
        /// 加载Injector字段
        /// Result = Left.^Right
        /// Left为Injector，Right为字段索引
        /// </summary>
        LoadObjectInjectorField,

        /// <summary>
        /// 设置Injector字段
        /// Result.^Left = Right
        /// Result为Injector，Left为字段索引
        /// </summary>
        SetIntInjectorField,

        /// <summary>
        /// 设置Injector字段
        /// Result.^Left = Right
        /// Result为Injector，Left为字段索引
        /// </summary>
        SetFloatInjectorField,

        /// <summary>
        /// 设置Injector字段
        /// Result.^Left = Right
        /// Result为Injector，Left为字段索引
        /// </summary>
        SetBoolInjectorField,

        /// <summary>
        /// 设置Injector字段
        /// Result.^Left = Right
        /// Result为Injector，Left为字段索引
        /// </summary>
        SetStringInjectorField,

        /// <summary>
        /// 设置Injector字段
        /// Result.^Left = Right
        /// Result为Injector，Left为字段索引
        /// </summary>
        SetObjectInjectorField,

        #endregion

        #region 调用

        /// <summary>
        /// 设置调用构造方法的Injector
        /// injector = Right
        /// </summary>
        SetInjector,

        /// <summary>
        /// 设置方法调用参数
        /// parameter(Left) = Right
        /// </summary>
        SetIntParameter,

        /// <summary>
        /// 设置方法调用参数
        /// parameter(Left) = Right
        /// </summary>
        SetFloatParameter,

        /// <summary>
        /// 设置方法调用参数
        /// parameter(Left) = Right
        /// </summary>
        SetBoolParameter,

        /// <summary>
        /// 设置方法调用参数
        /// parameter(Left) = Right
        /// </summary>
        SetStringParameter,

        /// <summary>
        /// 设置方法调用参数
        /// parameter(Left) = Right
        /// </summary>
        SetObjectParameter,

        /// <summary>
        /// 加载方法调用参数
        /// Result = parameter(Left)
        /// </summary>
        LoadIntParameter,

        /// <summary>
        /// 加载方法调用参数
        /// Result = parameter(Left)
        /// </summary>
        LoadFloatParameter,

        /// <summary>
        /// 加载方法调用参数
        /// Result = parameter(Left)
        /// </summary>
        LoadBoolParameter,

        /// <summary>
        /// 加载方法调用参数
        /// Result = parameter(Left)
        /// </summary>
        LoadStringParameter,

        /// <summary>
        /// 加载方法调用参数
        /// Result = parameter(Left)
        /// </summary>
        LoadObjectParameter,

        /// <summary>
        /// 加载调用构造方法的Injector
        /// Result = injector
        /// </summary>
        LoadInjector,

        /// <summary>
        /// 调用方法
        /// invoke Left.Right()
        /// Left为Object，Right为方法索引
        /// </summary>
        InvokeMethod,

        /// <summary>
        /// 调用方法
        /// invoke Left.Right()
        /// Left为类名，Right为方法索引
        /// </summary>
        InvokeStaticMethod,

        /// <summary>
        /// 调用接口方法
        /// invoke (Left)result.Right()
        /// Result为Object
        /// Left为接口名string
        /// Right为方法的接口索引
        /// </summary>
        InvokeInterfaceMethod,

        /// <summary>
        /// 调用构造方法
        /// new (构造方法编号为Right)
        /// 结果存在return槽中
        /// </summary>
        InvokeConstructor,

        /// <summary>
        /// 调用注入器构造方法
        /// new (注入器构造方法编号为Right)
        /// 结果存在return槽中
        /// </summary>
        InvokeInjectorConstructor,

        /// <summary>
        /// 调用delegate
        /// invoke left()
        /// left为delegate实例
        /// </summary>
        InvokeDelegate,

        /// <summary>
        /// 执行构造逻辑，构造方法编号为Right，带修饰对象为Constructor调用者
        /// DoConstruct(this)
        /// </summary>
        DoConstruct,

        /// <summary>
        /// 临时，调用IntArray的构造方法
        /// new int[Left]{Right}
        /// Left为数组长度地址
        /// Right为IntList地址
        /// </summary>
        InvokeIntArrayConstructor,

        /// <summary>
        /// 临时，调用IntArray的构造方法
        /// new int[Left]{Right}
        /// Left为数组长度地址
        /// Right为IntList地址
        /// </summary>
        InvokeFloatArrayConstructor,

        /// <summary>
        /// 临时，调用IntArray的构造方法
        /// new int[Left]{Right}
        /// Left为数组长度地址
        /// Right为IntList地址
        /// </summary>
        InvokeBoolArrayConstructor,

        /// <summary>
        /// 临时，调用StringArray的构造方法
        /// new string[Left]{Right}
        /// Left为数组长度地址
        /// Right为StringList地址
        /// </summary>
        InvokeStringArrayConstructor,

        /// <summary>
        /// 临时，调用ObjectArray的构造方法
        /// new object[Left]{Right}
        /// Left为数组长度地址
        /// Right为StringList地址
        /// </summary>
        InvokeObjectArrayConstructor,

        /// <summary>
        /// 取返回值
        /// result = return
        /// </summary>
        GetReturnInt,

        /// <summary>
        /// 取返回值
        /// result = return
        /// </summary>
        GetReturnFloat,

        /// <summary>
        /// 取返回值
        /// result = return
        /// </summary>
        GetReturnBool,

        /// <summary>
        /// 取返回值
        /// result = return
        /// </summary>
        GetReturnString,

        /// <summary>
        /// 取返回值
        /// result = return
        /// </summary>
        GetReturnObject,

        /// <summary>
        /// 构造代理对象
        /// result = new delegate:left
        /// left为delegate的定义编号
        /// </summary>
        ConstructDelegate,

        #endregion

        #region 跳转

        /// <summary>
        /// 条件跳转 if(left == false) then jump to right
        /// 跳转目标为中间代码行号
        /// </summary>
        JumpIfFalse,

        /// <summary>
        /// 条件跳转 if(left == true) then jump to right
        /// 跳转目标为中间代码行号
        /// </summary>
        JumpIfTrue,

        /// <summary>
        /// 强制跳转 jump to right
        /// 跳转目标为中间代码行号
        /// </summary>
        Jump,

        #endregion

        #region 退出

        /// <summary>
        /// 调用返回
        /// return left
        /// </summary>
        ReturnInt,

        /// <summary>
        /// 调用返回
        /// return left
        /// </summary>
        ReturnFloat,

        /// <summary>
        /// 调用返回
        /// return left
        /// </summary>
        ReturnBool,

        /// <summary>
        /// 调用返回
        /// return left
        /// </summary>
        ReturnString,

        /// <summary>
        /// 调用返回
        /// return left
        /// </summary>
        ReturnObject,

        /// <summary>
        /// 调用返回，无返回值
        /// return
        /// </summary>
        ReturnVoid,

        #endregion
    }
}