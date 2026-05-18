using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Gorge.GorgeCompiler.Exceptions
{
    public static class CodePositionExtension
    {
        /// <summary>
        /// 获取词法单元所在起点（含）
        /// </summary>
        /// <param name="token">词法单元</param>
        /// <returns>词法单元的起始位置（含）</returns>
        public static CodePosition Start(this IToken token)
        {
            return new CodePosition(token.Line, token.Column);
        }

        /// <summary>
        /// 获取词法单元所在终点（不含）
        /// </summary>
        /// <param name="token">词法单元</param>
        /// <returns>词法单元的结束位置（不含）</returns>
        public static CodePosition End(this IToken token)
        {
            // 如果是单行文本，直接返回行内结束位置
            if (!token.Text.Contains('\n') && !token.Text.Contains('\r'))
            {
                return new CodePosition(token.Line, token.Column + token.Text.Length);
            }

            // 处理多行文本
            var lines = token.Text.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None);
            var lastLineIndex = lines.Length - 1;
            var lastLineLength = lines[lastLineIndex].Length;

            // 结束行 = 开始行 + 换行符数量
            var endLine = token.Line + lastLineIndex;

            // 结束列 = 如果是第一行，则为起始列+长度；否则为最后一行长度
            var endColumn = lines.Length == 1 ? token.Column + lastLineLength : lastLineLength;

            return new CodePosition(endLine, endColumn);
        }

        /// <summary>
        /// 获取词法单元对应的代码区间
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static CodeLocation CodeLocation(this IToken token)
        {
            return new CodeLocation(token, token);
        }
        
        /// <summary>
        /// 获取终结符对应的代码区间
        /// </summary>
        /// <param name="terminalNode"></param>
        /// <returns></returns>
        public static CodeLocation CodeLocation(this ITerminalNode terminalNode)
        {
            return terminalNode.Symbol.CodeLocation();
        }

        /// <summary>
        /// 获取词法单元对应的代码区间
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static CodeRange CodeRange(this IToken token)
        {
            return new CodeRange(token.Start(), token.End());
        }
    }
}