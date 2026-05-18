using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Gorge.GorgeLanguage.Objective
{
    /// <summary>
    /// Gorge类定义
    /// </summary>
    public class ClassDeclaration
    {
        // TODO 是否需要有ID？不管用不用，感觉应当有
        // TODO 这个ID应该是链接时确定的，如果没有链接，那就要保持带namespace的完整名字作为类索引

        /// <summary>
        /// 类名
        /// </summary>
        public GorgeType Type { get; }

        public string Name => Type.FullName;

        /// <summary>
        /// 超类
        /// </summary>
        [AllowNull]
        public ClassDeclaration SuperClass { get; }

        /// <summary>
        /// 实现接口
        /// </summary>
        public GorgeInterface[] SuperInterfaces { get; }

        /// <summary>
        /// 是否为Native类
        /// </summary>
        public bool IsNative { get; }

        /// <summary>
        /// 字段表
        /// </summary>
        public FieldInformation[] Fields { get; }

        /// <summary>
        /// 方法表
        /// </summary>
        public MethodInformation[] Methods { get; }

        /// <summary>
        /// 静态方法表
        /// </summary>
        public MethodInformation[] StaticMethods { get; }

        /// <summary>
        /// 构造方法表
        /// </summary>
        public ConstructorInformation[] Constructors { get; }

        /// <summary>
        /// 注入器构造方法表
        /// </summary>
        public ConstructorInformation[] InjectorConstructors { get; }

        /// <summary>
        /// 注入器字段表
        /// </summary>
        public InjectorFieldInformation[] InjectorFields { get; }

        /// <summary>
        /// 实例字段计数，用于分配字段实例索引
        /// 当构造完成后，该值代表该类用到的各类实例字段数量（含超类）
        /// </summary>
        public readonly TypeCount ObjectTypeCount;

        /// <summary>
        /// 注入器字段计数，用于分配字段注入器索引
        /// 当构造完成后，该值代表该类用到的各类注入器字段数量（含超类）
        /// </summary>
        public readonly TypeCount InjectorFieldTypeCount;

        /// <summary>
        /// 注入器字段默认值技术，用于分配默认值索引
        /// 当构造完成后，该值代表该类用到的各类注入器字段默认值数量（含超类）
        /// </summary>
        public readonly TypeCount InjectorFieldDefaultValueTypeCount;

        /// <summary>
        /// 方法计数，用于分配方法ID
        /// 当构造完成后，该值代表该类用到的全部方法数量（含超类）
        /// </summary>
        public int MethodCount { get; }

        /// <summary>
        /// 静态方法计数，用于分配方法ID
        /// 当构造完成后，该值代表该类用到的全部方法数量（含超类）
        /// </summary>
        public int StaticMethodCount { get; }

        /// <summary>
        /// 本级方法起始ID
        /// </summary>
        public int MethodStartId { get; }

        /// <summary>
        /// 本级静态方法起始ID
        /// </summary>
        public int StaticMethodStartId { get; }

        /// <summary>
        /// 构造方法计数，用于分配构造方法ID
        /// 当构造完成后，该值代表该类用到的全部构造方法数量（含超类）
        /// </summary>
        public int ConstructorCount { get; }

        /// <summary>
        /// 本级构造方法起始ID
        /// </summary>
        public int ConstructorStartId { get; }

        /// <summary>
        /// 注入器构造方法计数，用于分配注入器构造方法ID
        /// 当构造完成后，该值代表该类用到的全部注入器构造方法数量（含超类）
        /// </summary>
        public int InjectorConstructorCount { get; }

        /// <summary>
        /// 字段计数，用于分配字段ID
        /// 当构造完成后，该值代表该类用到的全部字段数量（含超类）
        /// </summary>
        public int InjectorFieldCount { get; }

        /// <summary>
        /// 接口方法的实现的方法ID
        /// 索引是接口名
        /// 值是接口方法对应的类方法ID
        /// </summary>
        public readonly Dictionary<string, int[]> InterfaceMethodImplementationId;

        /// <summary>
        /// 注入器构造方法实现的构造方法ID
        /// 索引是注入器构造方法ID
        /// 值时实现的构造方法ID
        /// </summary>
        public readonly int[] InjectorConstructorImplementationId;

        /// <summary>
        /// 重写方法的方法ID
        /// 索引是被重写方法ID
        /// 值是重写方法ID
        /// </summary>
        public readonly Dictionary<int, int> MethodOverrideId;

        /// <summary>
        /// 注解
        /// </summary>
        public Annotation[] Annotations { get; }

        /// <summary>
        /// 编译用构造
        /// </summary>
        /// <param name="type"></param>
        /// <param name="isNative"></param>
        /// <param name="superClass"></param>
        /// <param name="superInterfaces"></param>
        /// <param name="fields"></param>
        /// <param name="methods"></param>
        /// <param name="staticMethods"></param>
        /// <param name="constructors"></param>
        /// <param name="injectorConstructors"></param>
        /// <param name="injectorFields">单独声明的注入器字段，不含字段注解派生的</param>
        /// <param name="annotations"></param>
        /// <param name="fieldIndexCount"></param>
        /// <param name="methodCount"></param>
        /// <param name="methodOverrideId"></param>
        /// <param name="interfaceMethodImplementationId"></param>
        /// <param name="staticMethodCount"></param>
        /// <param name="constructorCount"></param>
        /// <param name="injectorConstructorCount"></param>
        /// <param name="injectorConstructorImplementationId"></param>
        /// <param name="injectorFieldIndexCount"></param>
        /// <param name="injectorFieldDefaultValueIndexCount"></param>
        /// <param name="injectorFieldCount"></param>
        public ClassDeclaration(GorgeType type, bool isNative, [AllowNull] ClassDeclaration superClass,
            GorgeInterface[] superInterfaces, FieldInformation[] fields, MethodInformation[] methods,
            MethodInformation[] staticMethods, ConstructorInformation[] constructors,
            ConstructorInformation[] injectorConstructors, InjectorFieldInformation[] injectorFields,
            Annotation[] annotations, TypeCount fieldIndexCount, int methodCount, Dictionary<int, int> methodOverrideId,
            Dictionary<string, int[]> interfaceMethodImplementationId, int staticMethodCount, int constructorCount,
            int injectorConstructorCount, int[] injectorConstructorImplementationId, TypeCount injectorFieldIndexCount,
            TypeCount injectorFieldDefaultValueIndexCount, int injectorFieldCount)
        {
            Type = type;
            IsNative = isNative;
            SuperClass = superClass;
            SuperInterfaces = superInterfaces;
            Annotations = annotations;
            MethodStartId = SuperClass?.MethodCount ?? 0;
            StaticMethodStartId = SuperClass?.StaticMethodCount ?? 0;
            ConstructorStartId = SuperClass?.ConstructorCount ?? 0;
            Fields = fields;
            ObjectTypeCount = fieldIndexCount;
            Methods = methods;
            MethodOverrideId = methodOverrideId;
            MethodCount = methodCount;
            InterfaceMethodImplementationId = interfaceMethodImplementationId;
            StaticMethods = staticMethods;
            StaticMethodCount = staticMethodCount;
            Constructors = constructors;
            ConstructorCount = constructorCount;
            InjectorConstructors = injectorConstructors;
            InjectorConstructorCount = injectorConstructorCount;
            InjectorConstructorImplementationId = injectorConstructorImplementationId;
            InjectorFields = injectorFields;
            InjectorFieldTypeCount = injectorFieldIndexCount;
            InjectorFieldDefaultValueTypeCount = injectorFieldDefaultValueIndexCount;
            InjectorFieldCount = injectorFieldCount;

            // 构造反向索引
            _fieldNameDictionary = new Dictionary<string, FieldInformation>();
            // 收集本类的反向索引
            foreach (var field in Fields)
            {
                _fieldNameDictionary.Add(field.Name, field);
            }

            // 合并超类的反向索引
            if (SuperClass != null)
            {
                // 同名屏蔽
                foreach (var (fieldName, field) in SuperClass._fieldNameDictionary)
                {
                    _fieldNameDictionary.TryAdd(fieldName, field);
                }
            }

            _injectorFieldNameDictionary = new Dictionary<string, InjectorFieldInformation>();
            // 收集本类的反向索引
            foreach (var injectorField in InjectorFields)
            {
                _injectorFieldNameDictionary.Add(injectorField.Name, injectorField);
            }

            // 合并超类的反向索引
            if (SuperClass != null)
            {
                // 同名屏蔽
                foreach (var (fieldName, field) in SuperClass._injectorFieldNameDictionary)
                {
                    _injectorFieldNameDictionary.TryAdd(fieldName, field);
                }
            }
        }

        #region 成员定义获取

        /// <summary>
        /// 根据ID获取构造器
        /// </summary>
        /// <param name="constructorId"></param>
        /// <param name="constructor"></param>
        /// <returns></returns>
        public bool TryGetConstructorById(int constructorId, out ConstructorInformation constructor)
        {
            var index = constructorId - ConstructorStartId;
            if (index >= Constructors.Length || index < 0)
            {
                constructor = default;
                return false;
            }

            constructor = Constructors[index];
            return true;
        }

        #endregion

        #region 泛型

        /// <summary>
        /// 获取泛型参数表
        /// </summary>
        /// <param name="instanceType">实例类型</param>
        /// <returns></returns>
        public Dictionary<GorgeType, GorgeType> GenericsArguments(GorgeType instanceType)
        {
            var result = new Dictionary<GorgeType, GorgeType>();

            for (var i = 0; i < Type.SubTypes.Length; i++)
            {
                if (Type.SubTypes[i].IsGenerics)
                {
                    result.Add(Type.SubTypes[i], instanceType.SubTypes[i]);
                }
            }

            return result;
        }

        public Dictionary<string, GorgeType> GenericsParameters()
        {
            var result = new Dictionary<string, GorgeType>();

            foreach (var type in Type.SubTypes)
            {
                if (type.IsGenerics)
                {
                    result.Add(type.FullName, type);
                }
            }

            return result;
        }

        #endregion

        #region 反射

        #region 反射索引

        private readonly Dictionary<string, FieldInformation> _fieldNameDictionary;

        private readonly Dictionary<string, InjectorFieldInformation> _injectorFieldNameDictionary;

        #endregion

        /// <summary>
        /// 根据名字反射字段信息，会按继承顺序反向搜索超类
        /// </summary>
        /// <returns></returns>
        public bool TryGetFieldByName(string fieldName, out FieldInformation field)
        {
            return _fieldNameDictionary.TryGetValue(fieldName, out field);
        }

        /// <summary>
        /// 根据名字反射构造器字段信息，会按继承顺序反向搜索超类
        /// </summary>
        /// <returns></returns>
        public bool TryGetInjectorFieldByName(string fieldName, out InjectorFieldInformation field)
        {
            return _injectorFieldNameDictionary.TryGetValue(fieldName, out field);
        }

        /// <summary>
        /// 根据名字反射构造器字段信息，会按继承顺序反向搜索超类
        /// </summary>
        /// <returns></returns>
        public bool TryGetInjectorFieldById(int fieldId, out InjectorFieldInformation field)
        {
            // TODO 暂时使用即时搜索，可以改为预先建立索引，索引关系在构造时即确定
            field = InjectorFields.FirstOrDefault(f => f.Id == fieldId);
            if (field != null)
            {
                return true;
            }

            return SuperClass != null && SuperClass.TryGetInjectorFieldById(fieldId, out field);
        }

        /// <summary>
        /// 根据Index反射构造器int字段信息，会按继承顺序反向搜索超类
        /// </summary>
        /// <returns></returns>
        public bool TryGetInjectorIntFieldByIndex(int index, out InjectorFieldInformation field)
        {
            // TODO 暂时使用即时搜索，可以改为预先建立索引，索引关系在构造时即确定
            field = InjectorFields.FirstOrDefault(f =>
                f.Index == index && f.Type.BasicType is BasicType.Int or BasicType.Enum);
            if (field != null)
            {
                return true;
            }

            return SuperClass != null && SuperClass.TryGetInjectorIntFieldByIndex(index, out field);
        }

        /// <summary>
        /// 根据Index反射构造器float字段信息，会按继承顺序反向搜索超类
        /// </summary>
        /// <returns></returns>
        public bool TryGetInjectorFloatFieldByIndex(int index, out InjectorFieldInformation field)
        {
            // TODO 暂时使用即时搜索，可以改为预先建立索引，索引关系在构造时即确定
            field = InjectorFields.FirstOrDefault(f =>
                f.Index == index && f.Type.BasicType is BasicType.Float);
            if (field != null)
            {
                return true;
            }

            return SuperClass != null && SuperClass.TryGetInjectorFloatFieldByIndex(index, out field);
        }

        /// <summary>
        /// 根据Index反射构造器float字段信息，会按继承顺序反向搜索超类
        /// </summary>
        /// <returns></returns>
        public bool TryGetInjectorBoolFieldByIndex(int index, out InjectorFieldInformation field)
        {
            // TODO 暂时使用即时搜索，可以改为预先建立索引，索引关系在构造时即确定
            field = InjectorFields.FirstOrDefault(f =>
                f.Index == index && f.Type.BasicType is BasicType.Bool);
            if (field != null)
            {
                return true;
            }

            return SuperClass != null && SuperClass.TryGetInjectorBoolFieldByIndex(index, out field);
        }

        /// <summary>
        /// 根据Index反射构造器float字段信息，会按继承顺序反向搜索超类
        /// </summary>
        /// <returns></returns>
        public bool TryGetInjectorStringFieldByIndex(int index, out InjectorFieldInformation field)
        {
            // TODO 暂时使用即时搜索，可以改为预先建立索引，索引关系在构造时即确定
            field = InjectorFields.FirstOrDefault(f =>
                f.Index == index && f.Type.BasicType is BasicType.String);
            if (field != null)
            {
                return true;
            }

            return SuperClass != null && SuperClass.TryGetInjectorStringFieldByIndex(index, out field);
        }

        /// <summary>
        /// 根据Index反射构造器float字段信息，会按继承顺序反向搜索超类
        /// </summary>
        /// <returns></returns>
        public bool TryGetInjectorObjectFieldByIndex(int index, out InjectorFieldInformation field)
        {
            // TODO 暂时使用即时搜索，可以改为预先建立索引，索引关系在构造时即确定
            field = InjectorFields.FirstOrDefault(f =>
                f.Index == index && f.Type.BasicType is BasicType.Object);
            if (field != null)
            {
                return true;
            }

            return SuperClass != null && SuperClass.TryGetInjectorObjectFieldByIndex(index, out field);
        }

        /// <summary>
        /// 检查是否有目标名字的方法
        /// </summary>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public bool ContainsMethodWithName(string methodName)
        {
            if (Methods.Any(m => m.Name == methodName))
            {
                return true;
            }

            return SuperClass != null && SuperClass.ContainsMethodWithName(methodName);
        }

        /// <summary>
        /// 检查是否有目标名字的静态方法
        /// </summary>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public bool ContainsStaticMethodWithName(string methodName)
        {
            if (StaticMethods.Any(m => m.Name == methodName))
            {
                return true;
            }

            return SuperClass != null && SuperClass.ContainsStaticMethodWithName(methodName);
        }

        public bool TryGetMethodBySignature(string name, GorgeType[] parameterTypes, out MethodInformation method)
        {
            foreach (var m in Methods)
            {
                if (m.Name == name && m.Parameters.Select(p => p.Type).SequenceEqual(parameterTypes))
                {
                    method = m;
                    return true;
                }
            }

            method = default;
            return false;
        }

        public bool TryGetStaticMethodBySignature(string name, GorgeType[] parameterTypes, out MethodInformation method)
        {
            foreach (var m in StaticMethods)
            {
                if (m.Name == name && m.Parameters.Select(p => p.Type).SequenceEqual(parameterTypes))
                {
                    method = m;
                    return true;
                }
            }

            method = default;
            return false;
        }

        /// <summary>
        /// 按参数表检索可调用的方法
        /// 先搜本类模糊，再搜超类精确
        /// TODO 和静态方法检索完全一致，和构造方法检索部分一致，考虑合并
        /// </summary>
        /// <param name="typeDeclarationContext"></param>
        /// <param name="name"></param>
        /// <param name="argumentTypes"></param>
        /// <param name="genericsTypes"></param>
        /// <returns></returns>
        public MethodInformation[] GetMethodByNameAndArgumentTypes(GorgeLanguageRuntime typeDeclarationContext,
            string name, GorgeType[] argumentTypes, Dictionary<GorgeType, GorgeType> genericsTypes)
        {
            // 符合调用参数的重载
            var selectedConstructors = new List<MethodInformation>();

            foreach (var m in Methods)
            {
                var parameters = m.Parameters;
                if (m.Name != name || argumentTypes.Length != parameters.Length)
                {
                    continue;
                }

                // 记录参数表是否完全相同
                var completelyEqual = true;
                // 记录参数表是否完全可转换
                var completelyCastable = true;

                for (var i = 0; i < argumentTypes.Length; i++)
                {
                    if (argumentTypes[i].Equals(parameters[i].Type))
                    {
                        continue;
                    }

                    // 如果对位参数类型不同，关闭标记
                    completelyEqual = false;

                    if (parameters[i].Type.IsGenerics)
                    {
                        if (typeDeclarationContext.CanAutoCastTo(argumentTypes[i], genericsTypes[parameters[i].Type]))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (typeDeclarationContext.CanAutoCastTo(argumentTypes[i], parameters[i].Type))
                        {
                            continue;
                        }
                    }

                    // 如果对位参数不可转换，关闭标记
                    completelyCastable = false;
                    break;
                }

                // 如果参数表完全相同，则直接确定调用对象
                if (completelyEqual)
                {
                    return new[] {m};
                }

                // 如果参数表完全可转换，则设置候选调用端详
                if (completelyCastable)
                {
                    selectedConstructors.Add(m);
                }
            }

            if (selectedConstructors.Count == 0 && SuperClass != null)
            {
                return SuperClass.GetMethodByNameAndArgumentTypes(typeDeclarationContext, name, argumentTypes,
                    genericsTypes);
            }

            return selectedConstructors.ToArray();
        }


        /// <summary>
        /// 根据ID反射方法信息，会按继承顺序反向搜索超类。
        /// 不过滤参数表
        /// </summary>
        /// <returns></returns>
        public bool TryGetMethodById(int methodId, out MethodInformation method)
        {
            // TODO 暂时使用即时搜索，可以改为预先建立索引，索引关系在构造时即确定
            method = Methods.FirstOrDefault(m => m.Id == methodId);
            if (method != null)
            {
                return true;
            }

            return SuperClass != null && SuperClass.TryGetMethodById(methodId, out method);
        }

        /// <summary>
        /// 根据方法签名获取构造方法
        /// 精确匹配参数类型
        /// </summary>
        /// <param name="parameterTypes"></param>
        /// <param name="constructor"></param>
        /// <returns></returns>
        public bool TryGetConstructorBySignature(GorgeType[] parameterTypes, out ConstructorInformation constructor)
        {
            foreach (var c in Constructors)
            {
                if (c.Parameters.Select(p => p.Type).SequenceEqual(parameterTypes))
                {
                    constructor = c;
                    return true;
                }
            }

            constructor = default;
            return false;
        }

        /// <summary>
        /// 根据实参类型获取构造方法
        /// 向上搜索超类
        /// 如果有精确匹配，只返回一个
        /// 如果没有精确匹配，则返回多个非精确的重载
        /// </summary>
        /// <param name="typeDeclarationContext"></param>
        /// <param name="argumentTypes"></param>
        /// <returns></returns>
        public ConstructorInformation[] GetInjectorConstructorByArgumentTypes(
            GorgeLanguageRuntime typeDeclarationContext, GorgeType[] argumentTypes)
        {
            // 符合调用参数的重载
            var selectedConstructors = new List<ConstructorInformation>();

            for (var i = 0; i < InjectorConstructorCount; i++)
            {
                if (!TryGetInjectorConstructorById(i, out var c))
                {
                    throw new Exception($"不存在id为{i}的构造方法");
                }

                var parameters = c.Parameters;
                if (argumentTypes.Length != parameters.Length)
                {
                    continue;
                }

                // 记录参数表是否完全相同
                var completelyEqual = true;
                // 记录参数表是否完全可转换
                var completelyCastable = true;

                for (var j = 0; j < argumentTypes.Length; j++)
                {
                    if (argumentTypes[j].Equals(parameters[j].Type))
                    {
                        continue;
                    }

                    // 如果对位参数类型不同，关闭标记
                    completelyEqual = false;

                    if (typeDeclarationContext.CanAutoCastTo(argumentTypes[j], parameters[j].Type))
                    {
                        continue;
                    }

                    // 如果对位参数不可转换，关闭标记
                    completelyCastable = false;
                    break;
                }

                // 如果参数表完全相同，则直接确定调用对象
                if (completelyEqual)
                {
                    return new[] {c};
                }

                // 如果参数表完全可转换，则设置候选调用端详
                if (completelyCastable)
                {
                    selectedConstructors.Add(c);
                }
            }

            return selectedConstructors.ToArray();
        }

        /// <summary>
        /// 根据ID反射注入器构造方法信息，会按继承顺序反向搜索超类。
        /// 不过滤参数表
        /// </summary>
        /// <returns></returns>
        public bool TryGetInjectorConstructorById(int injectorConstructorId,
            out ConstructorInformation injectorConstructor)
        {
            // TODO 暂时使用即时搜索，可以改为预先建立索引，索引关系在构造时即确定
            injectorConstructor = InjectorConstructors.FirstOrDefault(c => c.Id == injectorConstructorId);
            if (injectorConstructor != null)
            {
                return true;
            }

            return SuperClass != null &&
                   SuperClass.TryGetInjectorConstructorById(injectorConstructorId, out injectorConstructor);
        }

        /// <summary>
        /// 获取注解
        /// 只检查本层
        /// </summary>
        /// <param name="annotationName"></param>
        /// <param name="annotation"></param>
        /// <returns></returns>
        public bool TryGetAnnotationByName(string annotationName, out Annotation annotation)
        {
            annotation = Annotations.FirstOrDefault(a => a.Name == annotationName);
            return annotation != null;
        }

        #endregion

        /// <summary>
        /// 判断本类是否是目标类或其子类
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public bool Is(string className)
        {
            // TODO 这个表是可以预先计算的
            if (Name == className)
            {
                return true;
            }

            if (SuperClass == null)
            {
                return false;
            }

            return SuperClass.Is(className);
        }

        public bool Is(GorgeType type)
        {
            return Is(type.FullName);
        }

        /// <summary>
        /// 本类的继承深度，从0开始，不计对object的继承
        /// </summary>
        /// <returns></returns>
        public int InheritanceDepth()
        {
            if (SuperClass == null)
            {
                return 0;
            }

            return SuperClass.InheritanceDepth() + 1;
        }

        #region Injector默认值索引计算

        /// <summary>
        /// 获取Injector默认值存储计数
        /// 本级总计数减去上级总计数
        /// </summary>
        /// <returns></returns>
        public TypeCount GetInjectorFieldDefaultValueStorageTypeCount()
        {
            var result = new TypeCount(InjectorFieldDefaultValueTypeCount);
            result.Minus(GetInjectorFieldDefaultValueStartTypeCount());

            return result;
        }

        /// <summary>
        /// Injector默认值索引起点
        /// </summary>
        /// <returns></returns>
        public TypeCount GetInjectorFieldDefaultValueStartTypeCount()
        {
            return SuperClass?.InjectorFieldDefaultValueTypeCount ?? new TypeCount();
        }

        /// <summary>
        /// 获取默认值存储相对索引
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public int GetInjectorIntFieldDefaultValueStorageIndexByIndex(int index)
        {
            if (SuperClass == null)
            {
                return index;
            }

            return index - SuperClass.InjectorFieldDefaultValueTypeCount.Int;
        }

        public int GetInjectorFloatFieldDefaultValueStorageIndexByIndex(int index)
        {
            if (SuperClass == null)
            {
                return index;
            }

            return index - SuperClass.InjectorFieldDefaultValueTypeCount.Float;
        }

        public int GetInjectorBoolFieldDefaultValueStorageIndexByIndex(int index)
        {
            if (SuperClass == null)
            {
                return index;
            }

            return index - SuperClass.InjectorFieldDefaultValueTypeCount.Bool;
        }

        public int GetInjectorStringFieldDefaultValueStorageIndexByIndex(int index)
        {
            if (SuperClass == null)
            {
                return index;
            }

            return index - SuperClass.InjectorFieldDefaultValueTypeCount.String;
        }

        public int GetInjectorObjectFieldDefaultValueStorageIndexByIndex(int index)
        {
            if (SuperClass == null)
            {
                return index;
            }

            return index - SuperClass.InjectorFieldDefaultValueTypeCount.Object;
        }

        #endregion
    }
}