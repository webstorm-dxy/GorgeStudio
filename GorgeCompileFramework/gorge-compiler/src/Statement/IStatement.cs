using System.Collections.Generic;
using Antlr4.Runtime;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.Statement
{
    public interface IStatement
    {
        public CodeBlockScope Block { get; }

        /// <summary>
        /// 增量翻译，将本语句的中间代码追加到已有代码的后方
        /// </summary>
        /// <param name="existCodes">已有代码表</param>
        public void AppendCodes(List<IntermediateCode> existCodes);

        public ParserRuleContext AntlrContext { get; }
    }
}