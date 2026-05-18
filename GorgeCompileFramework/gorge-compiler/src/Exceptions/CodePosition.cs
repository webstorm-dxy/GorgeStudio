using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Gorge.GorgeCompiler.Exceptions
{
    /// <summary>
    /// 代码位置
    /// 描述源文件中一个字符的具体位置
    /// 包含行号和列号，Antlr标准
    /// </summary>
    public record CodePosition : IComparable<CodePosition>
    {
        /// <summary>
        /// 行号，从1开始
        /// 表示字符所在的行数
        /// </summary>
        public readonly int Line;

        /// <summary>
        /// 列号，从0开始
        /// 表示字符所在的列数，以每一行的开头为起始点
        /// </summary>
        public readonly int Column;

        public CodePosition(int line, int column)
        {
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            return $"({Line},{Column})";
        }

        public int CompareTo(CodePosition other)
        {
            var lineComparison = Line.CompareTo(other.Line);
            if (lineComparison != 0) return lineComparison;
            return Column.CompareTo(other.Column);
        }

        public static bool operator >(CodePosition a, CodePosition b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool operator <(CodePosition a, CodePosition b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator >=(CodePosition a, CodePosition b)
        {
            return a.CompareTo(b) >= 0;
        }

        public static bool operator <=(CodePosition a, CodePosition b)
        {
            return a.CompareTo(b) <= 0;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Line, Column);
        }
    }
}