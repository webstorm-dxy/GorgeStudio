using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeCompiler.Expression.PrimaryLevel
{
    /// <summary>
    /// 不含外部引用的Lambda表达式，可为编译时常量
    /// </summary>
    public class StaticLambdaExpression : ConstantValueExpression
    {
        public StaticLambdaExpression(GorgeDelegateImplementation lambdaImplementation,SymbolicGorgeType delegateType, CodeBlockScope block,
            ParserRuleContext antlrContext) : base(block, antlrContext)
        {
            ValueType = delegateType;
            CompileConstantValue = new CompiledGorgeDelegate(lambdaImplementation);
        }

        public override SymbolicGorgeType ValueType { get; }
        public override object CompileConstantValue { get; }
    }
}