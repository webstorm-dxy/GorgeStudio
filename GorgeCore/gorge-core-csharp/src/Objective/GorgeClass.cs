#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Gorge.GorgeLanguage.VirtualMachine;
using Gorge.Native.Gorge;

namespace Gorge.GorgeLanguage.Objective
{
    /// <summary>
    /// Gorge类实现
    /// </summary>
    public abstract class GorgeClass
    {
        //public GorgeType Type => Declaration.Type;

        /// <summary>
        /// 构造本类的空白注入器
        /// </summary>
        /// <returns></returns>
        public abstract Injector EmptyInjector();

        public abstract ClassDeclaration Declaration { get; }

        public abstract GorgeClass? SuperClass { get; }

        /// <summary>
        /// 继承树中距离本类最近的Native类
        /// </summary>
        public abstract GorgeClass LatestNativeClass { get; }

        /// <summary>
        /// 调用指定ID的方法，不含参数布置和返回值获取过程
        /// </summary>
        /// <param name="gorgeObject"></param>
        /// <param name="methodId"></param>
        public abstract void InvokeMethod(GorgeObject gorgeObject, int methodId);

        /// <summary>
        /// 调用指定ID的静态方法，不含参数布置和返回值获取过程
        /// </summary>
        /// <param name="methodId"></param>
        public virtual void InvokeStaticMethod(int methodId)
        {
            throw new Exception("暂不支持调用native的static方法");
        }

        public void InvokeInterfaceMethod(GorgeObject gorgeObject, GorgeType interfaceType, int interfaceMethodId)
        {
            InvokeMethod(gorgeObject, GetInterfaceMethodImplementationId(interfaceType, interfaceMethodId));
        }

        /// <summary>
        /// 获取接口方法的实现的方法ID
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <param name="interfaceMethodId"></param>
        /// <returns></returns>
        public virtual int GetInterfaceMethodImplementationId(GorgeType interfaceType, int interfaceMethodId)
        {
            return Declaration.InterfaceMethodImplementationId[interfaceType.FullName][interfaceMethodId];
        }

        /// <summary>
        /// 调用构造方法，生成GorgeObject并存储在调用参数池的返回值中
        /// </summary>
        /// <param name="constructorId"></param>
        public void InvokeConstructor(int constructorId)
        {
            if (constructorId < Declaration.ConstructorStartId || constructorId >= Declaration.ConstructorCount)
            {
                throw new Exception($"类{Declaration.Name}没有编号为{constructorId}的构造方法");
            }

            InvokeParameterPool.ObjectReturn = DoConstruct(null, constructorId);
        }

        /// <summary>
        /// 调用构造方法，修饰目标object
        /// </summary>
        /// <param name="targetObject"></param>
        /// <param name="constructorId"></param>
        public void InvokeDoConstructor(GorgeObject targetObject, int constructorId)
        {
            if (constructorId < Declaration.ConstructorStartId || constructorId >= Declaration.ConstructorCount)
            {
                if (SuperClass == null)
                {
                    throw new Exception($"类{Declaration.Name}没有编号为{constructorId}的构造方法");
                }

                SuperClass.InvokeDoConstructor(targetObject, constructorId);
                return;
            }

            InvokeParameterPool.ObjectReturn = DoConstruct(targetObject, constructorId);
        }

        /// <summary>
        /// 执行构造方法，生成GorgeObject并返回
        /// </summary>
        /// <param name="targetObject">构造对象框架，如果为null则为起始构造的首层，需要由本函数负责创建构造对象框架</param>
        /// <param name="constructorId">构造方法编号</param>
        /// <returns>构造结果</returns>
        protected abstract GorgeObject DoConstruct(GorgeObject? targetObject, int constructorId);

        /// <summary>
        /// 获取本类Injector的默认值
        /// </summary>
        /// <param name="defaultValueIndex"></param>
        /// <returns></returns>
        public virtual int GetInjectorIntDefaultValue(int defaultValueIndex)
        {
            throw new Exception($"类{Declaration.Name}的Injector编号为{defaultValueIndex}的int字段没有默认值");
        }

