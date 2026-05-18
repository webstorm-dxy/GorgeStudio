namespace Gorge.GorgeCompiler
{
    public class SourceCodeFile
    {
        public readonly string Path;

        public readonly string Code;

        public readonly bool IsChartSourceCode;

        public SourceCodeFile(string path, string code, bool isChartSourceCode)
        {
            Path = path;
            Code = code;
            IsChartSourceCode = isChartSourceCode;
        }
    }
}