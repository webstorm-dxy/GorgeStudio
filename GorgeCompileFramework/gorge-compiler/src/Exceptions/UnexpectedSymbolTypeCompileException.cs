using System;
using System.Collections.Generic;
using Gorge.GorgeCompiler.CompileContext.Symbol;
using Gorge.GorgeCompiler.Exceptions.CompileException;

namespace Gorge.GorgeCompiler.Exceptions
{
    /// <summary>
    /// 符号类型不符合期望的异常
    /// </summary>
    public class UnexpectedSymbolTypeCompileException : GorgeCompileException
    {
        private static string GenerateMessage(SymbolType actualType, SymbolType[] expectedTypes)
        {
            return $"符号类型不符合期望，期望{string.Join(",", expectedTypes)}，实为{actualType}";
        }

        private static string GenerateMessage(Type actualType, Type[] expectedTypes)
        {
            return $"符号类型不符合期望，期望{string.Join<Type>(",", expectedTypes)}，实为{actualType.Name}";
        }

        public UnexpectedSymbolTypeCompileException(List<CodeLocation> position, SymbolType actualType,
            params SymbolType[] expectedTypes) : base(GenerateMessage(actualType, expectedTypes), position.ToArray())
        {
        }

        public UnexpectedSymbolTypeCompileException(CodeLocation position, Type actualType,
            params Type[] expectedTypes) : base(GenerateMessage(actualType, expectedTypes), position)
        {
        }
    }
}