using System.Text;
using Gorge.GorgeLanguage.Objective;

namespace Gorge.GorgeCompiler.Exceptions.CompileException
{
    public class ExpressionOperandWrongTypeException : GorgeCompileException
    {
        private static string BuildMessage(string expressionName, string operandName, BasicType actualType,
            params BasicType[] expectedTypes)
        {
            var expected = new StringBuilder();
            var first = true;
            foreach (var type in expectedTypes)
            {
                if (!first)
                {
                    expected.Append(", ");
                }

                first = false;
                expected.Append(type);
            }

            return $"{expressionName}表达式的{operandName}操作数类型应为{expected}，实为{actualType}";
        }

        public ExpressionOperandWrongTypeException(string expressionName, string operandName, BasicType actualType,
            params BasicType[] expectedTypes) : base(BuildMessage(expressionName, operandName, actualType,
            expectedTypes))
        {
        }
    }
}