        /// <summary>
        /// 获取本类Injector的默认值
        /// </summary>
        /// <param name="defaultValueIndex"></param>
        /// <returns></returns>
        public virtual float GetInjectorFloatDefaultValue(int defaultValueIndex)
        {
            throw new Exception($"类{Declaration.Name}的Injector编号为{defaultValueIndex}的float字段没有默认值");
        }

        /// <summary>
        /// 获取本类Injector的默认值
        /// </summary>
        /// <param name="defaultValueIndex"></param>
        /// <returns></returns>
        public virtual bool GetInjectorBoolDefaultValue(int defaultValueIndex)
        {
            throw new Exception($"类{Declaration.Name}的Injector编号为{defaultValueIndex}的bool字段没有默认值");
        }

        /// <summary>
        /// 获取本类Injector的默认值
        /// </summary>
        /// <param name="defaultValueIndex"></param>
        /// <returns></returns>
        public virtual string GetInjectorStringDefaultValue(int defaultValueIndex)
        {
            throw new Exception($"类{Declaration.Name}的Injector编号为{defaultValueIndex}的string字段没有默认值");
        }

        /// <summary>
        /// 获取本类Injector的默认值
        /// </summary>
        /// <param name="defaultValueIndex"></param>
        /// <returns></returns>
        public virtual GorgeObject GetInjectorObjectDefaultValue(int defaultValueIndex)
        {
            throw new Exception($"类{Declaration.Name}的Injector编号为{defaultValueIndex}的object字段没有默认值");
        }

        /// <summary>
        /// 获取本类注入器字段值，如未设置则返回默认值。
        /// 无默认值且无字段值的情况抛出异常
        /// </summary>
        /// <param name="injectorField"></param>
        /// <param name="injector"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public int GetInjectorIntValueOrDefault(InjectorFieldInformation injectorField, Injector injector)
        {
            if (injector.GetInjectorIntDefault(injectorField.Index))
            {
                if (injectorField.DefaultValueIndex == null)
                {
                    throw new Exception($"{Declaration.Name}类注入器的{injectorField.Name}字段没有默认值");
                }

                return GetInjectorIntDefaultValue(injectorField.DefaultValueIndex.Value);
            }

            return injector.GetInjectorInt(injectorField.Index);
        }

        /// <summary>
        /// 获取本类注入器字段值，如未设置则返回默认值。
        /// 无默认值且无字段值的情况抛出异常
        /// </summary>
        /// <param name="injectorField"></param>
        /// <param name="injector"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public float GetInjectorFloatValueOrDefault(InjectorFieldInformation injectorField, Injector injector)
        {
            if (injector.GetInjectorFloatDefault(injectorField.Index))
            {
                if (injectorField.DefaultValueIndex == null)
                {
                    throw new Exception($"{Declaration.Name}类注入器的{injectorField.Name}字段没有默认值");
                }

                return GetInjectorFloatDefaultValue(injectorField.DefaultValueIndex.Value);
            }

            return injector.GetInjectorFloat(injectorField.Index);
        }

        /// <summary>
        /// 获取本类注入器字段值，如未设置则返回默认值。
        /// 无默认值且无字段值的情况抛出异常
        /// </summary>
        /// <param name="injectorField"></param>
        /// <param name="injector"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool GetInjectorBoolValueOrDefault(InjectorFieldInformation injectorField, Injector injector)
        {
            if (injector.GetInjectorBoolDefault(injectorField.Index))
            {
                if (injectorField.DefaultValueIndex == null)
                {
                    throw new Exception($"{Declaration.Name}类注入器的{injectorField.Name}字段没有默认值");
                }

                return GetInjectorBoolDefaultValue(injectorField.DefaultValueIndex.Value);
            }

            return injector.GetInjectorBool(injectorField.Index);
        }

        /// <summary>
        /// 获取本类注入器字段值，如未设置则返回默认值。
        /// 无默认值且无字段值的情况抛出异常
        /// </summary>
        /// <param name="injectorField"></param>
        /// <param name="injector"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string GetInjectorStringValueOrDefault(InjectorFieldInformation injectorField, Injector injector)
        {
            if (injector.GetInjectorStringDefault(injectorField.Index))
            {
                if (injectorField.DefaultValueIndex == null)
                {
                    throw new Exception($"{Declaration.Name}类注入器的{injectorField.Name}字段没有默认值");
                }

                return GetInjectorStringDefaultValue(injectorField.DefaultValueIndex.Value);
            }

            return injector.GetInjectorString(injectorField.Index);
        }

