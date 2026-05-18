using System.Linq;

namespace Gorge.GorgeLanguage.Objective
{
    /// <summary>
    /// 字段完全信息。
    /// 由运行时和反射使用，字段的全部字面信息存储索引信息。
    /// </summary>
    public class FieldInformation
    {
        /// <summary>
        /// 字段编号。
        /// 在整个类内唯一，不与超类字段冲突。
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// 字段名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 字段类型
        /// </summary>
        public GorgeType Type { get; }

        /// <summary>
        /// 字段在对象实例中的索引
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// 字段注解
        /// </summary>
        public Annotation[] Annotations { get; }

        public Annotation[] GetAnnotations(string name)
        {
            return Annotations.Where(a => a.Name == name).ToArray();
        }

        public FieldInformation(int id, string name, GorgeType type, int index, Annotation[] annotations)
        {
            Id = id;
            Name = name;
            Type = type;
            Index = index;
            Annotations = annotations;
        }
    }
}