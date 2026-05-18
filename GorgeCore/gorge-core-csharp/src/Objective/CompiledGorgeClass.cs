using System;
using System.Collections.Generic;
using System.Linq;
using Gorge.GorgeLanguage.VirtualMachine;
using Gorge.Native.Gorge;

namespace Gorge.GorgeLanguage.Objective
{
    public class CompiledGorgeClass : GorgeClass
    {
        public CompiledGorgeClass(ClassDeclaration classDeclaration,
            List<CompiledMethodImplementation> methodImplementations,
            List<CompiledMethodImplementation> staticMethodImplementations,
            List<CompiledConstructorImplementation> constructorImplementations,
            List<CompiledFieldInitializerImplementation> fieldInitializerImplementations,
            GorgeDelegateImplementation[] delegateImplementation)
        {
            Declaration = @classDeclaration;
            MethodImplementations = methodImplementations;
            StaticMethodImplementations = staticMethodImplementations;
            ConstructorImplementations = constructorImplementations;
            FieldInitializerImplementations = fieldInitializerImplementations;
            DelegateImplementation = delegateImplementation;

            #region 根据Inject注解整合Injector默认值

            _injectorDefaultValues =
                new FixedFieldValuePool(classDeclaration.GetInjectorFieldDefaultValueStorageTypeCount());
            foreach (var field in classDeclaration.Fields)
            {
                foreach (var injectAnnotation in field.GetAnnotations("Inject"))
                {
                    string injectorFieldName;
                    if (injectAnnotation.TryGetParameter("name", out var name))
                    {
                        injectorFieldName = (string) name;
                    }
                    else
                    {
                        injectorFieldName = field.Name;
                    }

                    if (!classDeclaration.TryGetInjectorFieldByName(injectorFieldName, out var injectorField))
                    {
                        throw new Exception($"没有名为{injectorFieldName}的Injector字段");
                    }

                    if (injectorField.DefaultValueIndex == null) // 无默认值情况
                    {
                        continue;
                    }

                    if (!injectAnnotation.TryGetMetadata("defaultValue", out var defaultValueMetadata))
                    {
                        throw new Exception($"Injector字段{injectorFieldName}应当有默认值，但实际没有");
                    }

                    var defaultValue = defaultValueMetadata.Value;
                    var defaultValueIndex = injectorField.DefaultValueIndex.Value;
                    // 本级起始点
                    var startTypeCount = classDeclaration.GetInjectorFieldDefaultValueStartTypeCount();

                    switch (injectorField.Type.BasicType)
                    {
                        case BasicType.Enum:
                        case BasicType.Int:
                            _injectorDefaultValues.Int[defaultValueIndex - startTypeCount.Int] = (int) defaultValue;
                            break;
                        case BasicType.Float:
                            _injectorDefaultValues.Float[defaultValueIndex - startTypeCount.Float] =
                                defaultValue is int i ? i : (float) defaultValue;
                            break;
                        case BasicType.Bool:
                            _injectorDefaultValues.Bool[defaultValueIndex - startTypeCount.Bool] =
                                (bool) defaultValue;
                            break;
                        case BasicType.String:
                            _injectorDefaultValues.String[defaultValueIndex - startTypeCount.String] =
                                (string) defaultValue;
                            break;
                        case BasicType.Object:
                            _injectorDefaultValues.Object[defaultValueIndex - startTypeCount.Object] =
                                (GorgeObject) defaultValue;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            #endregion

            #region 将对应注解拷贝到Injector字段中

            foreach (var field in classDeclaration.Fields)
            {
                foreach (var injectAnnotation in field.GetAnnotations("Inject"))
                {
                    string injectorFieldName;
                    if (injectAnnotation.TryGetParameter("name", out var name))
                    {
                        injectorFieldName = (string) name;
                    }
                    else
                    {
                        injectorFieldName = field.Name;
                    }

                    if (!classDeclaration.TryGetInjectorFieldByName(injectorFieldName, out var injectorField))
                    {
                        throw new Exception($"没有名为{injectorFieldName}的Injector字段");
                    }

                    // 向injectorField插入该注解的元数据
                    injectorField.AppendMetadata(injectAnnotation.Metadatas);

                    if (injectorField.DefaultValueIndex == null) // 无默认值情况
                    {
                        continue;
                    }

                    if (!injectAnnotation.TryGetMetadata("defaultValue", out var defaultValueMetadata))
                    {
                        throw new Exception($"Injector字段{injectorFieldName}应当有默认值，但实际没有");
                    }

                    var defaultValue = defaultValueMetadata.Value;
                    var defaultValueIndex = injectorField.DefaultValueIndex.Value;
                    // 本级起始点
                    var startTypeCount = classDeclaration.GetInjectorFieldDefaultValueStartTypeCount();

                    switch (injectorField.Type.BasicType)
                    {
                        case BasicType.Enum:
                        case BasicType.Int:
                            _injectorDefaultValues.Int[defaultValueIndex - startTypeCount.Int] = (int) defaultValue;
                            break;
                        case BasicType.Float:
                            _injectorDefaultValues.Float[defaultValueIndex - startTypeCount.Float] =
                                defaultValue is int i ? i : (float) defaultValue;
                            break;
                        case BasicType.Bool:
                            _injectorDefaultValues.Bool[defaultValueIndex - startTypeCount.Bool] =
                                (bool) defaultValue;
                            break;
                        case BasicType.String:
                            _injectorDefaultValues.String[defaultValueIndex - startTypeCount.String] =
                                (string) defaultValue;
                            break;
                        case BasicType.Object:
                            _injectorDefaultValues.Object[defaultValueIndex - startTypeCount.Object] =
                                (GorgeObject) defaultValue;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            #endregion
        }

        /// <summary>
        /// 反序列化用构造器，接受预构建的 FixedFieldValuePool，跳过基于 [Inject] 注解的默认值提取。
        /// </summary>
        internal CompiledGorgeClass(
            ClassDeclaration classDeclaration,
            List<CompiledMethodImplementation> methodImplementations,
            List<CompiledMethodImplementation> staticMethodImplementations,
            List<CompiledConstructorImplementation> constructorImplementations,
            List<CompiledFieldInitializerImplementation> fieldInitializerImplementations,
            GorgeDelegateImplementation[] delegateImplementation,
            FixedFieldValuePool injectorDefaultValues)
        {
            Declaration = classDeclaration;
            MethodImplementations = methodImplementations;
            StaticMethodImplementations = staticMethodImplementations;
            ConstructorImplementations = constructorImplementations;
            FieldInitializerImplementations = fieldInitializerImplementations;
            DelegateImplementation = delegateImplementation;
            _injectorDefaultValues = injectorDefaultValues;
        }

        public override Injector EmptyInjector() => new CompiledInjector(Declaration);

        public override ClassDeclaration Declaration { get; }

        public override GorgeClass SuperClass
        {
            get
            {
                // TODO 可能可以缓存或者在编译完成后手动组装
                if (Declaration.SuperClass == null)
                {
                    return null;
                }

                return GorgeLanguageRuntime.Instance.GetClass(Declaration.SuperClass.Name);
            }
        }

        public override GorgeClass LatestNativeClass => SuperClass?.LatestNativeClass;

        public readonly List<CompiledMethodImplementation> MethodImplementations;
        public readonly List<CompiledMethodImplementation> StaticMethodImplementations;

        public readonly List<CompiledConstructorImplementation> ConstructorImplementations;
        public readonly List<CompiledFieldInitializerImplementation> FieldInitializerImplementations;

        public readonly GorgeDelegateImplementation[] DelegateImplementation;

        private readonly FixedFieldValuePool _injectorDefaultValues;

        /// <summary>
        /// 在目标对象上调用目标方法
        /// </summary>
        /// <param name="gorgeObject"></param>
        /// <param name="methodId"></param>
        public override void InvokeMethod(GorgeObject gorgeObject, int methodId)
        {
            var realMethodId = methodId;
            // 判断方法ID是否属于本层
            if (methodId < Declaration.MethodStartId || methodId >= Declaration.MethodCount)
            {
                // 本类未重写情况，从超类调用
                if (!Declaration.MethodOverrideId.TryGetValue(methodId, out realMethodId))
                {
                    if (Declaration.SuperClass == null)
                    {
                        throw new Exception($"类{Declaration.Name}没有编号为{methodId}的方法");
                    }

                    // 从超类调用
                    var superClass = SuperClass;
                    if (superClass == null)
                    {
                        throw new Exception($"类{Declaration.Name}的超类不存在或不完整");
                    }

                    superClass.InvokeMethod(gorgeObject, methodId);
                    return;
                }
            }

            // TODO 暂时使用即时搜索，可以预先存表
            var targetMethod = MethodImplementations.First(m => m.Declaration.Id == realMethodId);
            GorgeLanguageRuntime.Instance.Vm.InvokeMethod(targetMethod, gorgeObject);
        }

        /// <summary>
        /// 调用目标静态方法
        /// </summary>
        /// <param name="staticMethodId"></param>
        public override void InvokeStaticMethod(int staticMethodId)
        {
            var realMethodId = staticMethodId;
            // 判断方法ID是否属于本层
            if (staticMethodId < Declaration.StaticMethodStartId || staticMethodId >= Declaration.StaticMethodCount)
            {
                // 不属于本层则从超类调用
                if (Declaration.SuperClass == null)
                {
                    throw new Exception($"类{Declaration.Name}没有编号为{staticMethodId}的方法");
                }

                // 从超类调用
                var superClass = SuperClass;
                if (superClass == null)
                {
                    throw new Exception($"类{Declaration.Name}的超类不存在或不完整");
                }

                superClass.InvokeStaticMethod(staticMethodId);
                return;
            }

            // TODO 暂时使用即时搜索，可以预先存表
            var targetMethod = StaticMethodImplementations.First(m => m.Declaration.Id == realMethodId);
            GorgeLanguageRuntime.Instance.Vm.InvokeMethod(targetMethod, null);
        }

        protected override GorgeObject DoConstruct(GorgeObject targetObject, int constructorId)
        {
            var targetConstructor = ConstructorImplementations.First(c => c.Declaration.Id == constructorId);
            if (targetObject == null) // 如果从本层开始构造，则创建对象框架
            {
                targetObject = new CompiledGorgeObject(this);
            }
            
            // 存储调用参数（值类型用 stackalloc 避免堆分配，引用类型保留托管数组）
            const int poolLen = InvokeParameterPool.PoolSize;
            Span<int> savedInvokeInt = stackalloc int[poolLen];
            Span<float> savedInvokeFloat = stackalloc float[poolLen];
            Span<bool> savedInvokeBool = stackalloc bool[poolLen];
            var savedInvokeString = new string[InvokeParameterPool.String.Length];
            var savedInvokeObject = new GorgeObject[InvokeParameterPool.Object.Length];
            InvokeParameterPool.Int.AsSpan().CopyTo(savedInvokeInt);
            InvokeParameterPool.Float.AsSpan().CopyTo(savedInvokeFloat);
            InvokeParameterPool.Bool.AsSpan().CopyTo(savedInvokeBool);
            InvokeParameterPool.String.CopyTo(savedInvokeString, 0);
            InvokeParameterPool.Object.CopyTo(savedInvokeObject, 0);

            // 本类字段初始化
            FieldInitialize(targetObject);

            // 还原存储的调用参数（值类型拷贝回池数组，引用类型替换引用）
            savedInvokeInt.CopyTo(InvokeParameterPool.Int);
            savedInvokeFloat.CopyTo(InvokeParameterPool.Float);
            savedInvokeBool.CopyTo(InvokeParameterPool.Bool);
            InvokeParameterPool.String = savedInvokeString;
            InvokeParameterPool.Object = savedInvokeObject;

            // 本类构造逻辑，其中调用父类构造逻辑
            //   卸载调用参数
            //   准备父类构造方法调用参数
            //   调用父类构造方法
            //   调用本类构造方法
            GorgeLanguageRuntime.Instance.Vm.InvokeMethod(targetConstructor, targetObject);

            // 设置返回
            return targetObject;
        }

        public override int GetInjectorIntDefaultValue(int defaultValueIndex)
        {
            var storageIndex = defaultValueIndex - Declaration.GetInjectorFieldDefaultValueStartTypeCount().Int;
            if (storageIndex < 0)
            {
                return SuperClass.GetInjectorIntDefaultValue(defaultValueIndex);
            }

            return _injectorDefaultValues.Int[storageIndex];
        }

        public override float GetInjectorFloatDefaultValue(int defaultValueIndex)
        {
            var storageIndex = defaultValueIndex - Declaration.GetInjectorFieldDefaultValueStartTypeCount().Float;
            if (storageIndex < 0)
            {
                return SuperClass.GetInjectorFloatDefaultValue(defaultValueIndex);
            }

            return _injectorDefaultValues.Float[storageIndex];
        }

        public override bool GetInjectorBoolDefaultValue(int defaultValueIndex)
        {
            var storageIndex = defaultValueIndex - Declaration.GetInjectorFieldDefaultValueStartTypeCount().Bool;
            if (storageIndex < 0)
            {
                return SuperClass.GetInjectorBoolDefaultValue(defaultValueIndex);
            }

            return _injectorDefaultValues.Bool[storageIndex];
        }

        public override string GetInjectorStringDefaultValue(int defaultValueIndex)
        {
            var storageIndex = defaultValueIndex - Declaration.GetInjectorFieldDefaultValueStartTypeCount().String;
            if (storageIndex < 0)
            {
                return SuperClass.GetInjectorStringDefaultValue(defaultValueIndex);
            }

            return _injectorDefaultValues.String[storageIndex];
        }

        public override GorgeObject GetInjectorObjectDefaultValue(int defaultValueIndex)
        {
            var storageIndex = defaultValueIndex - Declaration.GetInjectorFieldDefaultValueStartTypeCount().Object;
            if (storageIndex < 0)
            {
                return SuperClass.GetInjectorObjectDefaultValue(defaultValueIndex);
            }

            return _injectorDefaultValues.Object[storageIndex];
        }

        public void FieldInitialize(GorgeObject targetObject)
        {
            // 按顺序初始化所有字段
            foreach (var fieldInitializer in FieldInitializerImplementations)
            {
                GorgeLanguageRuntime.Instance.Vm.InvokeMethod(fieldInitializer, targetObject);
            }
        }
    }
}