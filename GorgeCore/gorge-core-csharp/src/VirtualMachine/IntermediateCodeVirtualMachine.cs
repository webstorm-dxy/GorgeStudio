using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Gorge.GorgeLanguage.Objective;
using Gorge.Native.Gorge;

namespace Gorge.GorgeLanguage.VirtualMachine
{
    /// <summary>
    /// 直接执行中间代码的虚拟机，
    /// 暂时只能执行一个方法
    /// </summary>
    public class IntermediateCodeVirtualMachine
    {
        private readonly UnifiedOperandStack _stack = new();

        public void InvokeMethod(IVirtualMachineExecutable method, GorgeObject gorgeObject)
        {
            // method.Marker.Begin();

            var localVariableCount = method.LocalVariableCount;
            _stack.PushFrame(localVariableCount);

            #region 代码执行

            // Profiler.VmProcess.Begin();

            var codes = method.Code;

            for (var programCounter = 0; programCounter < codes.Length; programCounter++)
            {
                // Profiler.Temp.Begin();
                var code = codes[programCounter];
                // Profiler.Temp.End();
                var returnFlag = false;

                // Profiler.VmCode.Begin();
                switch (code.Operator)
                {
                    case IntermediateOperator.Nop:
                        break;
                    case IntermediateOperator.LocalIntAssign:
                        SetInt(code.Result, GetInt(code.Left));
                        break;
                    case IntermediateOperator.LocalFloatAssign:
                        SetFloat(code.Result, GetFloat(code.Left));
                        break;
                    case IntermediateOperator.LocalBoolAssign:
                        SetBool(code.Result, GetBool(code.Left));
                        break;
                    case IntermediateOperator.LocalStringAssign:
                        SetString(code.Result, GetString(code.Left));
                        break;
                    case IntermediateOperator.LocalObjectAssign:
                        SetObject(code.Result, GetObject(code.Left));
                        break;
                    case IntermediateOperator.SetIntField:
                        GetObject(code.Result)
                            .SetIntField(GetInt(code.Left), GetInt(code.Right));
                        break;
                    case IntermediateOperator.SetFloatField:
                        GetObject(code.Result).SetFloatField(GetInt(code.Left),
                            GetFloat(code.Right));
                        break;
                    case IntermediateOperator.SetBoolField:
                        GetObject(code.Result).SetBoolField(GetInt(code.Left),
                            GetBool(code.Right));
                        break;
                    case IntermediateOperator.SetStringField:
                        GetObject(code.Result).SetStringField(GetInt(code.Left),
                            GetString(code.Right));
                        break;
                    case IntermediateOperator.SetObjectField:
                        GetObject(code.Result).SetObjectField(GetInt(code.Left),
                            GetObject(code.Right));
                        break;
                    case IntermediateOperator.IntOpposite:
                        SetInt(code.Result, -GetInt(code.Left));
                        break;
                    case IntermediateOperator.FloatOpposite:
                        SetFloat(code.Result, -GetFloat(code.Left));
                        break;
                    case IntermediateOperator.IntAddition:
                        SetInt(code.Result,
                            GetInt(code.Left) + GetInt(code.Right));
                        break;
                    case IntermediateOperator.FloatAddition:
                        SetFloat(code.Result,
                            GetFloat(code.Left) + GetFloat(code.Right));
                        break;
                    case IntermediateOperator.StringAddition:
                        SetString(code.Result,
                            GetString(code.Left) + GetString(code.Right));
                        break;
                    case IntermediateOperator.IntSubtraction:
                        SetInt(code.Result,
                            GetInt(code.Left) - GetInt(code.Right));
                        break;
                    case IntermediateOperator.FloatSubtraction:
                        SetFloat(code.Result,
                            GetFloat(code.Left) - GetFloat(code.Right));
                        break;
                    case IntermediateOperator.IntMultiplication:
                        SetInt(code.Result,
                            GetInt(code.Left) * GetInt(code.Right));
                        break;
                    case IntermediateOperator.FloatMultiplication:
                        SetFloat(code.Result,
                            GetFloat(code.Left) * GetFloat(code.Right));
                        break;
                    case IntermediateOperator.IntDivision:
                        SetInt(code.Result,
                            GetInt(code.Left) / GetInt(code.Right));
                        break;
                    case IntermediateOperator.FloatDivision:
                        SetFloat(code.Result,
                            GetFloat(code.Left) / GetFloat(code.Right));
                        break;
                    case IntermediateOperator.IntRemainder:
                        SetInt(code.Result,
                            GetInt(code.Left) % GetInt(code.Right));
                        break;
                    case IntermediateOperator.FloatRemainder:
                        SetFloat(code.Result,
                            GetFloat(code.Left) % GetFloat(code.Right));
                        break;
                    case IntermediateOperator.IntLess:
                        SetBool(code.Result,
                            GetInt(code.Left) < GetInt(code.Right));
                        break;
                    case IntermediateOperator.FloatLess:
                        SetBool(code.Result,
                            GetFloat(code.Left) < GetFloat(code.Right));
                        break;
                    case IntermediateOperator.IntGreater:
                        SetBool(code.Result,
                            GetInt(code.Left) > GetInt(code.Right));
                        break;
                    case IntermediateOperator.FloatGreater:
                        SetBool(code.Result,
                            GetFloat(code.Left) > GetFloat(code.Right));
                        break;
                    case IntermediateOperator.IntLessEqual:
                        SetBool(code.Result,
                            GetInt(code.Left) <= GetInt(code.Right));
                        break;
                    case IntermediateOperator.FloatLessEqual:
                        SetBool(code.Result,
                            GetFloat(code.Left) <= GetFloat(code.Right));
                        break;
                    case IntermediateOperator.IntGreaterEqual:
                        SetBool(code.Result,
                            GetInt(code.Left) >= GetInt(code.Right));
                        break;
                    case IntermediateOperator.FloatGreaterEqual:
                        SetBool(code.Result,
                            GetFloat(code.Left) >= GetFloat(code.Right));
                        break;
                    case IntermediateOperator.IntEquality:
                        SetBool(code.Result,
                            GetInt(code.Left) == GetInt(code.Right));
                        break;
                    case IntermediateOperator.FloatEquality:
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        SetBool(code.Result,
                            GetFloat(code.Left) == GetFloat(code.Right));
                        break;
                    case IntermediateOperator.BoolEquality:
                        SetBool(code.Result,
                            GetBool(code.Left) == GetBool(code.Right));
                        break;
                    case IntermediateOperator.StringEquality:
                        SetBool(code.Result,
                            GetString(code.Left) == GetString(code.Right));
                        break;
                    case IntermediateOperator.ObjectEquality:
                        SetBool(code.Result, ReferenceEquals(GetObject(code.Left), GetObject(code.Right)));
                        break;
                    case IntermediateOperator.IntInequality:
                        SetBool(code.Result, GetInt(code.Left) != GetInt(code.Right));
                        break;
                    case IntermediateOperator.FloatInequality:
                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        SetBool(code.Result,
                            GetFloat(code.Left) != GetFloat(code.Right));
                        break;
                    case IntermediateOperator.BoolInequality:
                        SetBool(code.Result,
                            GetBool(code.Left) != GetBool(code.Right));
                        break;
                    case IntermediateOperator.StringInequality:
                        SetBool(code.Result,
                            GetString(code.Left) != GetString(code.Right));
                        break;
                    case IntermediateOperator.ObjectInequality:
                        SetBool(code.Result,
                            GetObject(code.Left) != GetObject(code.Right));
                        break;
                    case IntermediateOperator.LogicalAnd:
                        SetBool(code.Result,
                            GetBool(code.Left) && GetBool(code.Right));
                        break;
                    case IntermediateOperator.LogicalOr:
                        SetBool(code.Result,
                            GetBool(code.Left) || GetBool(code.Right));
                        break;
                    case IntermediateOperator.LogicalNot:
                        SetBool(code.Result, !GetBool(code.Left));
                        break;
                    case IntermediateOperator.IntCastToFloat:
                        SetFloat(code.Result, GetInt(code.Left));
                        break;
                    case IntermediateOperator.FloatCastToInt:
                        SetInt(code.Result, (int) GetFloat(code.Left));
                        break;
                    case IntermediateOperator.IntCastToString:
                        SetString(code.Result, GetInt(code.Left).ToString());
                        break;
                    case IntermediateOperator.FloatCastToString:
                        SetString(code.Result, GetFloat(code.Left).ToString(CultureInfo.InvariantCulture));
                        break;
                    case IntermediateOperator.BoolCastToString:
                        SetString(code.Result, GetBool(code.Left).ToString(CultureInfo.InvariantCulture));
                        break;
                    case IntermediateOperator.ObjectCastToObject:
                        // TODO 检查是否能进行类型转换
                        SetObject(code.Result, GetObject(code.Left));
                        break;
                    case IntermediateOperator.JumpIfFalse:
                        if (!GetBool(code.Left))
                        {
                            programCounter = GetInt(code.Right);
                            programCounter--; // TODO 检查跳转设置，是否目标为下一条指令，还是下一条指令前
                        }

                        break;
                    case IntermediateOperator.JumpIfTrue:
                        if (GetBool(code.Left))
                        {
                            programCounter = GetInt(code.Right);
                            programCounter--; // TODO 检查跳转设置，是否目标为下一条指令，还是下一条指令前
                        }

                        break;
                    case IntermediateOperator.Jump:
                        programCounter = GetInt(code.Right);
                        programCounter--;
                        break;
                    case IntermediateOperator.SetIntParameter:
                        InvokeParameterPool.Int[GetInt(code.Left)] = GetInt(code.Right);
                        break;
                    case IntermediateOperator.SetFloatParameter:
                        InvokeParameterPool.Float[GetInt(code.Left)] = GetFloat(code.Right);
                        break;
                    case IntermediateOperator.SetBoolParameter:
                        InvokeParameterPool.Bool[GetInt(code.Left)] = GetBool(code.Right);
                        break;
                    case IntermediateOperator.SetStringParameter:
                        InvokeParameterPool.String[GetInt(code.Left)] = GetString(code.Right);
                        break;
                    case IntermediateOperator.SetObjectParameter:
                        InvokeParameterPool.Object[GetInt(code.Left)] = GetObject(code.Right);
                        break;
                    case IntermediateOperator.LoadIntParameter:
                        SetInt(code.Result, InvokeParameterPool.Int[GetInt(code.Left)]);
                        break;
                    case IntermediateOperator.LoadFloatParameter:
                        SetFloat(code.Result, InvokeParameterPool.Float[GetInt(code.Left)]);
                        break;
                    case IntermediateOperator.LoadBoolParameter:
                        SetBool(code.Result, InvokeParameterPool.Bool[GetInt(code.Left)]);
                        break;
                    case IntermediateOperator.LoadStringParameter:
                        SetString(code.Result, InvokeParameterPool.String[GetInt(code.Left)]);
                        break;
                    case IntermediateOperator.LoadObjectParameter:
                        SetObject(code.Result, InvokeParameterPool.Object[GetInt(code.Left)]);
                        break;
                    case IntermediateOperator.InvokeMethod:
                        GetObject(code.Left).RealObject.InvokeMethod(GetInt(code.Right));
                        break;
                    case IntermediateOperator.InvokeStaticMethod:
                    {
                        var gorgeClass = method.GetClass(GetString(code.Left));
                        gorgeClass.InvokeStaticMethod(GetInt(code.Right));
                        break;
                    }
                    case IntermediateOperator.InvokeInterfaceMethod:
                        GetObject(code.Result).RealObject.InvokeInterfaceMethod(
                            GorgeLanguageRuntime.Instance.GetInterface(GetString(code.Left)).Type,
                            GetInt(code.Right));
                        break;
                    case IntermediateOperator.InvokeDelegate:
                    {
                        var delegateInstance = (GorgeDelegate) GetObject(code.Left);
                        delegateInstance.Invoke();
                        break;
                    }
                    case IntermediateOperator.InvokeConstructor:
                    {
                        var index = GetInt(code.Right);
                        var gorgeClass =
                            method.GetClass(InvokeParameterPool.Injector.InjectedClassDeclaration.Name);
                        gorgeClass.InvokeConstructor(index);
                        break;
                    }
                    case IntermediateOperator.InvokeInjectorConstructor:
                    {
                        var index = GetInt(code.Right);
                        var gorgeClass =
                            method.GetClass(InvokeParameterPool.Injector
                                .InjectedClassDeclaration
                                .Name);
                        var realIndex = gorgeClass.Declaration.InjectorConstructorImplementationId[index];
                        gorgeClass.InvokeConstructor(realIndex);
                        break;
                    }
                    case IntermediateOperator.DoConstruct:
                    {
                        var index = GetInt(code.Right);
                        var gorgeClass =
                            method.GetClass(InvokeParameterPool.Injector
                                .InjectedClassDeclaration
                                .Name);
                        gorgeClass.InvokeDoConstructor(gorgeObject, index);
                        break;
                    }
                    case IntermediateOperator.InvokeIntArrayConstructor:
                    {
                        var length = GetInt(code.Left);
                        var intList = GetObject(code.Right);
                        InvokeParameterPool.ObjectReturn = new IntArray(length, (IntList) intList);
                        break;
                    }
                    case IntermediateOperator.InvokeBoolArrayConstructor:
                    {
                        var length = GetInt(code.Left);
                        var boolList = GetObject(code.Right);
                        InvokeParameterPool.ObjectReturn = new BoolArray(length, (BoolList) boolList);
                        break;
                    }
                    case IntermediateOperator.InvokeFloatArrayConstructor:
                    {
                        var length = GetInt(code.Left);
                        var intList = GetObject(code.Right);
                        InvokeParameterPool.ObjectReturn = new FloatArray(length, (FloatList) intList);
                        break;
                    }
                    case IntermediateOperator.InvokeStringArrayConstructor:
                    {
                        var length = GetInt(code.Left);
                        var stringList = GetObject(code.Right);
                        InvokeParameterPool.ObjectReturn = new StringArray(length, (StringList) stringList);
                        break;
                    }
                    case IntermediateOperator.InvokeObjectArrayConstructor:
                    {
                        var length = GetInt(code.Left);
                        var objectList = GetObject(code.Right);
                        InvokeParameterPool.ObjectReturn = new ObjectArray(length, (ObjectList) objectList);
                        break;
                    }
                    case IntermediateOperator.ConstructDelegate:
                    {
                        GorgeDelegateImplementation delegateImplementation;
                        // lambda表达式嵌套的情况，实现代码存储在上级实现中
                        if (gorgeObject is CompiledGorgeDelegate d)
                        {
                            delegateImplementation = d.Implementation.DelegateImplementations[GetInt(code.Left)];
                        }
                        // 非嵌套情况，实现代码存储在类实现中
                        else if (gorgeObject.GorgeClass is CompiledGorgeClass @class)
                        {
                            delegateImplementation = @class.DelegateImplementation[GetInt(code.Left)];
                        }
                        else
                        {
                            throw new Exception("不能在native类中创建delegate");
                        }

                        SetObject(code.Result, new CompiledGorgeDelegate(delegateImplementation));
                        break;
                    }
                    case IntermediateOperator.ReturnInt:
                        InvokeParameterPool.IntReturn = GetInt(code.Left);
                        returnFlag = true;
                        break;
                    case IntermediateOperator.ReturnFloat:
                        InvokeParameterPool.FloatReturn = GetFloat(code.Left);
                        returnFlag = true;
                        break;
                    case IntermediateOperator.ReturnBool:
                        InvokeParameterPool.BoolReturn = GetBool(code.Left);
                        returnFlag = true;
                        break;
                    case IntermediateOperator.ReturnString:
                        InvokeParameterPool.StringReturn = GetString(code.Left);
                        returnFlag = true;
                        break;
                    case IntermediateOperator.ReturnObject:
                        InvokeParameterPool.ObjectReturn = GetObject(code.Left);
                        returnFlag = true;
                        break;
                    case IntermediateOperator.ReturnVoid:
                        returnFlag = true;
                        break;
                    case IntermediateOperator.LoadThis:
                        SetObject(code.Result, gorgeObject);
                        break;
                    case IntermediateOperator.LoadInjector:
                        SetObject(code.Result, InvokeParameterPool.Injector);
                        break;
                    case IntermediateOperator.SetInjector:
                        InvokeParameterPool.Injector = (Injector) GetObject(code.Right);
                        break;
                    case IntermediateOperator.LoadIntField:
                        SetInt(code.Result, GetObject(code.Left).RealObject.GetIntField(GetInt(code.Right)));
                        break;
                    case IntermediateOperator.LoadFloatField:
                        SetFloat(code.Result,
                            GetObject(code.Left).RealObject.GetFloatField(GetInt(code.Right)));
                        break;
                    case IntermediateOperator.LoadBoolField:
                        SetBool(code.Result,
                            GetObject(code.Left).RealObject.GetBoolField(GetInt(code.Right)));
                        break;
                    case IntermediateOperator.LoadStringField:
                        SetString(code.Result,
                            GetObject(code.Left).RealObject.GetStringField(GetInt(code.Right)));
                        break;
                    case IntermediateOperator.LoadObjectField:
                        SetObject(code.Result,
                            GetObject(code.Left).RealObject.GetObjectField(GetInt(code.Right)));
                        break;
                    case IntermediateOperator.LoadIntInjectorField:
                    {
                        var index = GetInt(code.Right);
                        var injector = (Injector) GetObject(code.Left);
                        if (injector.GetInjectorIntDefault(index))
                        {
                            if (injector.InjectedClassDeclaration.TryGetInjectorIntFieldByIndex(index,
                                    out var field))
                            {
                                if (field.DefaultValueIndex != null)
                                {
                                    var defaultValue = method.GetClass(injector.InjectedClassDeclaration.Name)
                                        .GetInjectorIntDefaultValue(field.DefaultValueIndex.Value);
                                    SetInt(code.Result, defaultValue);
                                    break;
                                }

                                throw new Exception(
                                    $"{injector.InjectedClassDeclaration.Name}类的Injector索引为{index}的int字段没有被设置，且没有默认值");
                            }

                            throw new Exception(
                                $"{injector.InjectedClassDeclaration.Name}类的Injector没有名为索引为{index}的int字段");
                        }

                        SetInt(code.Result, injector.GetInjectorInt(index));
                        break;
                    }
                    case IntermediateOperator.LoadFloatInjectorField:
                    {
                        var index = GetInt(code.Right);
                        var injector = (Injector) GetObject(code.Left);
                        if (injector.GetInjectorFloatDefault(index))
                        {
                            if (injector.InjectedClassDeclaration.TryGetInjectorFloatFieldByIndex(index,
                                    out var field))
                            {
                                if (field.DefaultValueIndex != null)
                                {
                                    var defaultValue = method.GetClass(injector.InjectedClassDeclaration.Name)
                                        .GetInjectorFloatDefaultValue(field.DefaultValueIndex.Value);
                                    SetFloat(code.Result, defaultValue);
                                    break;
                                }

                                throw new Exception(
                                    $"{injector.InjectedClassDeclaration.Name}类的Injector索引为{index}的float字段没有被设置，且没有默认值");
                            }

                            throw new Exception(
                                $"{injector.InjectedClassDeclaration.Name}类的Injector没有名为索引为{index}的float字段");
                        }

                        SetFloat(code.Result, injector.GetInjectorFloat(index));
                        break;
                    }
                    case IntermediateOperator.LoadBoolInjectorField:
                    {
                        var index = GetInt(code.Right);
                        var injector = (Injector) GetObject(code.Left);
                        if (injector.GetInjectorBoolDefault(index))
                        {
                            if (injector.InjectedClassDeclaration.TryGetInjectorBoolFieldByIndex(index,
                                    out var field))
                            {
                                if (field.DefaultValueIndex != null)
                                {
                                    var defaultValue = method.GetClass(injector.InjectedClassDeclaration.Name)
                                        .GetInjectorBoolDefaultValue(field.DefaultValueIndex.Value);
                                    SetBool(code.Result, defaultValue);
                                    break;
                                }

                                throw new Exception(
                                    $"{injector.InjectedClassDeclaration.Name}类的Injector索引为{index}的bool字段没有被设置，且没有默认值");
                            }

                            throw new Exception(
                                $"{injector.InjectedClassDeclaration.Name}类的Injector没有名为索引为{index}的bool字段");
                        }

                        SetBool(code.Result, injector.GetInjectorBool(index));
                        break;
                    }
                    case IntermediateOperator.LoadStringInjectorField:
                    {
                        var index = GetInt(code.Right);
                        var injector = (Injector) GetObject(code.Left);
                        if (injector.GetInjectorStringDefault(index))
                        {
                            if (injector.InjectedClassDeclaration
                                .TryGetInjectorStringFieldByIndex(index, out var field))
                            {
                                if (field.DefaultValueIndex != null)
                                {
                                    var defaultValue = method.GetClass(injector.InjectedClassDeclaration.Name)
                                        .GetInjectorStringDefaultValue(field.DefaultValueIndex.Value);
                                    SetString(code.Result, defaultValue);
                                    break;
                                }

                                throw new Exception(
                                    $"{injector.InjectedClassDeclaration.Name}类的Injector索引为{index}的string字段没有被设置，且没有默认值");
                            }

                            throw new Exception(
                                $"{injector.InjectedClassDeclaration.Name}类的Injector没有名为索引为{index}的string字段");
                        }

                        SetString(code.Result, injector.GetInjectorString(index));
                        break;
                    }
                    case IntermediateOperator.LoadObjectInjectorField:
                    {
                        var index = GetInt(code.Right);
                        var injector = (Injector) GetObject(code.Left);
                        if (injector.GetInjectorObjectDefault(index))
                        {
                            if (injector.InjectedClassDeclaration
                                .TryGetInjectorObjectFieldByIndex(index, out var field))
                            {
                                if (field.DefaultValueIndex != null)
                                {
                                    var defaultValue = method.GetClass(injector.InjectedClassDeclaration.Name)
                                        .GetInjectorObjectDefaultValue(field.DefaultValueIndex.Value);
                                    SetObject(code.Result, defaultValue);
                                    break;
                                }

                                throw new Exception(
                                    $"{injector.InjectedClassDeclaration.Name}类的Injector索引为{index}的object字段没有被设置，且没有默认值");
                            }

                            throw new Exception(
                                $"{injector.InjectedClassDeclaration.Name}类的Injector没有名为索引为{index}的object字段");
                        }

                        SetObject(code.Result, injector.GetInjectorObject(index));
                        break;
                    }
                    case IntermediateOperator.SetIntInjectorField:
                        ((Injector) GetObject(code.Result)).SetInjectorInt(GetInt(code.Left), GetInt(code.Right));
                        break;
                    case IntermediateOperator.SetFloatInjectorField:
                        ((Injector) GetObject(code.Result)).SetInjectorFloat(GetInt(code.Left),
                            GetFloat(code.Right));
                        break;
                    case IntermediateOperator.SetBoolInjectorField:
                        ((Injector) GetObject(code.Result)).SetInjectorBool(GetInt(code.Left), GetBool(code.Right));
                        break;
                    case IntermediateOperator.SetStringInjectorField:
                        ((Injector) GetObject(code.Result)).SetInjectorString(GetInt(code.Left),
                            GetString(code.Right));
                        break;
                    case IntermediateOperator.SetObjectInjectorField:
                        ((Injector) GetObject(code.Result)).SetInjectorObject(GetInt(code.Left),
                            GetObject(code.Right));
                        break;
                    case IntermediateOperator.GetReturnInt:
                        SetInt(code.Result, InvokeParameterPool.IntReturn);
                        break;
                    case IntermediateOperator.GetReturnFloat:
                        SetFloat(code.Result, InvokeParameterPool.FloatReturn);
                        break;
                    case IntermediateOperator.GetReturnBool:
                        SetBool(code.Result, InvokeParameterPool.BoolReturn);
                        break;
                    case IntermediateOperator.GetReturnString:
                        SetString(code.Result, InvokeParameterPool.StringReturn);
                        break;
                    case IntermediateOperator.GetReturnObject:
                        SetObject(code.Result, InvokeParameterPool.ObjectReturn);
                        break;
                    default:
                        throw new Exception($"不支持执行该代码{code}");
                }


                // Profiler.VmCode.End();

                if (returnFlag)
                {
                    break;
                }
            }

            // Profiler.VmProcess.End();

            #endregion

            _stack.PopFrame();

            // method.Marker.End();
        }

