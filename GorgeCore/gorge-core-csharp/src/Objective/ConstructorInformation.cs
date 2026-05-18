using System.Linq;

namespace Gorge.GorgeLanguage.Objective
{
    /// <summary>
    /// 构造方法完全信息。
    /// 由运行时和反射使用，构造方法的全部字面信息存储索引信息。
    /// </summary>
    public class ConstructorInformation
    {
        /// <summary>
        /// 构造方法编号
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// 参数表
        /// </summary>
        public ParameterInformation[] Parameters { get; }
        
        /// <summary>
        /// 字段注解
        /// </summary>
        public Annotation[] Annotations { get; }
        
        public Annotation[] GetAnnotations(string name)
        {
            return Annotations.Where(a => a.Name == name).ToArray();
        }

        public ConstructorInformation(int id, ParameterInformation[] parameters, Annotation[] annotations)
        {
            Id = id;
            Parameters = parameters;
            Annotations = annotations;
        }

        /// <summary>
        /// 检测是否具有相同构造方法签名
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool IsSameSignatureTo(ConstructorInformation target)
        {
            if (Parameters.Length != target.Parameters.Length)
            {
                return false;
            }

            for (var i = 0; i < Parameters.Length; i++)
            {
                if (!Parameters[i].Type.Equals(target.Parameters[i].Type))
                {
                    return false;
                }
            }

            return true;
        }
    }
}