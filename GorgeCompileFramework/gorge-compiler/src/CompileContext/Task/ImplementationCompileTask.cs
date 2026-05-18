using System.Collections.Generic;
using Gorge.GorgeCompiler.Exceptions.CompileException;

namespace Gorge.GorgeCompiler.CompileContext.Task
{
    public interface IImplementationCompileTask
    {
        public List<GorgeCompileException> PanicExceptions { get; }
        public void DoCompile(ClassImplementationContext compileContext, bool panicMode, bool generateCode);
    }
}