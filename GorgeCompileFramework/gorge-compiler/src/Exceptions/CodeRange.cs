using Antlr4.Runtime;

namespace Gorge.GorgeCompiler.Exceptions
{
    /// <summary>
    /// 代码区间
    /// 描述源文件中的一段代码的具体区间
    /// 包括起始位置和结束位置
    /// </summary>
    public record CodeRange
    {
        /// <summary>
        /// 起始位置
        /// 表示代码段的开始位置（包含）
        /// </summary>
        public readonly CodePosition Start;

        /// <summary>
        /// 结束位置
        /// 表示代码段的结束位置（不包含）
        /// </summary>
        public readonly CodePosition End;

        public CodeRange(CodePosition start, CodePosition end)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// 检查指定的位置是否在本代码段内
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns></returns>
        public bool Contains(CodePosition position)
        {
            return position >= Start && position < End;
        }

        /// <summary>
        /// 检查指定的位置是否在本代码段内
        /// </summary>
        /// <param name="line">行号</param>
        /// <param name="column">列号</param>
        /// <returns></returns>
        public bool Contains(int line, int column)
        {
            var position = new CodePosition(line, column);
            return Contains(position);
        }

        public override string ToString()
        {
            return $"[{Start} - {End}]";
        }

        public static implicit operator CodeRange(ParserRuleContext context)
        {
            return new CodeRange(context.Start.Start(), context.Stop.End());
        }
    }
}