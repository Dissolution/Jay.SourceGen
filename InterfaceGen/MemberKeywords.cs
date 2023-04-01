namespace Jay.SourceGen.InterfaceGen;

[Flags]
public enum MemberKeywords
{
    None = 0,
    Virtual = 1 << 0,
    Abstract = 1 << 1,
    Sealed = 1 << 2,
    Partial = 1 << 3,
}

internal static class MemberKeywordsExtensions
{
    public static void DeclareTo(this MemberKeywords keywords, CodeBuilder codeBuilder)
    {
        codeBuilder.Enumerate(
            keywords.GetFlags(),
            static (cb, v) => cb.Append(v.ToString().ToLower()).Append(' '));
    }
}
