using System.Collections.Generic;
using System.Linq;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.CompileContext.Symbol;

namespace Gorge.GorgeCompiler.Exceptions.CompileException
{
    /// <summary>
    /// 继承多个类的异常
    /// </summary>
    public class CircularInheritanceException : GorgeCompilerException
    {
        /// <summary>
        /// 生成错误信息
        /// </summary>
        /// <returns>格式化的错误信息</returns>
        private static string GenerateMessage(List<ClassSymbol> inheritanceCycle)
        {
            return $"存在循环继承{string.Join(" -> ", inheritanceCycle.Select(s => s.Identifier))}:";
        }

        public CircularInheritanceException(List<ClassSymbol> inheritanceCycle) : base(
            GenerateMessage(inheritanceCycle),
            inheritanceCycle.SelectMany(s => s.DefinitionToken).ToArray())
        {
        }
    }
}