using System.Collections.Generic;
using System.Linq;

namespace Gorge.GorgeLanguage.Objective
{
    public abstract class GorgeInterface
    {
        /// <summary>
        /// 类名
        /// </summary>
        public abstract GorgeType Type { get; }

        public string Name => Type.FullName;

        public abstract bool IsNative { get; }

        /// <summary>
        /// 方法表
        /// </summary>
        public MethodInformation[] Methods { get; }

        public GorgeInterface(MethodInformation[] methodDeclarations)
        {
            Methods = methodDeclarations;
        }

        /// <summary>
        /// 按参数表检索可调用的方法
        /// 先搜本类模糊，再搜超类精确
        /// </summary>
        /// <param name="typeDeclarationContext"></param>
        /// <param name="name"></param>
        /// <param name="argumentTypes"></param>
        /// <returns></returns>
        public MethodInformation[] GetMethodByNameAndArgumentTypes(GorgeLanguageRuntime typeDeclarationContext,
            string name, GorgeType[] argumentTypes)
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

                    if (typeDeclarationContext.CanAutoCastTo(argumentTypes[i], parameters[i].Type))
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
                    return new[] {m};
                }

                // 如果参数表完全可转换，则设置候选调用端详
                if (completelyCastable)
                {
                    selectedConstructors.Add(m);
                }
            }

            return selectedConstructors.ToArray();
        }

        public bool ContainsMethodWithName(string methodName)
        {
            return Methods.Any(m => m.Name == methodName);
        }
    }

    public class CompiledInterface : GorgeInterface
    {
        /// <summary>
        /// 类名
        /// </summary>
        public override GorgeType Type { get; }

        public override bool IsNative { get; }

        public CompiledInterface(GorgeType type, bool isNative, MethodInformation[] methodDeclarations) : base(
            methodDeclarations)
        {
            Type = type;
            IsNative = isNative;
        }
    }
}