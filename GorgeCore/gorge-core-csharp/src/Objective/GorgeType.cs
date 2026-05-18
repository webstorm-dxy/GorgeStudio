using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Gorge.GorgeLanguage.Objective
{
    public class GorgeType : IEquatable<GorgeType>
    {
        /// <summary>
        /// 类基础类型
        /// </summary>
        public readonly BasicType BasicType;

        /// <summary>
        /// 类名
        /// TODO 未来可能替换成复合namespace的结构
        /// </summary>
        public readonly string ClassName;

        public readonly string NamespaceName;

        /// <summary>
        /// 本类是否为泛型参数
        /// </summary>
        public readonly bool IsGenerics;

        /// <summary>
        /// 子类型
        /// 如果是ObjectArray，则0号位为内容的类型
        /// 如果是Injector，则0号位为对应的类型
        /// 其他情况可能考虑存储泛型类型
        ///   有可能把Injector纳入视为泛型？
        /// </summary>
        public readonly GorgeType[] SubTypes;

        internal GorgeType(BasicType basicType, string className, string namespaceName, bool isGenerics,
            params GorgeType[] subTypes)
        {
            BasicType = basicType;
            ClassName = className;
            NamespaceName = namespaceName;
            IsGenerics = isGenerics;
            SubTypes = subTypes;
        }

        public string FullName
        {
            get
            {
                if (NamespaceName == null)
                {
                    return ClassName;
                }
                else
                {
                    return NamespaceName + "." + ClassName;
                }
            }
        }

        /// <summary>
        /// 判断目标类是否是本类的泛型参数类
        /// 也就是判断子类型长度是否相等，而不要求子类型严格相等
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsGenericsInstance(GorgeType other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return BasicType == other.BasicType && ClassName == other.ClassName &&
                   NamespaceName == other.NamespaceName &&
                   SubTypes.Length == other.SubTypes.Length;
        }

        #region 等价性

        // TODO 需要改动，考虑subtype

        public bool Equals(GorgeType other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return BasicType == other.BasicType && ClassName == other.ClassName &&
                   NamespaceName == other.NamespaceName &&
                   SubTypes.SequenceEqual(other.SubTypes);
        }

        public override bool Equals(object obj)
        {
            return obj is GorgeType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int) BasicType, ClassName, NamespaceName);
        }

        #endregion

        #region 静态构造

        public static readonly GorgeType Int = new(BasicType.Int, null, null, false);

        public static readonly GorgeType Float = new(BasicType.Float, null, null, false);

        public static readonly GorgeType Bool = new(BasicType.Bool, null, null, false);

        public static readonly GorgeType String = new(BasicType.String, null, null, false);

        public static readonly GorgeType IntArray = Object("IntArray", "Gorge");
        public static readonly GorgeType FloatArray = Object("FloatArray", "Gorge");
        public static readonly GorgeType BoolArray = Object("BoolArray", "Gorge");
        public static readonly GorgeType StringArray = Object("StringArray", "Gorge");

        public static GorgeType ObjectArray(GorgeType itemType) =>
            new(BasicType.Object, "ObjectArray", "Gorge", false, itemType);

        public static readonly GorgeType IntList = Object("IntList", "Gorge");
        public static readonly GorgeType FloatList = Object("FloatList", "Gorge");
        public static readonly GorgeType BoolList = Object("BoolList", "Gorge");
        public static readonly GorgeType StringList = Object("StringList", "Gorge");

        public static GorgeType ObjectList(GorgeType itemType) =>
            new(BasicType.Object, "ObjectList", "Gorge", false, itemType);

        public static GorgeType Enum(string enumName, string namespaceName = null)
        {
            return new GorgeType(BasicType.Enum, enumName, namespaceName, false);
        }

        public static GorgeType Object(string className, string namespaceName = null, params GorgeType[] genericsTypes)
        {
            return new GorgeType(BasicType.Object, className, namespaceName, false, genericsTypes);
        }

        public static GorgeType Injector(GorgeType injectedClass)
        {
            return new GorgeType(BasicType.Object, "Injector", "Gorge", false, injectedClass);
        }

        public static GorgeType Generics(string className)
        {
            return new GorgeType(BasicType.Object, className, null, true);
        }

        public static GorgeType Interface(string interfaceName, string namespaceName = null)
        {
            return new GorgeType(BasicType.Interface, interfaceName, namespaceName, false);
        }

        public static GorgeType Delegate([AllowNull] GorgeType returnType, params GorgeType[] parameterTypes)
        {
            var subTypes = new List<GorgeType>();
            subTypes.Add(returnType);
            subTypes.AddRange(parameterTypes);
            return new GorgeType(BasicType.Delegate, null, null, false, subTypes.ToArray());
        }

        #endregion

        #region 派生

        /// <summary>
        /// 填充泛型参数类型形成对应的实例类型
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
        public GorgeType CreateGenericsInstanceType(params GorgeType[] types)
        {
            if (IsGenerics)
            {
                throw new Exception("不能直接填充泛型类本身");
            }

            var subTypes = new GorgeType[SubTypes.Length];
            var j = 0;

            for (var i = 0; i < subTypes.Length; i++)
            {
                var subType = SubTypes[i];
                if (subType.IsGenerics)
                {
                    subTypes[i] = types[j];
                    j++;
                }
                else
                {
                    subTypes[i] = SubTypes[i];
                }
            }

            return new GorgeType(BasicType, ClassName, NamespaceName, false, subTypes);
        }

        #endregion

        public override string ToString()
        {
            var namespacePart = NamespaceName == null ? "" : (NamespaceName + ".");

            return namespacePart + BasicType switch
            {
                BasicType.Int => "int",
                BasicType.Float => "float",
                BasicType.Bool => "bool",
                BasicType.Enum => FullName + "(Enum)",
                BasicType.String => "string",
                BasicType.Object => FullName + (SubTypes == null || SubTypes.Length == 0
                    ? ""
                    : $"({string.Join<GorgeType>(",", SubTypes)})"),
                BasicType.Interface => FullName + "(Interface)",
                BasicType.Delegate =>
                    $"delegate:{SubTypes[0]?.ToString() ?? "Void"}({string.Join(",", SubTypes.Skip(1))})",
                _ => throw new Exception("未知类型")
            };
        }

        #region Injector序列化使用

        /// <summary>
        /// 本类型对应的硬编码代码
        /// </summary>
        /// <returns></returns>
        public string HardcodeType()
        {
            switch (BasicType)
            {
                case BasicType.Int:
                    return "int";
                case BasicType.Float:
                    return "float";
                case BasicType.Bool:
                    return "bool";
                case BasicType.Enum:
                    return FullName;
                case BasicType.String:
                    return "string";
                case BasicType.Object:
                    switch (FullName)
                    {
                        case "Gorge.Injector":
                            return SubTypes[0].FullName + "^";
                        case "Gorge.IntArray":
                            return "int[]";
                        case "Gorge.StringArray":
                            return "string[]";
                        case "Gorge.ObjectArray":
                            return SubTypes[0].FullName + "[]";
                        default:
                            return FullName;
                    }
                case BasicType.Interface:
                    return FullName;
                case BasicType.Delegate:
                    throw new Exception($"类型{this}暂不支持生成硬编码代码");
                default:
                    throw new Exception($"类型{this}无法生成硬编码代码");
            }
        }

        #endregion
    }
}