using System;

namespace Gorge.GorgeLanguage.Objective
{
    /// <summary>
    ///     Gorge基本数据类型
    /// </summary>
    public enum BasicType
    {
        Int,
        Float,
        Bool,
        Enum,
        String,
        Object,
        Interface,
        Delegate
    }

    public static class BasicTypeExtension
    {
        public static Type JsonSerializeType(this BasicType type)
        {
            return type switch
            {
                BasicType.Int => typeof(int),
                BasicType.Float => typeof(float),
                BasicType.Bool => typeof(bool),
                BasicType.Enum => typeof(Enum),
                BasicType.String => typeof(string),
                BasicType.Object => typeof(GorgeObject),
                _ => throw new Exception("不支持该类型")
            };
        }
    }
}