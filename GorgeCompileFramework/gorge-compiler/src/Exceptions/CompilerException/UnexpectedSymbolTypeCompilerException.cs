using System;
using System.Collections.Generic;
using Gorge.GorgeCompiler.CompileContext.Symbol;

namespace Gorge.GorgeCompiler.Exceptions.CompilerException
{
    /// <summary>
    /// 符号类型不符合期望的异常
    /// </summary>
    public class UnexpectedSymbolTypeCompilerException : GorgeCompilerException
    {
        private static string GenerateMessage(SymbolType actualType, SymbolType[] expectedTypes)
        {
            return $"符号类型不符合期望，期望{string.Join(",", expectedTypes)}，实为{actualType}";
        }

        private static string GenerateMessage(Type actualType, Type expectedType)
        {
            return $"符号类型不符合期望，期望{expectedType.Name}，实为{actualType.Name}";
        }


        public UnexpectedSymbolTypeCompilerException(List<CodeLocation> position, SymbolType actualType,
            params SymbolType[] expectedTypes) : base(GenerateMessage(actualType, expectedTypes), position.ToArray())
        {
        }

        public UnexpectedSymbolTypeCompilerException(CodeLocation position, SymbolType actualType,
            params SymbolType[] expectedTypes) : this(new List<CodeLocation>() {position}, actualType, expectedTypes)
        {
        }

        public UnexpectedSymbolTypeCompilerException(List<CodeLocation> position, Type actualType,
            Type expectedType) : base(GenerateMessage(actualType, expectedType), position.ToArray())
        {
        }

        public UnexpectedSymbolTypeCompilerException(CodeLocation position, Type actualType,
            Type expectedType) : this(new List<CodeLocation>() {position}, actualType, expectedType)
        {
        }
    }
}