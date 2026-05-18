using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Expression.Tools;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Expression.AssignmentTarget
{
    /// <summary>
    /// 数组访问在赋值左侧
    ///   a[b]
    /// </summary>
    public class ArrayAccess : AssignmentTargetExpression
    {
        private readonly AssignmentTargetExpression _arrayOperand;
        private readonly IGorgeValueExpression _index;
        public override SymbolicGorgeType ValueType { get; }

        public override int FieldIndex => default;
        public override AssignmentTargetType AssignmentTargetType { get; }
        public override SymbolicGorgeType AssignType { get; }
        public override Address DynamicAccessorAddress { get; }

        private readonly Address _indexAddress;

        public ArrayAccess(AssignmentTargetExpression arrayOperand, IGorgeValueExpression index, CodeBlockScope block,
            ParserRuleContext antlrContext) : base(block, antlrContext)
        {
            _arrayOperand = arrayOperand;
            _index = index;
            AssignmentTargetType = AssignmentTargetType.Array;

            _indexAddress = block.AddTempVariable(SymbolicGorgeType.Int);
            DynamicAccessorAddress = _indexAddress;

            ValueType = arrayOperand.AssignType;

            AssignType = arrayOperand.AssignType.Assert<ArrayType>(arrayOperand.ExpressionLocation).ItemType;
        }

        protected override SymbolicAddress AppendNotConstantCodes(CodeBlockScope codeBlockScope,
            List<IntermediateCode> existCodes)
        {
            var arrayAddress = _arrayOperand.AppendCodes(Block, existCodes);
            var indexAddress = _index.AppendCodes(Block, existCodes);

            // 将index复制到预申请的地址中
            existCodes.Add(IntermediateCode.LocalIntAssign(_indexAddress, (Address)indexAddress));

            // 输出数组对象地址
            switch (_arrayOperand.AssignmentTargetType)
            {
                case AssignmentTargetType.This:
                case AssignmentTargetType.LocalVariable:
                    return arrayAddress;
                case AssignmentTargetType.Field:
                    // 取出数组对象
                    existCodes.Add(IntermediateCode.LoadObjectField(ValueAddress, (Address)arrayAddress,
                        _arrayOperand.FieldIndex));
                    return ValueAddress;
                case AssignmentTargetType.InjectorField:
                    // 取出本级字段所在对象
                    existCodes.Add(IntermediateCode.LoadInjectorField(ValueAddress, (Address)arrayAddress,
                        _arrayOperand.FieldIndex));
                    return ValueAddress;
                case AssignmentTargetType.Array:
                    // 取出本级字段所在对象
                    CommonImmediateCodes.AppendArrayGet(ValueAddress, existCodes, (Address)arrayAddress,
                        _arrayOperand.DynamicAccessorAddress);
                    return ValueAddress;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}