using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions;

namespace Gorge.GorgeCompiler.Expression
{
    /// <summary>
    /// 引用表达式
    /// </summary>
    public abstract class ReferenceExpression<TSymbol> : ExpressionBase where TSymbol : ISymbol
    {
        public TSymbol Symbol { get; }

        protected ReferenceExpression(TSymbol symbol, CodeLocation expressionLocation) : base(expressionLocation)
        {
            Symbol = symbol;
        }
    }
}