        /// <summary>
        /// 获取本类注入器字段值，如未设置则返回默认值。
        /// 无默认值且无字段值的情况抛出异常
        /// </summary>
        /// <param name="injectorField"></param>
        /// <param name="injector"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public GorgeObject? GetInjectorObjectValueOrDefault(InjectorFieldInformation injectorField, Injector injector)
        {
            if (injector.GetInjectorObjectDefault(injectorField.Index))
            {
                if (injectorField.DefaultValueIndex == null)
                {
                    throw new Exception($"{Declaration.Name}类注入器的{injectorField.Name}字段没有默认值");
                }

                return GetInjectorObjectDefaultValue(injectorField.DefaultValueIndex.Value);
            }

            return injector.GetInjectorObject(injectorField.Index);
        }

        #region 反射调用

        /// <summary>
        /// 调用方法
        /// 含参数布置和返回值取出过程
        /// </summary>
        /// <param name="gorgeObject"></param>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public object? InvokeMethod(GorgeObject gorgeObject, MethodInformation method, object[] args)
        {
            if (args.Length != method.Parameters.Length)
                throw new Exception("目标方法" + method.Name + "参数数量不正确，应为" + method.Parameters.Length + "，实为" +
                                    args.Length);

            for (var i = 0; i < args.Length; i++)
            {
                var argType = args[i].GetType();
                bool typeCheckSuccess;
                switch (method.Parameters[i].Type.BasicType)
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
                        typeCheckSuccess = typeof(GorgeObject).IsAssignableFrom(argType);
                        break;
                    default:
                        throw new Exception("不支持该类型");
                }

                if (!typeCheckSuccess)
                {
                    throw new Exception("目标方法" + method.Name + "参数" + (i + 1) + "类型不正确，应为" + method.Parameters[i].Type +
                                        "，实为" + args[i].GetType());
                }
            }

            // 参数布置
            for (var i = 0; i < args.Length; i++)
            {
                switch (method.Parameters[i].Type.BasicType)
                {
                    case BasicType.Int:
                    case BasicType.Enum:
                        InvokeParameterPool.Int[method.Parameters[i].Index] = (int) args[i];
                        break;
                    case BasicType.Float:
                        InvokeParameterPool.Float[method.Parameters[i].Index] = (float) args[i];
                        break;
                    case BasicType.Bool:
                        InvokeParameterPool.Bool[method.Parameters[i].Index] = (bool) args[i];
                        break;
                    case BasicType.String:
                        InvokeParameterPool.String[method.Parameters[i].Index] = (string) args[i];
                        break;
                    case BasicType.Object:
                        InvokeParameterPool.Object[method.Parameters[i].Index] = (GorgeObject) args[i];
                        break;
                    default:
                        throw new Exception("不支持该类型");
                }
            }

            // 调用
            InvokeMethod(gorgeObject, method.Id);

            // 取出返回值
            if (method.ReturnType == null)
            {
                return null;
            }

            return method.ReturnType.BasicType switch
            {
                BasicType.Int or BasicType.Enum => InvokeParameterPool.IntReturn,
                BasicType.Float => InvokeParameterPool.FloatReturn,
                BasicType.Bool => InvokeParameterPool.BoolReturn,
                BasicType.String => InvokeParameterPool.StringReturn,
                BasicType.Object => InvokeParameterPool.ObjectReturn,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        /// <summary>
        /// 反射调用指定名字的方法
        /// TODO 暂时不按参数表搜索
        /// 含参数布置和返回值取出过程
        /// </summary>
        /// <param name="gorgeObject"></param>
        /// <param name="methodName"></param>
        /// <param name="argumentTypes"></param>
        /// <param name="genericsTypes"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public object? InvokeMethod(GorgeObject gorgeObject, string methodName, GorgeType[] argumentTypes,
            Dictionary<GorgeType, GorgeType> genericsTypes, object[] args)
        {
            var methods = Declaration.GetMethodByNameAndArgumentTypes(GorgeLanguageRuntime.Instance, methodName,
                argumentTypes, genericsTypes);
            if (methods.Length == 0)
            {
                throw new Exception("没有找到符合参数表的方法");
            }

            if (methods.Length > 1)
            {
                throw new Exception("找到多个符合参数表的方法");
            }

            return InvokeMethod(gorgeObject, methods[0], args);
        }

