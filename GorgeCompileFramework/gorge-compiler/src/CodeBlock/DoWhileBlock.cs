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
    /// do while代码快
    /// do{
    ///     ...
    ///     语句
    ///     ...
    /// } while(表达式);
    /// </summary>
    public class DoWhileBlock : BaseCodeBlock
    {
        private readonly IGorgeValueExpression _condition;
        private readonly NormalBlock _loopBlock;

        public DoWhileBlock(bool isElse, IGorgeValueExpression condition, List<IStatement> statements,
            CodeBlockScope block) : base(
            isElse, block)
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
            #region 循环块

            var whileStartLine = existCodes.Count;

            _loopBlock.AppendCodes(existCodes);

            // 子块continue接收进入条件检查
            var loopBlockContinue = IntermediateCode.UnpatchedJump();
            existCodes.Add(loopBlockContinue);

            // 子块break接收则break离开
            existCodes.Add(Block.RegisterBreakJump(LeaveBlockTarget.SpecificQuantity(1)));

            #endregion

            #region 循环退出判断

            // 条件判断
            var conditionAddress = _condition.AppendCodes(Block, existCodes);

            // 如果条件为假，则continue退出
            existCodes.Add(
                Block.RegisterContinueJumpIfFalse((Address)conditionAddress, LeaveBlockTarget.SpecificQuantity(1)));

            // 否则回到循环块
            existCodes.Add(IntermediateCode.Jump(whileStartLine));

            #endregion
        }

        public override CodeBlockType Type => CodeBlockType.DoWhile;
    }
}