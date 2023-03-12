namespace Jay.SourceGen.Text;

public static class WrittenExtensions
{
    public static CodeBuilder TrimStart(this CodeBuilder textBuilder)
    {
        int i = 0;
        while (i < textBuilder.Length && char.IsWhiteSpace(textBuilder[i]))
            i++;
        if (i > 0)
        {
            TextHelper.CopyTo(textBuilder.Written[i..], textBuilder.Written);
            textBuilder.Length -= i;
        }
        return textBuilder;
    }
    
    public static CodeBuilder TrimEnd(this CodeBuilder textBuilder)
    {
        int e = textBuilder.Length - 1;
        while (e >= 0 && char.IsWhiteSpace(textBuilder[e]))
            e--;
        if (e < textBuilder.Length-1)
        {
            textBuilder.Length = e + 1;
        }
        return textBuilder;
    }

    public static string CurrentNewLineIndent(this CodeBuilder codeBuilder)
    {
        var written = codeBuilder.Written;

        var lastNewLineIndex = written.LastIndexOf<char>(CodeBuilder.DefaultNewLine.AsSpan());
        if (lastNewLineIndex == -1)
            return CodeBuilder.DefaultNewLine;
        return written.Slice(lastNewLineIndex).ToString();
    }
}