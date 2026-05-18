using System;
using System.Collections.Generic;
using System.Linq;
using Gorge.GorgeCompiler.CompileContext.Scope;
using Gorge.GorgeCompiler.Statement;
using Gorge.GorgeLanguage.VirtualMachine;

namespace Gorge.GorgeCompiler.CodeBlock
{
    /// <summary>
    /// 普通代码快
    /// {
    ///     ...
    ///     语句
    ///     ...
    /// }
    /// </summary>
    public class NormalBlock : BaseCodeBlock
    {
        private readonly List<IStatement> _statements;

        public NormalBlock(bool isElse, List<IStatement> statements, CodeBlockScope block) : base(isElse, block)
        {
            _statements = statements;
        }

        public override CodeBlockType Type => CodeBlockType.Normal;

        protected override void AppendBlockContentCodes(List<IntermediateCode> existCodes)
        {
            foreach (var statement in _statements)
            {
                statement.AppendCodes(existCodes);
            }
        }
    }
}