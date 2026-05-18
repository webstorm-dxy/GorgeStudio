using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions.CompileException;
using Gorge.GorgeCompiler.Expression.Tools;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.ConditionalLevel
{
    /// <summary>
    /// 三元条件表达式：? :
    /// 要求操作数1是bool，操作数2和3类型相同，或一个int一个float
    /// 结果是操作数2和3的类型
    /// </summary>
    public class ConditionalExpression : BaseValueExpression
    {
        private readonly IGorgeValueExpression _condition;
        private readonly IGorgeValueExpression _caseTrue;
        private readonly IGorgeValueExpression _caseFalse;

        public ConditionalExpression(IGorgeValueExpression condition, IGorgeValueExpression caseTrue,
            IGorgeValueExpression caseFalse,
            CodeBlockScope context, ParserRuleContext antlrContext) : base(context, antlrContext)
        {
            // 判定返回值类型并执行类型检查
            if (condition.ValueType.BasicType != BasicType.Bool)
            {
                throw new Exception(
                    $"三元条件操作符的第一操作数类型必须为bool，但实际第一操作数类型为{condition.ValueType}");
            }

            switch (caseTrue.ValueType.BasicType)
            {
                case BasicType.Int:
                    ValueType = caseFalse.ValueType.BasicType switch
                    {
                        BasicType.Int => SymbolicGorgeType.Int,
                        BasicType.Float => SymbolicGorgeType.Float,
                        _ => throw new Exception(
                            $"三元条件操作符的第二和第三操作数类型必须一致，但实际第二操作数类型为{caseTrue.ValueType}，第三操作数类型为{caseFalse.ValueType}")
                    };
                    break;
                case BasicType.Float:
                    ValueType = caseFalse.ValueType.BasicType switch
                    {
                        BasicType.Int or BasicType.Float => SymbolicGorgeType.Float,
                        _ => throw new Exception(
                            $"三元条件操作符的第二和第三操作数类型必须一致，但实际第二操作数类型为{caseTrue.ValueType}，第三操作数类型为{caseFalse.ValueType}")
                    };
                    break;
                case BasicType.Bool:
                case BasicType.String:
                case BasicType.Object:
                default:
                    if (caseTrue.ValueType.CanAutoCastTo(caseFalse.ValueType))
                    {
                        ValueType = caseFalse.ValueType;
                    }
                    else if (caseFalse.ValueType.CanAutoCastTo(caseTrue.ValueType))
                    {
                        ValueType = caseTrue.ValueType;
                    }
                    else
                    {
                        throw new GorgeCompileException(
                            $"三元条件操作符的第二和第三操作数类型必须一致，但实际第二操作数类型为{caseTrue.ValueType}，第三操作数类型为{caseFalse.ValueType}",
                            ExpressionLocation);
                    }

                    break;
            }

            _condition = condition;
            _caseTrue = caseTrue;
            _caseFalse = caseFalse;
            IsCompileConstant =
                condition.IsCompileConstant && caseTrue.IsCompileConstant && caseFalse.IsCompileConstant;
            if (IsCompileConstant)
            {
                if ((bool) condition.CompileConstantValue)
                {
                    CompileConstantValue = caseTrue.CompileConstantValue;
                }
                else
                {
                    CompileConstantValue = caseFalse.CompileConstantValue;
                }
            }
        }

        public override SymbolicGorgeType ValueType { get; }

        protected override SymbolicAddress AppendNotConstantCodes(CodeBlockScope codeBlockScope,
            List<IntermediateCode> existCodes)
        {
            if (_condition.IsCompileConstant) // 如果条件表达式是常量，则不追加另一半代码
            {
                if ((bool) _condition.CompileConstantValue)
                {
                    return CommonImmediateCodes.AppendAutoCastCode(Block, _caseTrue.AppendCodes(Block, existCodes),
                        ValueType, existCodes);
                }

                return CommonImmediateCodes.AppendAutoCastCode(Block, _caseFalse.AppendCodes(Block, existCodes),
                    ValueType, existCodes);
            }

            // 子表达式返回地址
            var conditionResultAddress = _condition.AppendCodes(Block, existCodes);
            var conditionalJump = IntermediateCode.UnpatchedJumpIfFalse((Address)conditionResultAddress);
            existCodes.Add(conditionalJump);

            var resultAddress = Block.AddTempVariable(ValueType);

            // true分支
            var trueResult =
                CommonImmediateCodes.AppendAutoCastCode(Block, _caseTrue.AppendCodes(Block, existCodes), ValueType,
                    existCodes);
            existCodes.Add(IntermediateCode.LocalAssign(resultAddress, (Address)trueResult));
            var forceJump = IntermediateCode.UnpatchedJump();
            existCodes.Add(forceJump);
            // 回填条件跳转地址
            IntermediateCode.PatchJump(existCodes.Count, conditionalJump);

            // false分支
            var falseResult = CommonImmediateCodes.AppendAutoCastCode(Block, _caseFalse.AppendCodes(Block, existCodes),
                ValueType, existCodes);
            existCodes.Add(IntermediateCode.LocalAssign(resultAddress, (Address)falseResult));

            // 回填强制跳转地址
            IntermediateCode.PatchJump(existCodes.Count, forceJump);

            return resultAddress;
        }

        public override bool IsCompileConstant { get; }
        public override object CompileConstantValue { get; }
    }
}