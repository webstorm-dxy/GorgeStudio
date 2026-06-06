using System.Text;

namespace GorgeStudio.Services.CodeGeneration;

public static class StringBuilderExtension
{
    public static void AppendLine(this StringBuilder stringBuilder, string content, int indentation)
    {
        for (var i = 0; i < indentation; i++)
            stringBuilder.Append("    ");
        stringBuilder.AppendLine(content);
    }

    public static void Append(this StringBuilder stringBuilder, string content, int indentation)
    {
        for (var i = 0; i < indentation; i++)
            stringBuilder.Append("    ");
        stringBuilder.Append(content);
    }
}
