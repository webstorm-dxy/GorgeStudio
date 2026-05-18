using System;
using System.Collections.Generic;
using Gorge.GorgeCompiler.CompileContext.Block;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Expression;
using Gorge.GorgeCompiler.Statement;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.CodeBlock
{
    /// <summary>
    /// switch代码快
    /// switch(表达式){
    ///     case 表达式 :
    ///         ...
    ///         语句
    ///         ...
    ///     default:
    ///         ...
    ///         语句
    ///         ...
    /// }
    /// </summary>
    public class SwitchBlock : BaseCodeBlock
    {
        private readonly IGorgeValueExpression _condition;
        private readonly List<SwitchCase> _cases;

        public SwitchBlock(bool isElse, IGorgeValueExpression condition, List<SwitchCase> cases,
            CodeBlockScope block) : base(isElse, block)
        {
            foreach (var switchCase in cases)
            {
                if (switchCase.CaseExpression != null &&
                    !switchCase.CaseExpression.ValueType.Equals(condition.ValueType))
                {
                    if (condition.ValueType.BasicType == BasicType.Float &&
                        switchCase.CaseExpression.ValueType.BasicType == BasicType.Int) // int到float的自动类型转换
                    {
                    }
                    else
                    {
                        throw new Exception(
                            $"case类型与switch块类型不同，switch块类型是{condition.ValueType}，case类型是{switchCase.CaseExpression.ValueType}");
                    }
                }
            }

            _condition = condition;
            _cases = cases;
        }

        public override CodeBlockType Type => CodeBlockType.Switch;

        protected override void AppendBlockContentCodes(List<IntermediateCode> existCodes)
        {
            // 判断条件
            var conditionAddress = _condition.AppendCodes(Block, existCodes);

            // 回填跳转指令表
            var jumpToCases = new Dictionary<SwitchCase, IntermediateCode>();
            var hasDefaultCase = false;

            foreach (var switchCase in _cases)
            {
                if (switchCase.CaseExpression != null)
                {
                    var caseAddress = switchCase.CaseExpression.AppendCodes(Block, existCodes);
                    // int到float的隐式类型转换
                    if (_condition.ValueType.BasicType == BasicType.Float &&
                        switchCase.CaseExpression.ValueType.BasicType == BasicType.Int)
                    {
                        var castAddress = Block.AddTempVariable(SymbolicGorgeType.Float);
                        existCodes.Add(IntermediateCode.IntCastToFloat(castAddress, (Address) castAddress));
                        caseAddress = castAddress;
                    }

                    // 比较是否和当前case相等
                    var compareResult = Block.AddTempVariable(SymbolicGorgeType.Bool);

                    switch (_condition.ValueType.BasicType)
                    {
                        case BasicType.Int:
                        case BasicType.Enum:
                            existCodes.Add(IntermediateCode.IntEquality(compareResult, (Address) conditionAddress,
                                (Address) caseAddress));
                            break;
                        case BasicType.Float:
                            existCodes.Add(IntermediateCode.FloatEquality(compareResult, (Address) conditionAddress,
                                (Address) caseAddress));
                            break;
                        case BasicType.Bool:
                            existCodes.Add(IntermediateCode.BoolEquality(compareResult, (Address) conditionAddress,
                                (Address) caseAddress));
                            break;
                        case BasicType.String:
                            existCodes.Add(
                                IntermediateCode.StringEquality(compareResult, (Address) conditionAddress,
                                    (Address) caseAddress));
                            break;
                        case BasicType.Object:
                            existCodes.Add(
                                IntermediateCode.ObjectEquality(compareResult, (Address) conditionAddress,
                                    (Address) caseAddress));
                            break;
                        default:
                            throw new Exception("未知类型");
                    }

                    // 如果相等，则跳转到对应代码块
                    var jumpToCase = IntermediateCode.UnpatchedJumpIfTrue((Address) compareResult);
                    existCodes.Add(jumpToCase);
                    jumpToCases[switchCase] = jumpToCase;
                }
                else
                {
                    hasDefaultCase = true;
                }
            }

            // 所有case失配的情况
            // 如果有default，则回填至default开始
            // 否则回填至break出块
            var defaultJump = IntermediateCode.UnpatchedJump();
            existCodes.Add(defaultJump);

            // 开始写入各个Case的代码

            foreach (var switchCase in _cases)
            {
                if (switchCase.CaseExpression != null)
                {
                    // 回填跳转指令
                    IntermediateCode.PatchJump(existCodes.Count, jumpToCases[switchCase]);
                }
                else
                {
                    // 回填default跳转指令
                    IntermediateCode.PatchJump(existCodes.Count, defaultJump);
                }

                // 写入代码
                foreach (var statement in switchCase.Statements)
                {
                    statement.AppendCodes(existCodes);
                }
            }

            // 如果没有定义default，则回填default为break离块
            if (!hasDefaultCase)
            {
                Block.RegisterBreak(new LeaveBlockBackPatchTask(
                    code: defaultJump,
                    targets: LeaveBlockTarget.SpecificQuantity(1)
                ));
            }
        }
    }

    public class SwitchCase
    {
        /// <summary>
        /// case的条件，为null则代表default
        /// </summary>
        public IGorgeValueExpression CaseExpression;

        public List<IStatement> Statements;
    }
}