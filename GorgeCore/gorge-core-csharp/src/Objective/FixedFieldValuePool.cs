using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Gorge.GorgeLanguage.Objective
{
    /// <summary>
    ///     静态字段池。
    ///     字段类型和数量必须在构造时确定
    /// </summary>
    public class FixedFieldValuePool
    {
        public FixedFieldValuePool(TypeCount typeCount)
        {
            Int = new int[typeCount.Int];
            Float = new float[typeCount.Float];
            Bool = new bool[typeCount.Bool];
            String = new string[typeCount.String];
            Object = new GorgeObject[typeCount.Object];
        }

        public bool Equals(FixedFieldValuePool target)
        {
            return Int.SequenceEqual(target.Int) &&
                   Float.SequenceEqual(target.Float) &&
                   Bool.SequenceEqual(target.Bool) &&
                   String.SequenceEqual(target.String) &&
                   Object.SequenceEqual(target.Object);
        }

        #region 数据池

        public readonly int[] Int;
        public readonly float[] Float;
        public readonly bool[] Bool;
        public readonly string[] String;
        public readonly GorgeObject[] Object;

        #endregion

        #region 零开销 ref 访问器 (绕过数组边界检查)

        public ref int IntRef(int index) =>
            ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Int), index);
        public ref float FloatRef(int index) =>
            ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Float), index);
        public ref bool BoolRef(int index) =>
            ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Bool), index);
        public ref string? StringRef(int index) =>
            ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(String), index);
        public ref GorgeObject? ObjectRef(int index) =>
            ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Object), index);

        #endregion
    }

    /// <summary>
    /// 存储类型计数器
    /// </summary>
    public class TypeCount
    {
        public int Bool;
        public int Float;
        public int Object;
        public int Int;
        public int String;

        /// <summary>
        /// 构造并初始化为0
        /// </summary>
        public TypeCount()
        {
        }

        /// <summary>
        /// 拷贝构造
        /// </summary>
        /// <param name="source"></param>
        public TypeCount(TypeCount source)
        {
            Bool = source.Bool;
            Float = source.Float;
            Object = source.Object;
            Int = source.Int;
            String = source.String;
        }

        /// <summary>
        /// 构造并初始化
        /// </summary>
        /// <param name="intCount"></param>
        /// <param name="floatCount"></param>
        /// <param name="boolCount"></param>
        /// <param name="stringCount"></param>
        /// <param name="objectCount"></param>
        public TypeCount(int intCount, int floatCount, int boolCount, int stringCount, int objectCount)
        {
            Int = intCount;
            Float = floatCount;
            Bool = boolCount;
            String = stringCount;
            Object = objectCount;
        }

        /// <summary>
        /// 两计数相加
        /// </summary>
        /// <param name="count"></param>
        public void Add(TypeCount count)
        {
            Int += count.Int;
            Float += count.Float;
            Bool += count.Bool;
            String += count.String;
            Object += count.Object;
        }

        public void Minus(TypeCount count)
        {
            Int -= count.Int;
            Float -= count.Float;
            Bool -= count.Bool;
            String -= count.String;
            Object -= count.Object;
        }

        /// <summary>
        /// 合并最大值
        /// </summary>
        /// <param name="count"></param>
        public void Max(TypeCount count)
        {
            Int = Math.Max(count.Int, Int);
            Float = Math.Max(count.Float, Float);
            Bool = Math.Max(count.Bool, Bool);
            String = Math.Max(count.String, String);
            Object = Math.Max(count.Object, Object);
        }

        /// <summary>
        ///     计数并返回计数前的值，返回值可做用作分配的索引值
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public int Count(BasicType type)
        {
            int result;
            switch (type)
            {
                case BasicType.Int:
                case BasicType.Enum:
                    result = Int;
                    Int += 1;
                    break;
                case BasicType.Float:
                    result = Float;
                    Float += 1;
                    break;
                case BasicType.Bool:
                    result = Bool;
                    Bool += 1;
                    break;
                case BasicType.String:
                    result = String;
                    String += 1;
                    break;
                case BasicType.Object:
                case BasicType.Interface:
                case BasicType.Delegate:
                    result = Object;
                    Object += 1;
                    break;
                default:
                    throw new Exception("不支持当前类型");
            }

            return result;
        }
    }
}