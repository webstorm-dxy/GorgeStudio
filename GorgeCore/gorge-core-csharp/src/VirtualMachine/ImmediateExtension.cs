using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeLanguage.VirtualMachine
{
    public static class ImmediateExtension
    {
        /// <summary>
        /// 转换为中间代码常量操作数
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Immediate ToImmediate(this int value)
        {
            return Immediate.Int(value);
        }

        /// <summary>
        /// 转换为中间代码常量操作数
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Immediate ToImmediate(this float value)
        {
            return Immediate.Float(value);
        }

        /// <summary>
        /// 转换为中间代码常量操作数
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Immediate ToImmediate(this bool value)
        {
            return Immediate.Bool(value);
        }

        /// <summary>
        /// 转换为中间代码常量操作数
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Immediate ToImmediate(this string value)
        {
            return Immediate.String(value);
        }

        /// <summary>
        /// 转换为中间代码常量操作数
        /// </summary>
        /// <param name="value"></param>
        /// <param name="className"></param>
        /// <param name="namespaceName"></param>
        /// <returns></returns>
        public static Immediate ToImmediate(this GorgeObject value, string className, string namespaceName)
        {
            return Immediate.Object(value, className,namespaceName);
        }
    }
}