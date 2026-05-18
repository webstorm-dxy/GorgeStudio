namespace Gorge.GorgeLanguage.Objective
{
    /// <summary>
    /// 字段声明信息。
    /// 由编译器使用，只包含字段声明的字面信息。
    /// </summary>
    public class FieldDeclaration
    {
        /// <summary>
        /// 字段名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 字段类型
        /// </summary>
        public GorgeType Type { get; }

        /// <summary>
        /// 字段注解
        /// </summary>
        public Annotation[] Annotations { get; }

        public FieldDeclaration(string name, GorgeType type, Annotation[] annotations)
        {
            Name = name;
            Type = type;
            Annotations = annotations;
        }
    }
}