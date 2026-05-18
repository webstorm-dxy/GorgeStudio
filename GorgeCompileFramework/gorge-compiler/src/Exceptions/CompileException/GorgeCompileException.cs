using System;
using System.Collections.Generic;
using System.Text;

namespace Gorge.GorgeCompiler.Exceptions.CompileException
{
    /// <summary>
    /// Gorge语言编译异常
    /// </summary>
    public class GorgeCompileException : Exception
    {
        public readonly string CompileMessage;
        public readonly CodeLocation[] Positions;

        public static string GenerateMessage(string message, CodeLocation[] positions)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(message);
            if (positions.Length > 0)
            {
                stringBuilder.AppendLine("\n位于：");
                foreach (var position in positions)
                {
                    if (position == null)
                    {
                        continue;
                    }
                    stringBuilder.AppendLine(position.ToString());
                }
            }

            return stringBuilder.ToString();
        }

        public GorgeCompileException(string compileMessage, params CodeLocation[] positions) : base(
            GenerateMessage(compileMessage, positions))
        {
            CompileMessage = compileMessage;
            Positions = positions;
        }
        
        public GorgeCompileException(string compileMessage, List<CodeLocation> positions) : this(compileMessage,positions.ToArray())
        {
        }
    }
}