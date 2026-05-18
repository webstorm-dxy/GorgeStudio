using Gorge.GorgeCompiler.Exceptions;

namespace Gorge.GorgeCompiler.Expression
{
    /// <summary>
    ///  不追加代码的表达式
    /// </summary>
    public abstract class ExpressionBase : IExpression
    {
        protected ExpressionBase(CodeLocation expressionLocation)
        {
            ExpressionLocation = expressionLocation;
        }

        public CodeLocation ExpressionLocation { get; }
        public ExpressionValueType ExpressionValueType { get; }
    }
}