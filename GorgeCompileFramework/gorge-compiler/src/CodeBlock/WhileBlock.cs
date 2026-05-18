using System;
using System.Collections.Generic;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Expression;
using Gorge.GorgeCompiler.Statement;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.CodeBlock
{
    /// <summary>
    /// while代码快
    /// while(表达式){
    ///     ...
    ///     语句
    ///     ...
    /// }
    /// </summary>
    public class WhileBlock : BaseCodeBlock
    {
        private readonly IGorgeValueExpression _condition;
        private readonly NormalBlock _loopBlock;


        public WhileBlock(bool isElse, IGorgeValueExpression condition, List<IStatement> statements,
            CodeBlockScope block) : base(isElse, block)
        {
            if (condition.ValueType.BasicType != BasicType.Bool)
            {
                throw new Exception($"while块的条件必须是bool型表达式，当前类型为{condition.ValueType}");
            }

            _loopBlock = new NormalBlock(false, statements, block.GenerateSubBlock());

            _condition = condition;
        }

        protected override void AppendBlockContentCodes(List<IntermediateCode> existCodes)
        {
            #region 循环退出判断

            var whileStartLine = existCodes.Count;

            var conditionAddress = _condition.AppendCodes(Block, existCodes);

            // 如果条件为假，则continue退出
            existCodes.Add(
                Block.RegisterContinueJumpIfFalse((Address)conditionAddress, LeaveBlockTarget.SpecificQuantity(1)));

            // 否则向下执行循环块

            #endregion

            #region 循环块

            // 块内语句
            _loopBlock.AppendCodes(existCodes);

            // 子块continue接收返回条件检查
            existCodes.Add(IntermediateCode.Jump(whileStartLine));

            // 子块break接收则break离开
            existCodes.Add(Block.RegisterBreakJump(LeaveBlockTarget.SpecificQuantity(1)));

            #endregion
        }

        public override CodeBlockType Type => CodeBlockType.While;
    }
}