        public object? InvokeInterfaceMethod(GorgeObject gorgeObject, GorgeType interfaceType, string methodName,
            GorgeType[] argumentTypes, object[] args)
        {
            var interfaceDeclaration = Declaration.SuperInterfaces.FirstOrDefault(i => i.Type.Equals(interfaceType));

            if (interfaceDeclaration == null)
            {
                throw new Exception($"{Declaration.Name}类没有实现{interfaceType.FullName}接口");
            }

            var methods = interfaceDeclaration.GetMethodByNameAndArgumentTypes(GorgeLanguageRuntime.Instance,
                methodName, argumentTypes);

            if (methods.Length == 0)
            {
                throw new Exception("没有找到符合参数表的方法");
            }

            if (methods.Length > 1)
            {
                throw new Exception("找到多个符合参数表的方法");
            }

            var implementationMethodId =
                Declaration.InterfaceMethodImplementationId[interfaceDeclaration.Name][methods[0].Id];

            if (!Declaration.TryGetMethodById(implementationMethodId, out var method))
            {
                throw new Exception($"{Declaration.Name}类没有编号为{implementationMethodId}的方法");
            }

            return InvokeMethod(gorgeObject, method, args);
        }

        /// <summary>
        /// 反射构造对象实例
        /// </summary>
        /// <param name="injector"></param>
        /// <param name="constructorId"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public GorgeObject InvokeConstructor(GorgeObject injector, int constructorId, object[] args)
        {
            // TODO 旧版本GorgeClass遗留代码改，大量复制

            // 类型检查
            var constructor = Declaration.Constructors.FirstOrDefault(c => c.Id == constructorId);
            if (constructor == null) throw new Exception("目标构造方法" + constructorId + "不存在");

            if (args.Length != constructor.Parameters.Length)
                throw new Exception("目标构造方法" + constructorId + "参数数量不正确，应为" + constructor.Parameters.Length + "，实为" +
                                    args.Length);

            for (var i = 0; i < args.Length; i++)
            {
                var argType = args[i].GetType();
                bool typeCheckSuccess;
                switch (constructor.Parameters[i].Type.BasicType)
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
                        typeCheckSuccess = typeof(GorgeObject).IsAssignableFrom(argType);
                        break;
                    default:
                        throw new Exception("不支持该类型");
                }

                if (!typeCheckSuccess)
                {
                    throw new Exception("目标构造方法" + constructorId + "参数" + (i + 1) + "类型不正确，应为" +
                                        constructor.Parameters[i].Type +
                                        "，实为" + args[i].GetType());
                }
            }

            // 参数布置
            for (var i = 0; i < args.Length; i++)
            {
                switch (constructor.Parameters[i].Type.BasicType)
                {
                    case BasicType.Int:
                        InvokeParameterPool.Int[constructor.Parameters[i].Index] = (int) args[i];
                        break;
                    case BasicType.Float:
                        InvokeParameterPool.Float[constructor.Parameters[i].Index] = (float) args[i];
                        break;
                    case BasicType.Bool:
                        InvokeParameterPool.Bool[constructor.Parameters[i].Index] = (bool) args[i];
                        break;
                    case BasicType.Enum:
                        InvokeParameterPool.Int[constructor.Parameters[i].Index] = (int) args[i];
                        break;
                    case BasicType.String:
                        InvokeParameterPool.String[constructor.Parameters[i].Index] = (string) args[i];
                        break;
                    case BasicType.Object:
                        InvokeParameterPool.Object[constructor.Parameters[i].Index] = (GorgeObject) args[i];
                        break;
                    default:
                        throw new Exception("不支持该类型");
                }
            }

            // 调用

            InvokeParameterPool.Injector = (Injector) injector;
            InvokeConstructor(constructorId);

            return InvokeParameterPool.ObjectReturn;
        }

