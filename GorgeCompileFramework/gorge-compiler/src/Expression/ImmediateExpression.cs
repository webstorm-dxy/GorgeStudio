using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeCompiler.Expression
{
    /// <summary>
    /// int字面量
    /// </summary>
    public class IntLiteral : ConstantValueExpression
    {
        public override SymbolicGorgeType ValueType => SymbolicGorgeType.Int;
        public override object CompileConstantValue { get; }

        public IntLiteral(int value, CodeBlockScope block, ParserRuleContext antlrContext) : base(block, antlrContext)
        {
            CompileConstantValue = value;
        }
    }

    /// <summary>
    /// float字面量
    /// </summary>
    public class FloatLiteral : ConstantValueExpression
    {
        public override SymbolicGorgeType ValueType => SymbolicGorgeType.Float;
        public override object CompileConstantValue { get; }

        public FloatLiteral(float value, CodeBlockScope block, ParserRuleContext antlrContext) : base(block,
            antlrContext)
        {
            CompileConstantValue = value;
        }
    }

    /// <summary>
    /// bool字面量
    /// </summary>
    public class BoolLiteral : ConstantValueExpression
    {
        public override SymbolicGorgeType ValueType => SymbolicGorgeType.Bool;
        public override object CompileConstantValue { get; }

        public BoolLiteral(bool value, CodeBlockScope block, ParserRuleContext antlrContext) : base(block, antlrContext)
        {
            CompileConstantValue = value;
        }
    }

    /// <summary>
    /// string字面量
    /// </summary>
    public class StringImmediate : ConstantValueExpression
    {
        public override SymbolicGorgeType ValueType => SymbolicGorgeType.String;
        public override object CompileConstantValue { get; }

        public StringImmediate(string value, CodeBlockScope block, ParserRuleContext antlrContext) : base(block,
            antlrContext)
        {
            CompileConstantValue = value;
        }
    }

    /// <summary>
    /// Object字面量
    /// </summary>
    public class ObjectImmediate : ConstantValueExpression
    {
        public override SymbolicGorgeType ValueType { get; }
        public override object CompileConstantValue { get; }

        public readonly ParserRuleContext P;
        
        public ObjectImmediate(GorgeObject immediateValue, SymbolicGorgeType objectType, CodeBlockScope context,
            ParserRuleContext antlrContext) : base(context, antlrContext)
        {
            CompileConstantValue = immediateValue;
            ValueType = objectType;
            P = antlrContext;
        }
    }

    /// <summary>
    /// null字面量
    /// </summary>
    public class NullImmediate : ConstantValueExpression
    {
        public override SymbolicGorgeType ValueType => SymbolicGorgeType.Null;
        public override object CompileConstantValue => null;

        public NullImmediate(CodeBlockScope block, ParserRuleContext antlrContext) : base(block, antlrContext)
        {
        }
    }
}