using System.Collections.Generic;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeLanguage.VirtualMachine
{
    /// <summary>
    /// 可被虚拟机调用执行的结构
    /// </summary>
    public interface IVirtualMachineExecutable
    {
        /// <summary>
        /// 返回值类型，用于执行return语句
        /// 为null则无返回值
        /// </summary>
        public GorgeType ReturnType { get; }

        /// <summary>
        /// 待执行代码
        /// </summary>
        public IntermediateCode[] Code { get; }

        /// <summary>
        /// 本地变量存储空间大小
        /// </summary>
        public TypeCount LocalVariableCount { get; }

        /// <summary>
        /// 获取类定义
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public GorgeClass GetClass(string className);

        public string DebugName { get; }
    }

    public abstract class VirtualMachineExecutableBase : IVirtualMachineExecutable
    {
        public abstract GorgeType ReturnType { get; }
        public abstract IntermediateCode[] Code { get; }
        public abstract TypeCount LocalVariableCount { get; }

        private Dictionary<string, GorgeClass> _classCache = new();

        public VirtualMachineExecutableBase(string className, string methodName)
        {
            DebugName = className + "." + methodName;
        }

        public GorgeClass GetClass(string className)
        {
            if (_classCache.TryGetValue(className, out var @class))
            {
                return @class;
            }

            @class = GorgeLanguageRuntime.Instance.GetClass(className);
            _classCache.Add(className, @class);
            return @class;
        }

        public string DebugName { get; }
    }
}