        /// <summary>
        /// 调用静态方法
        /// 含参数布置和返回值取出过程
        /// </summary>
        /// <param name="method"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public object? InvokeStaticMethod(MethodInformation method, params object[] args)
        {
            if (args.Length != method.Parameters.Length)
                throw new Exception("目标方法" + method.Name + "参数数量不正确，应为" + method.Parameters.Length + "，实为" +
                                    args.Length);

            for (var i = 0; i < args.Length; i++)
            {
                var argType = args[i].GetType();
                bool typeCheckSuccess;
                switch (method.Parameters[i].Type.BasicType)
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
                        typeCheckSuccess = typeof(GorgeObject).IsAssignableFrom(argType);
                        break;
                    default:
                        throw new Exception("不支持该类型");
                }

                if (!typeCheckSuccess)
                {
                    throw new Exception("目标方法" + method.Name + "参数" + (i + 1) + "类型不正确，应为" + method.Parameters[i].Type +
                                        "，实为" + args[i].GetType());
                }
            }

            // 参数布置
            for (var i = 0; i < args.Length; i++)
            {
                switch (method.Parameters[i].Type.BasicType)
                {
                    case BasicType.Int:
                    case BasicType.Enum:
                        InvokeParameterPool.Int[method.Parameters[i].Index] = (int) args[i];
                        break;
                    case BasicType.Float:
                        InvokeParameterPool.Float[method.Parameters[i].Index] = (float) args[i];
                        break;
                    case BasicType.Bool:
                        InvokeParameterPool.Bool[method.Parameters[i].Index] = (bool) args[i];
                        break;
                    case BasicType.String:
                        InvokeParameterPool.String[method.Parameters[i].Index] = (string) args[i];
                        break;
                    case BasicType.Object:
                        InvokeParameterPool.Object[method.Parameters[i].Index] = (GorgeObject) args[i];
                        break;
                    default:
                        throw new Exception("不支持该类型");
                }
            }

            // 调用
            InvokeStaticMethod(method.Id);

            // 取出返回值
            if (method.ReturnType == null)
            {
                return null;
            }

