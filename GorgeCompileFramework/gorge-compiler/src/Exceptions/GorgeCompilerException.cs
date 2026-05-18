using System;
using Gorge.GorgeCompiler.Exceptions.CompileException;

namespace Gorge.GorgeCompiler.Exceptions
{
    /// <summary>
    /// Gorge编译器异常
    /// 用来记录编译器内部异常
    /// </summary>
    public class GorgeCompilerException : Exception
    {
        public GorgeCompilerException(string message, params CodeLocation[] positions) : base(
            GorgeCompileException.GenerateMessage(message, positions))
        {
        }
    }
}