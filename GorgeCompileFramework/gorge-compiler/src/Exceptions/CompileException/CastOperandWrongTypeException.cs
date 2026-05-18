using Gorge.GorgeCompiler.CompileContext.Symbol;

namespace Gorge.GorgeCompiler.Exceptions.CompileException
{
    /// <summary>
    /// 强制类型转换异常
    /// </summary>
    public class CastOperandWrongTypeException : GorgeCompileException
    {
        private static string BuildMessage(SymbolicGorgeType castFrom, SymbolicGorgeType castTo)
        {
            return $"无法从{castFrom}强制类型转换为{castTo}";
        }

        public CastOperandWrongTypeException(SymbolicGorgeType castFrom, SymbolicGorgeType castTo,
            params CodeLocation[] positions) : base(BuildMessage(castFrom, castTo), positions)
        {
        }
    }
}