namespace Gorge.GorgeCompiler.Visitors
{
    public static class LiteralHelper
    {
        /// <summary>
        /// 处理字符串字面量的转义
        /// </summary>
        /// <param name="stringLiteral"></param>
        /// <returns></returns>
        public static string StringLiteralToString(string stringLiteral)
        {
            var result = stringLiteral;
            result = result.Substring(1, result.Length - 2);
            result = result.Replace(@"\n", "\n");
            result = result.Replace(@"\r", "\r");
            result = result.Replace("\\\"", "\"");
            result = result.Replace(@"\\", @"\");
            return result;
        }

        /// <summary>
        /// 字符串转换为字符串字面量
        /// </summary>
        /// <param name="fromString"></param>
        /// <returns></returns>
        public static string StringToStringLiteral(string? fromString)
        {
            if (fromString == null)
            {
                return null;
            }

            var result = fromString;
            result = result.Replace(@"\", @"\\");
            result = result.Replace("\"", "\\\"");
            result = result.Replace("\r", @"\r");
            result = result.Replace("\n", @"\n");
            result = "\"" + result + "\"";
            return result;
        }
    }
}