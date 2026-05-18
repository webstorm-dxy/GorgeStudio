using System;
using System.Collections.Generic;
using System.Linq;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeLanguage.Objective
{
    /// <summary>
    /// Gorge语言运行时环境
    /// </summary>
    public class GorgeLanguageRuntime : IImplementationBase
    {
        public static GorgeLanguageRuntime Instance;

        public IEnumerable<ClassDeclaration> ClassDeclarations { get; }
        IEnumerable<GorgeInterface> IImplementationBase.Interfaces => Interfaces;
        IEnumerable<GorgeEnum> IImplementationBase.Enums => Enums;
        IEnumerable<GorgeClass> IImplementationBase.Classes => Classes;

        public readonly GorgeClass[] Classes;
        public readonly GorgeEnum[] Enums;
        public readonly GorgeInterface[] Interfaces;
        public readonly IntermediateCodeVirtualMachine Vm;

        public GorgeLanguageRuntime(IImplementationBase native, IImplementationBase context)
        {
            var classes = new List<GorgeClass>();
            classes.AddRange(native.Classes);
            classes.AddRange(context.Classes);
            Classes = classes.ToArray();
            ClassDeclarations = Classes.Select(c => c.Declaration);
            _classes = new Dictionary<string, GorgeClass>();

            foreach (var @class in Classes)
            {
                _classes.Add(@class.Declaration.Name, @class);
            }

            var enums = new List<GorgeEnum>();
            enums.AddRange(native.Enums);
            enums.AddRange(context.Enums);
            Enums = enums.ToArray();

            var interfaces = new List<GorgeInterface>();
            interfaces.AddRange(native.Interfaces);
            interfaces.AddRange(context.Interfaces);
            Interfaces = interfaces.ToArray();

            Vm = new IntermediateCodeVirtualMachine();
        }

        /// <summary>
        /// Class名字索引
        /// </summary>
        private Dictionary<string, GorgeClass> _classes;

        /// <summary>
        ///     反射类定义
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public GorgeClass GetClass(string className)
        {
            if (!_classes.TryGetValue(className, out var @class))
            {
                throw new Exception($"当前运行环境不存在名为{className}的类");
            }

            return @class;
        }

        /// <summary>
        ///     反射类定义
        /// </summary>
        /// <param name="interfaceName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public GorgeInterface GetInterface(string interfaceName)
        {
            // TODO 可以预先构建索引
            var result = Interfaces.FirstOrDefault(i => i.Name == interfaceName);
            if (result == null) throw new Exception($"当前运行环境不存在名为{interfaceName}的接口");

            return result;
        }

        /// <summary>
        ///     反射枚举定义
        /// </summary>
        /// <param name="enumName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public GorgeEnum GetEnum(string enumName)
        {
            // TODO 可以预先构建索引
            var result = Enums.FirstOrDefault(e => e.Name == enumName);
            if (result == null) throw new Exception($"当前运行环境不存在名为{enumName}的枚举");
            return result;
        }

        public bool TryGetClassDeclarationByType(GorgeType type, out ClassDeclaration classDeclaration)
        {
            if (_classes.TryGetValue(type.FullName, out var @class))
            {
                classDeclaration = @class.Declaration;
                return true;
            }

            classDeclaration = null;
            return false;
        }

        /// <summary>
        /// 判断一个类型是否能强制转换为另一个类型
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public bool CanCastTo(GorgeType from, GorgeType to)
        {
            // 能自动转换，就能强制转换
            if (CanAutoCastTo(from, to))
            {
                return true;
            }

            // 能反向自动转换，就能强制转换
            // 包含超类转子类，接口转实现，float转int
            if (CanAutoCastTo(to, from))
            {
                return true;
            }

            // 其他情况下不能转换
            // 目前认为字符串拼接时转为字符串的操作为加法特定转换
            return false;
        }

        /// <summary>
        /// 判断一个类型是否能自动转换为另一个类型
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public bool CanAutoCastTo(GorgeType from, GorgeType to)
        {
            // 相等类型之间可以自动转换
            if (from.Equals(to))
            {
                return true;
            }

            // 非相等类型
            switch (from.BasicType)
            {
                // int可转float
                case BasicType.Int:
                    return to.BasicType is BasicType.Float;
                // 其他基本类型不能自动转换
                case BasicType.Float:
                case BasicType.Bool:
                case BasicType.Enum:
                case BasicType.String:
                    return false;
                // 类可以转换到超类或实现的接口
                case BasicType.Object:
                    // null可转任意对象和string
                    if (from.FullName == "null")
                    {
                        return to.BasicType is BasicType.Object or BasicType.Interface or BasicType.String
                            or BasicType.Delegate;
                    }

                    // Object只能转Object
                    // 由于已经有相等判断，进入此处则必然不能转换
                    if (from.FullName == "Object")
                    {
                        return false;
                    }

                    if (!TryGetClassDeclarationByType(from, out var fromDeclaration))
                    {
                        throw new Exception($"类{from}定义不存在");
                    }

                    // 类转类，判断超类
                    if (to.BasicType is BasicType.Object)
                    {
                        // 任意类可转Object
                        if (to.FullName == "Object")
                        {
                            return true;
                        }

                        // Injector对转时，判断注入对象的类
                        if (to.FullName == "Gorge.Injector" && from.FullName == "Gorge.Injector")
                        {
                            return CanAutoCastTo(from.SubTypes[0], to.SubTypes[0]);
                        }

                        // 无超类不可转
                        if (fromDeclaration.SuperClass == null)
                        {
                            return false;
                        }

                        // 否则判断自己的超类能否则转为目标类
                        return CanAutoCastTo(fromDeclaration.SuperClass.Type, to);
                    }
                    else if (to.BasicType is BasicType.Interface)
                    {
                        // 判断是否存在一个实现的接口能转为目标接口
                        return fromDeclaration.SuperInterfaces.Any(i => CanAutoCastTo(i.Type, to));
                    }
                    // 类不可转其他类型
                    else
                    {
                        return false;
                    }
                // 接口可转换到Object
                // TODO 实现了接口继承需要调整这里
                case BasicType.Interface:
                    if (to.BasicType is BasicType.Object && to.FullName is "Object")
                    {
                        return true;
                    }

                    return false;
                case BasicType.Delegate:
                    for (var i = 0; i < from.SubTypes.Length; i++)
                    {
                        if (i == 0)
                        {
                            // 如果转换源的返回值能auto cast成转换目标的返回值，则接受
                            if (!CanAutoCastTo(from.SubTypes[i], to.SubTypes[i]))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            // 如果转换目标的参数能auto cast成转换源的参数，则接受
                            if (!CanAutoCastTo(to.SubTypes[i], from.SubTypes[i]))
                            {
                                return false;
                            }
                        }
                    }

                    return true;
                default:
                    throw new Exception("不支持当前类型");
            }
        }
    }
}