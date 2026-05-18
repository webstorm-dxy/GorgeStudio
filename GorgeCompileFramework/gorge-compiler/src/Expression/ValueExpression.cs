#nullable enable
using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;
using Gorge.GorgeLanguage.Objective;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression
{
    public interface IGorgeValueExpression : IExpression
    {
        /// <summary> 
        /// 本表达式的值类型
        /// </summary>
        public SymbolicGorgeType ValueType { get; }

        /// <summary>
        /// 本表达式是否为编译时常量
        /// </summary>
        public bool IsCompileConstant { get; }

        /// <summary>
        /// 本表达式的编译时常量值
        /// </summary>
        public object? CompileConstantValue { get; }

        /// <summary>
        /// 向代码表中追加本表达式的代码
        /// </summary>
        /// <param name="codeBlockScope"></param>
        /// <param name="existCodes"></param>
        /// <returns></returns>
        public SymbolicAddress AppendCodes(CodeBlockScope codeBlockScope, List<IntermediateCode> existCodes);
    }

    /// <summary>
    /// 有值表达式
    /// 自动生成返回值地址
    /// </summary>
    public abstract class ValueExpression : ExpressionBase, IGorgeValueExpression
    {
        protected ValueExpression(CodeBlockScope block, CodeLocation expressionLocation) : base(expressionLocation)
        {
            Block = block;
        }

        public abstract SymbolicAddress AppendCodes(CodeBlockScope codeBlockScope, List<IntermediateCode> existCodes);

        /// <summary> 
        /// 本表达式的值类型
        /// </summary>
        public abstract SymbolicGorgeType ValueType { get; }

        /// <summary>
        /// 本表达式是否为编译时常量
        /// </summary>
        public abstract bool IsCompileConstant { get; }

        /// <summary>
        /// 本表达式的编译时常量值
        /// </summary>
        public abstract object? CompileConstantValue { get; }

        protected CodeBlockScope Block { get; }

        /// <summary>
        /// 将编译时常量代码追加到值地址中
        /// </summary>
        /// <param name="existCodes"></param>
        /// <returns>值地址</returns>
        protected SymbolicAddress AppendCompileValueConstantToResult(List<IntermediateCode> existCodes)
        {
            existCodes.Add(ValueType.BasicType switch
            {
                BasicType.Int or BasicType.Enum => IntermediateCode.LocalIntAssign(ValueAddress,
                    (int) CompileConstantValue),
                BasicType.Float => IntermediateCode.LocalFloatAssign(ValueAddress,
                    (float) CompileConstantValue),
                BasicType.Bool =>
                    IntermediateCode.LocalBoolAssign(ValueAddress, (bool) CompileConstantValue),
                BasicType.String => IntermediateCode.LocalStringAssign(ValueAddress,
                    (string) CompileConstantValue),
                BasicType.Object or BasicType.Delegate or BasicType.Interface => IntermediateCode.LocalObjectAssign(
                    ValueAddress, (GorgeObject) CompileConstantValue),
                _ => throw new ArgumentOutOfRangeException()
            });

            return ValueAddress;
        }

        #region 表达式值地址自动生成

        /// <summary>
        /// 表达式值地址
        /// 首次访问时根据表达式值类型自动生成，重复访问结果一致
        /// </summary>
        protected SymbolicAddress ValueAddress
        {
            get
            {
                if (_isResultAddressGenerated)
                {
                    return _resultAddress;
                }

                _resultAddress = Block.AddTempVariable(ValueType);
                _isResultAddressGenerated = true;
                return _resultAddress;
            }
        }

        private bool _isResultAddressGenerated = false;
        private SymbolicAddress _resultAddress;

        #endregion
    }

    /// <summary>
    /// 有值表达式
    /// 处理了编译时常量情况，直接在代码中写入常量值
    /// </summary>
    public abstract class BaseValueExpression : ValueExpression
    {
        protected BaseValueExpression(CodeBlockScope block, ParserRuleContext antlrContext) : base(block,
            antlrContext)
        {
        }

        /// <summary>
        /// 增量翻译，将本表达式代码追加到已有代码的后方
        /// 不需要考虑编译时常量的情况
        /// </summary>
        /// <param name="codeBlockScope"></param>
        /// <param name="existCodes"></param>
        /// <returns></returns>
        protected abstract SymbolicAddress AppendNotConstantCodes(CodeBlockScope codeBlockScope,
            List<IntermediateCode> existCodes);

        public sealed override SymbolicAddress AppendCodes(CodeBlockScope codeBlockScope,
            List<IntermediateCode> existCodes)
        {
            // 处理常量情况，直接赋值
            if (IsCompileConstant)
            {
                return AppendCompileValueConstantToResult(existCodes);
            }

            return AppendNotConstantCodes(codeBlockScope, existCodes);
        }
    }

    /// <summary>
    /// 常量表达式，必然是编译时常量
    /// </summary>
    public abstract class ConstantValueExpression : ValueExpression
    {
        protected ConstantValueExpression(CodeBlockScope block, ParserRuleContext antlrContext) : base(block,
            antlrContext)
        {
        }

        public override SymbolicAddress AppendCodes(CodeBlockScope codeBlockScope, List<IntermediateCode> existCodes)
        {
            return AppendCompileValueConstantToResult(existCodes);
        }

        public sealed override bool IsCompileConstant => true;
    }

    /// <summary>
    /// 必然是动态值的有值表达式
    /// </summary>
    public abstract class DynamicValueExpression : ValueExpression
    {
        protected DynamicValueExpression(CodeBlockScope block, CodeLocation expressionLocation) : base(block,
            expressionLocation)
        {
        }

        public sealed override bool IsCompileConstant => false;
        public sealed override object CompileConstantValue => null;
    }
}