        private int GetInt(IOperand operand)
        {
            return operand switch
            {
                Immediate immediate => (int)immediate.Value,
                Address address => _stack.Int(address.Index),
                _ => throw new Exception($"暂不支持此类操作数{operand.GetType()}")
            };
        }

        private float GetFloat(IOperand operand)
        {
            return operand switch
            {
                Immediate immediate => immediate.Value is int i ? i : (float)immediate.Value,
                Address address => _stack.Float(address.Index),
                _ => throw new Exception("暂不支持此类操作数")
            };
        }

        private bool GetBool(IOperand operand)
        {
            return operand switch
            {
                Immediate immediate => (bool)immediate.Value,
                Address address => _stack.Bool(address.Index),
                _ => throw new Exception("暂不支持此类操作数")
            };
        }

        private string GetString(IOperand operand)
        {
            return operand switch
            {
                Immediate immediate => (string)immediate.Value,
                Address address => _stack.String(address.Index),
                _ => throw new Exception("暂不支持此类操作数")
            };
        }

        private GorgeObject GetObject(IOperand operand)
        {
            return operand switch
            {
                Immediate immediate => (GorgeObject)immediate.Value,
                Address address => _stack.Object(address.Index),
                _ => throw new Exception("暂不支持此类操作数")
            };
        }

        private void SetInt(Address address, int value)
        {
            _stack.Int(address.Index) = value;
        }

        private void SetFloat(Address address, float value)
        {
            _stack.Float(address.Index) = value;
        }

        private void SetBool(Address address, bool value)
        {
            _stack.Bool(address.Index) = value;
        }

        private void SetString(Address address, string value)
        {
            _stack.String(address.Index) = value;
        }

        private void SetObject(Address address, GorgeObject value)
        {
            _stack.Object(address.Index) = value;
        }
    }
}