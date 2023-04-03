namespace Jay.SourceGen.InterfaceGen;

internal static class KeywordsExtractor
{
    public static (Visibility, MemberKeywords, ObjType) Parse(string? text)
    {
        Visibility vis = default;
        MemberKeywords keys = default;
        ObjType otype = default;

        var keywords = text.TextSplit(" ", TextSplitOptions.RemoveEmptyLines | TextSplitOptions.TrimLines);
        var e = keywords.GetEnumerator();
        while (e.MoveNext())
        {
            if (Enum.TryParse<Visibility>(e.String, true, out var visibility))
            {
                vis |= visibility;
            }
            else if (Enum.TryParse<MemberKeywords>(e.String, true, out var memberKeywords))
            {
                keys |= memberKeywords;
            }
            else if (Enum.TryParse<ObjType>(e.String, true, out var objType))
            {
                otype |= objType;
            }
            else
            {
                throw new ArgumentException($"Invalid keyword '{e.String}'", nameof(text));
            }
        }
        return (vis, keys, otype);
    }
}