using Antlr4.Runtime;

namespace Gorge.GorgeCompiler.Exceptions
{
    /// <summary>
    /// 代码位置
    /// 表示一个具体源文件位置与代码区间
    /// </summary>
    public record CodeLocation
    {
        /// <summary>
        /// 源文件名
        /// 表示发生故障的源文件名称
        /// </summary>
        public readonly string SourceName;

        /// <summary>
        /// 代码区间
        /// 表示出错代码在源文件中的具体区间
        /// </summary>
        public readonly CodeRange CodeRange;

        public CodeLocation(string sourceName, CodeRange codeRange)
        {
            SourceName = sourceName;
            CodeRange = codeRange;
        }

        public CodeLocation((string sourceName, CodeRange codeRange) values) : this(values.sourceName,
            values.codeRange)
        {
        }

        private static (string, CodeRange) Convert(IToken startToken, IToken endToken)
        {
            var sourceName = startToken.TokenSource.SourceName;
            if (sourceName != endToken.TokenSource.SourceName)
            {
                throw new GorgeCompilerException("尝试使用来自不同源文件的两个词法单元创建代码区间");
            }

            return (sourceName, new CodeRange(startToken.Start(), endToken.End()));
        }

        public CodeLocation(IToken startToken, IToken endToken) : this(Convert(startToken, endToken))
        {
        }

        public CodeLocation(ParserRuleContext context) : this(context.Start, context.Stop)
        {
        }

        public static implicit operator CodeLocation(ParserRuleContext context)
        {
            return new CodeLocation(context);
        }

        public static implicit operator CodeLocation((IToken startToken, IToken endToken) range)
        {
            return new CodeLocation(range.startToken, range.endToken);
        }

        public override string ToString()
        {
            return $"{SourceName}:{CodeRange}";
        }
    }
}