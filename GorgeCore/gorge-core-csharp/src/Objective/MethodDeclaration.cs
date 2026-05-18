using System.Linq;

namespace Gorge.GorgeLanguage.Objective
{
    // /// <summary>
    // /// 方法声明信息。
    // /// 由编译器使用，只包含方法声明的字面信息。
    // /// </summary>
    // public class MethodDeclaration : InvokableMemberDeclaration
    // {
    //     /// <summary>
    //     /// 方法名
    //     /// </summary>
    //     public string Name { get; }
    //
    //     /// <summary>
    //     /// 方法返回类型，为null代表无返回值
    //     /// </summary>
    //     public GorgeType ReturnType { get; }
    //     
    //     public Annotation[] Annotations { get; }
    //
    //     public MethodDeclaration(string name, GorgeType returnType, ParameterDeclaration[] parameterDeclarations, Annotation[] annotations)
    //         : base(parameterDeclarations)
    //     {
    //         Name = name;
    //         ReturnType = returnType;
    //         Annotations = annotations;
    //     }
    //     
    //     /// <summary>
    //     /// 检查两方法是否签名相同
    //     /// 即名字和参数表相同
    //     /// </summary>
    //     /// <param name="target"></param>
    //     /// <returns></returns>
    //     public bool SignatureEquals(MethodDeclaration target)
    //     {
    //         if (Name != target.Name)
    //         {
    //             return false;
    //         }
    //
    //         return Parameters.Select(p => p.Type).SequenceEqual(target.Parameters.Select(p => p.Type));
    //     }
    // }
}