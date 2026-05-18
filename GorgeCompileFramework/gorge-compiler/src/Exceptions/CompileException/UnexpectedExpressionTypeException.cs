using System;

namespace Gorge.GorgeCompiler.Exceptions.CompileException
{
    /// <summary>
    /// 表达式类型错误异常
    /// </summary>
    public class UnexpectedExpressionTypeException : GorgeCompileException
    {
        /// <summary>
        /// 生成错误信息
        /// </summary>
        /// <returns>格式化的错误信息</returns>
        private static string GenerateMessage(Type expectedExpressionType, Type actualExpressionType)
        {
            return $"表达式错误，期望{expectedExpressionType}，实际{actualExpressionType}";
        }

        public UnexpectedExpressionTypeException(Type expectedExpressionType, Type actualExpressionType,
            CodeLocation position) : base(GenerateMessage(expectedExpressionType, actualExpressionType), position)
        {
        }
    }

    /// <summary>
    /// 类型错误异常
    /// </summary>
    public class UnexpectedGorgeTypeException : GorgeCompileException
    {
        /// <summary>
        /// 生成错误信息
        /// </summary>
        /// <returns>格式化的错误信息</returns>
        private static string GenerateMessage(Type expectedType, Type actualType)
        {
            return $"类型错误，期望{expectedType}，实际{actualType}";
        }

        public UnexpectedGorgeTypeException(Type expectedType, Type actualType, CodeLocation position) : base(
            GenerateMessage(expectedType, actualType), position)
        {
        }
    }
    
    /// <summary>
    /// 符号类型错误异常
    /// </summary>
    public class UnexpectedSymbolTypeException : GorgeCompilerException
    {
        /// <summary>
        /// 生成错误信息
        /// </summary>
        /// <returns>格式化的错误信息</returns>
        private static string GenerateMessage(Type expectedType, Type actualType)
        {
            return $"符号类型错误，期望{expectedType}，实际{actualType}";
        }

        public UnexpectedSymbolTypeException(Type expectedType, Type actualType, CodeLocation position) : base(
            GenerateMessage(expectedType, actualType), position)
        {
        }
    }
}