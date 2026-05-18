namespace Gorge.GorgeLanguage.Objective
{
    /// <summary>
    /// 参数完全信息。
    /// 由运行时和反射使用，参数的全部字面信息存储索引信息。
    /// </summary>
    public class ParameterInformation
    {
        /// <summary>
        /// 参数编号
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// 参数名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 参数类型
        /// </summary>
        public GorgeType Type { get; }

        /// <summary>
        /// 参数索引
        /// </summary>
        public int Index { get; }

        public ParameterInformation(int id, string name, GorgeType type, int index)
        {
            Id = id;
            Name = name;
            Type = type;
            Index = index;
        }
    }
}