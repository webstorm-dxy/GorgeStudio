using System;
using System.Collections.Generic;
using Gorge.GorgeCompiler.CodeBlock;
using Gorge.GorgeCompiler.Statement;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.CompileContext.Block
{
    /// <summary>
    /// 代码块上下文类型
    /// </summary>
    public enum BlockContextType
    {
        /// <summary>
        /// 常量上下文，表达式必须是编译时常量
        /// TODO 暂时合并Static到这里，目前没有认识到这两者的区别
        /// TODO 目前认识中，static和constant的区别在于static有所属类信息
        /// </summary>
        Constant,

        /// <summary>
        /// 静态方法上下文
        /// 允许本地变量赋值
        /// </summary>
        StaticMethod,

        /// <summary>
        /// 注入器上下文，只能操作注入器字段，不能操作实例字段
        /// </summary>
        Injector,

        /// <summary>
        /// 注入中上下文，注入器字段只读
        /// </summary>
        FieldInjecting,

        /// <summary>
        /// 实例上下文，只能操作实例字段，不能操作注入器字段
        /// </summary>
        Instance
    }

    /// <summary>
    /// 单个离块回填任务
    /// </summary>
    public class LeaveBlockBackPatchTask
    {
        private readonly IntermediateCode _code;
        private readonly Queue<LeaveBlockTarget> _targets;

        public LeaveBlockBackPatchTask(IntermediateCode code, params LeaveBlockTarget[] targets)
        {
            this._code = code;
            this._targets = new Queue<LeaveBlockTarget>(targets);
        }

        /// <summary>
        /// 尝试回填
        /// 修改回填目标，如果已经到达目标则实施回填
        /// </summary>
        /// <returns>如果回填完成，则返回true，否则返回false</returns>
        public bool TryBackPatch(int codeIndex, CodeBlockType type, bool isElse)
        {
            var target = _targets.Peek();
            switch (target.Type)
            {
                case LeaveBlockTargetType.SpecificQuantity:
                    target.Level--;
                    if (target.Level != 0)
                    {
                        return false;
                    }

                    _targets.Dequeue();
                    break;
                case LeaveBlockTargetType.For:
                    if (type is not CodeBlockType.For)
                    {
                        return false;
                    }

                    _targets.Dequeue();
                    break;
                case LeaveBlockTargetType.While:
                    if (type is not CodeBlockType.While)
                    {
                        return false;
                    }

                    _targets.Dequeue();
                    break;
                case LeaveBlockTargetType.Switch:
                    if (type is not CodeBlockType.Switch)
                    {
                        return false;
                    }

                    _targets.Dequeue();
                    break;
                case LeaveBlockTargetType.Else:
                    if (!isElse)
                    {
                        return false;
                    }

                    _targets.Dequeue();
                    break;
                case LeaveBlockTargetType.If:
                    if (type is not CodeBlockType.If)
                    {
                        return false;
                    }

                    _targets.Dequeue();
                    break;
                case LeaveBlockTargetType.DoWhile:
                    if (type is not CodeBlockType.DoWhile)
                    {
                        return false;
                    }

                    _targets.Dequeue();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // 完成所有目标后执行回填
            if (_targets.Count == 0)
            {
                // 回填
                IntermediateCode.PatchJump(codeIndex, _code);
                return true;
            }

            return false;
        }
    }
}