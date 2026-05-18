using System.Collections.Generic;

namespace Gorge.GorgeLanguage.Objective
{
    /// <summary>
    /// 可调用成员声明基类
    /// 实现了参数表的分配
    /// </summary>
    public abstract class InvokableMemberInformation
    {
        /// <summary>
        /// 参数表
        /// </summary>
        public ParameterInformation[] Parameters { get; }

        public InvokableMemberInformation(ParameterInformation[] parameterDeclarations)
        {
            var parameterCount = 0;
            var parameterIndexCount = new TypeCount();
            Parameters = parameterDeclarations;
            // List<ParameterInformation> parameters = new();
            // foreach (var parameter in parameterDeclarations)
            // {
            //     var index = parameterIndexCount.Count(parameter.Type.BasicType);
            //     parameters.Add(new ParameterInformation(parameterCount, parameter.Name,
            //         parameter.Type, index));
            //     parameterCount++;
            // }
            //
            // Parameters = parameters.ToArray();
        }
    }
}