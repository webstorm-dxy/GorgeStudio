using System.Linq;

namespace Gorge.GorgeLanguage.Objective
{
    /// <summary>
    /// 方法完全信息。
    /// 由运行时和反射使用，方法的全部字面信息存储索引信息。
    /// </summary>
    public class MethodInformation
    {
        /// <summary>
        /// 方法编号
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// 方法名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 方法返回类型，为null代表无返回值
        /// </summary>
        public GorgeType ReturnType { get; }

        /// <summary>
        /// 方法参数信息
        /// </summary>
        public ParameterInformation[] Parameters { get; }

        public Annotation[] Annotations { get; }
        
        public MethodInformation(int id, string name, GorgeType returnType, ParameterInformation[] parameters, Annotation[] annotations)
        {
            Id = id;
            Name = name;
            ReturnType = returnType;
            Parameters = parameters;
            Annotations = annotations;
        }

        /// <summary>
        /// 根据参数名获取参数定义
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public bool TryGetParameterByName(string parameterName, out ParameterInformation parameter)
        {
            // TODO 暂时使用即时搜索，可以改为预先建立索引，索引关系在构造时即确定
            parameter = Parameters.FirstOrDefault(p => p.Name == parameterName);
            return parameter != null;
        }

        public bool TryGetAnnotationByName(string annotationName, out Annotation annotation)
        {
            // TODO 暂时使用即时搜索，可以改为预先建立索引，索引关系在构造时即确定
            annotation = Annotations.FirstOrDefault(a => a.Name == annotationName);
            return annotation != null;
        }

        /// <summary>
        /// 检查两方法是否签名相同
        /// 即名字和参数表相同
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool SignatureEquals(MethodInformation target)
        {
            if (Name != target.Name)
            {
                return false;
            }

            return Parameters.Select(p => p.Type).SequenceEqual(target.Parameters.Select(p => p.Type));
        }
    }
}