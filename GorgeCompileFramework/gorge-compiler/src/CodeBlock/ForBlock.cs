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
    /// for代码快
    /// for(...,语句,...;表达式;...,语句,...){
    ///     ...
    ///     语句
    ///     ...
    /// }
    /// </summary>
    public class ForBlock : BaseCodeBlock
    {
        private readonly List<IStatement> _initStatements;
        private readonly IGorgeValueExpression _condition;
        private readonly List<IStatement> _updateStatements;
        private readonly NormalBlock _loopBlock;

        public ForBlock(bool isElse, List<IStatement> initStatements, IGorgeValueExpression condition,
            List<IStatement> updateStatements, List<IStatement> contentStatements, CodeBlockScope block) : base(isElse,
            block)
        {
            if (condition.ValueType.BasicType != BasicType.Bool)
            {
                throw new Exception($"while块的条件必须是bool型表达式，当前类型为{condition.ValueType}");
            }

            _loopBlock = new NormalBlock(false, contentStatements, block.GenerateSubBlock());

            _initStatements = initStatements;
            _condition = condition;
            _updateStatements = updateStatements;
        }

        protected override void AppendBlockContentCodes(List<IntermediateCode> existCodes)
        {
            #region 循环初始化

            foreach (var statement in _initStatements)
            {
                statement.AppendCodes(existCodes);
            }

            #endregion

            #region 循环退出判断

            var forStartLine = existCodes.Count;

            // 判断条件，为true则向下执行，为false则continue离块
            var conditionAddress = _condition.AppendCodes(Block, existCodes);

            existCodes.Add(
                Block.RegisterContinueJumpIfFalse((Address)conditionAddress, LeaveBlockTarget.SpecificQuantity(1)));

            #endregion

            #region 循环块

            _loopBlock.AppendCodes(existCodes);

            // 子块continue进入循环更新
            var loopBlockContinue = IntermediateCode.UnpatchedJump();
            existCodes.Add(loopBlockContinue);

            // 子块break接收则break离开
            existCodes.Add(Block.RegisterBreakJump(LeaveBlockTarget.SpecificQuantity(1)));

            #endregion

            #region 循环更新

            // 回填循环块continue
            IntermediateCode.PatchJump(existCodes.Count, loopBlockContinue);

            // for循环更新
            foreach (var statement in _updateStatements)
            {
                statement.AppendCodes(existCodes);
            }

            // 跳回条件判断处
            existCodes.Add(IntermediateCode.Jump(forStartLine));

            #endregion
        }

        public override CodeBlockType Type => CodeBlockType.For;
    }
}