using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Statement
{
    public class BreakStatement : IStatement
    {
        private readonly List<LeaveBlockTarget> _targets;

        public BreakStatement(List<LeaveBlockTarget> targets, CodeBlockScope block, ParserRuleContext antlrContext)
        {
            _targets = targets;
            Block = block;
            AntlrContext = antlrContext;
        }

        public CodeBlockScope Block { get; }

        public void AppendCodes(List<IntermediateCode> existCodes)
        {
            existCodes.Add(Block.RegisterBreakJump(_targets.ToArray()));
        }

        public ParserRuleContext AntlrContext { get; }
    }

    /// <summary>
    /// break和continue语句使用的离块语句单次目标
    /// </summary>
    public class LeaveBlockTarget
    {
        /// <summary>
        /// 类型
        /// </summary>
        public LeaveBlockTargetType Type;

        /// <summary>
        /// 如果是SpecificQuantity，则表示离块数量
        /// </summary>
        public int Level;

        private LeaveBlockTarget(LeaveBlockTargetType type, int level)
        {
            Type = type;
            Level = level;
        }

        public static LeaveBlockTarget SpecificQuantity(int level)
        {
            return new LeaveBlockTarget(LeaveBlockTargetType.SpecificQuantity, level);
        }

        public static LeaveBlockTarget For()
        {
            return new LeaveBlockTarget(LeaveBlockTargetType.For, 1);
        }

        public static LeaveBlockTarget While()
        {
            return new LeaveBlockTarget(LeaveBlockTargetType.While, 1);
        }

        public static LeaveBlockTarget Switch()
        {
            return new LeaveBlockTarget(LeaveBlockTargetType.Switch, 1);
        }

        public static LeaveBlockTarget Else()
        {
            return new LeaveBlockTarget(LeaveBlockTargetType.Else, 1);
        }

        public static LeaveBlockTarget If()
        {
            return new LeaveBlockTarget(LeaveBlockTargetType.If, 1);
        }

        public static LeaveBlockTarget DoWhile()
        {
            return new LeaveBlockTarget(LeaveBlockTargetType.DoWhile, 1);
        }
    }

    public enum LeaveBlockTargetType
    {
        /// <summary>
        /// 离开指定层数
        /// </summary>
        SpecificQuantity,

        /// <summary>
        /// 离开最近的for块
        /// </summary>
        For,

        /// <summary>
        /// 离开最近的while块
        /// </summary>
        While,

        /// <summary>
        /// 离开最近的switch块
        /// </summary>
        Switch,

        /// <summary>
        /// 离开最近的else块
        /// </summary>
        Else,

        /// <summary>
        /// 离开最近的if块
        /// </summary>
        If,

        /// <summary>
        /// 离开最近的do-while块
        /// </summary>
        DoWhile
    }
}