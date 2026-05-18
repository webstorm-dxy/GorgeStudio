using System.Collections.Generic;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeCompiler.Expression;
using Gorge.GorgeCompiler.Statement;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.CodeBlock
{
    /// <summary>
    /// if代码快
    /// if(条件表达式){
    ///     ...
    ///     语句
    ///     ...
    /// }
    /// </summary>
    public class IfBlock : BaseCodeBlock
    {
        private readonly IGorgeValueExpression _condition;
        private readonly List<IStatement> _statements;

        public IfBlock(bool isElse, IGorgeValueExpression condition, List<IStatement> statements,
            CodeBlockScope block) : base(isElse, block)
        {
            if (condition.ValueType.BasicType != BasicType.Bool)
            {
                throw new GorgeCompileException($"if块的条件必须是bool型表达式，当前类型为{condition.ValueType}",
                    condition.ExpressionLocation);
            }

            _condition = condition;
            _statements = statements;
        }

        public override CodeBlockType Type => CodeBlockType.If;

        protected override void AppendBlockContentCodes(List<IntermediateCode> existCodes)
        {
            #region 条件判断

            // 条件判断
            var conditionAddress = _condition.AppendCodes(Block, existCodes);

            // 如果条件失败，则break离块
            existCodes.Add(
                Block.RegisterBreakJumpIfFalse((Address) conditionAddress, LeaveBlockTarget.SpecificQuantity(1)));

            // 否则向下执行块内语句

            #endregion

            #region 块内语句

            foreach (var statement in _statements)
            {
                statement.AppendCodes(existCodes);
            }

            #endregion
        }
    }
}