            return method.ReturnType.BasicType switch
            {
                BasicType.Int or BasicType.Enum => InvokeParameterPool.IntReturn,
                BasicType.Float => InvokeParameterPool.FloatReturn,
                BasicType.Bool => InvokeParameterPool.BoolReturn,
                BasicType.String => InvokeParameterPool.StringReturn,
                BasicType.Object => InvokeParameterPool.ObjectReturn,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        #endregion

        #region 反射字段操作

        public int GetInjectorInt(Injector injector, string fieldName)
        {
            if (Declaration.TryGetInjectorFieldByName(fieldName, out var field))
            {
                if (field.Type.BasicType != BasicType.Int)
                {
                    throw new Exception($"{Declaration.Name}类的{fieldName}字段类型不为int，实为{field.Type}");
                }

                return injector.GetInjectorInt(field.Index);
            }

            throw new Exception($"{Declaration.Name}类无名为{fieldName}的字段");
        }

        public float GetInjectorFloat(Injector injector, string fieldName)
        {
            if (Declaration.TryGetInjectorFieldByName(fieldName, out var field))
            {
                if (field.Type.BasicType != BasicType.Float)
                {
                    throw new Exception($"{Declaration.Name}类的{fieldName}字段类型不为float，实为{field.Type}");
                }

                return injector.GetInjectorFloat(field.Index);
            }

            throw new Exception($"{Declaration.Name}类无名为{fieldName}的字段");
        }

        public bool GetInjectorBool(Injector injector, string fieldName)
        {
            if (Declaration.TryGetInjectorFieldByName(fieldName, out var field))
            {
                if (field.Type.BasicType != BasicType.Bool)
                {
                    throw new Exception($"{Declaration.Name}类的{fieldName}字段类型不为bool，实为{field.Type}");
                }

                return injector.GetInjectorBool(field.Index);
            }

            throw new Exception($"{Declaration.Name}类无名为{fieldName}的字段");
        }

        public string GetInjectorString(Injector injector, string fieldName)
        {
            if (Declaration.TryGetInjectorFieldByName(fieldName, out var field))
            {
                if (field.Type.BasicType != BasicType.String)
                {
                    throw new Exception($"{Declaration.Name}类的{fieldName}字段类型不为string，实为{field.Type}");
                }

                return injector.GetInjectorString(field.Index);
            }

            throw new Exception($"{Declaration.Name}类无名为{fieldName}的字段");
        }

        public GorgeObject GetInjectorObject(Injector injector, string fieldName)
        {
            if (Declaration.TryGetInjectorFieldByName(fieldName, out var field))
            {
                if (field.Type.BasicType != BasicType.Object)
                {
                    throw new Exception($"{Declaration.Name}类的{fieldName}字段类型不为object，实为{field.Type}");
                }

                return injector.GetInjectorObject(field.Index);
            }

            throw new Exception($"{Declaration.Name}类无名为{fieldName}的字段");
        }

        public void SetInjectorInt(Injector injector, string fieldName, int value)
        {
            if (Declaration.TryGetInjectorFieldByName(fieldName, out var field))
            {
                if (field.Type.BasicType != BasicType.Int && field.Type.BasicType != BasicType.Enum)
                {
                    throw new Exception($"{Declaration.Name}类的{fieldName}字段类型不为int，实为{field.Type}");
                }

                injector.SetInjectorInt(field.Index, value);
                return;
            }

            throw new Exception($"{Declaration.Name}类无名为{fieldName}的字段");
        }

        public void SetInjectorFloat(Injector injector, string fieldName, float value)
        {
            if (Declaration.TryGetInjectorFieldByName(fieldName, out var field))
            {
                if (field.Type.BasicType != BasicType.Float)
                {
                    throw new Exception($"{Declaration.Name}类的{fieldName}字段类型不为float，实为{field.Type}");
                }

                injector.SetInjectorFloat(field.Index, value);
                return;
            }

            throw new Exception($"{Declaration.Name}类无名为{fieldName}的字段");
        }

        public void SetInjectorBool(Injector injector, string fieldName, bool value)
        {
            if (Declaration.TryGetInjectorFieldByName(fieldName, out var field))
            {
                if (field.Type.BasicType != BasicType.Bool)
                {
                    throw new Exception($"{Declaration.Name}类的{fieldName}字段类型不为bool，实为{field.Type}");
                }

                injector.SetInjectorBool(field.Index, value);
                return;
            }

            throw new Exception($"{Declaration.Name}类无名为{fieldName}的字段");
        }

        public void SetInjectorString(Injector injector, string fieldName, string value)
        {
            if (Declaration.TryGetInjectorFieldByName(fieldName, out var field))
            {
                if (field.Type.BasicType != BasicType.String)
                {
                    throw new Exception($"{Declaration.Name}类的{fieldName}字段类型不为string，实为{field.Type}");
                }

                injector.SetInjectorString(field.Index, value);
                return;
            }

            throw new Exception($"{Declaration.Name}类无名为{fieldName}的字段");
        }

        public void SetInjectorObject(Injector injector, string fieldName, GorgeObject value)
        {
            if (Declaration.TryGetInjectorFieldByName(fieldName, out var field))
            {
                if (field.Type.BasicType != BasicType.Object)
                {
                    throw new Exception($"{Declaration.Name}类的{fieldName}字段类型不为object，实为{field.Type}");
                }

                injector.SetInjectorObject(field.Index, value);
                return;
            }

            throw new Exception($"{Declaration.Name}类无名为{fieldName}的字段");
        }

        #endregion

        #region 反射注入器字段信息

        /// <summary>
        /// 根据名字反射构造器字段信息，会按继承顺序反向搜索超类
        /// </summary>
        /// <param name="fieldId">待查找注入器字段ID</param>
        /// <param name="injectorField">查找出的注入器字段</param>
        /// <param name="declaringClass">查找出的注入器字段所在类</param>
        /// <returns>是否查找成功</returns>
        public bool TryGetInjectorFieldById(int fieldId,
            [NotNullWhen(true)] out InjectorFieldInformation? injectorField,
            [NotNullWhen(true)] out GorgeClass? declaringClass)
        {
            // TODO 暂时使用即时搜索，可以改为预先建立索引，索引关系在构造时即确定
            injectorField = Declaration.InjectorFields.FirstOrDefault(f => f.Id == fieldId);
            if (injectorField != null)
            {
                declaringClass = this;
                return true;
            }

            declaringClass = null;
            return SuperClass != null &&
                   SuperClass.TryGetInjectorFieldById(fieldId, out injectorField, out declaringClass);
        }

        #endregion
    }
}