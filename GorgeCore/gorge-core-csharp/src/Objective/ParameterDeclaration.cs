using System.Diagnostics.CodeAnalysis;

namespace Gorge.GorgeLanguage.Objective
{
    /// <summary>
    /// 参数声明信息。
    /// 由编译器使用，只包含参数声明的字面信息。
    /// </summary>
    public class ParameterDeclaration
    {
        /// <summary>
        /// 参数名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 参数类型
        /// </summary>
        public GorgeType Type { get; }

        public ParameterDeclaration([DisallowNull] string name, GorgeType type)
        {
            Name = name;
            Type = type;
        }
    }
}