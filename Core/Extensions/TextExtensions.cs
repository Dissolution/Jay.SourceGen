
namespace Jay.SourceGen.Extensions;

public static class TextExtensions
{
    public static bool StartsWith(this ReadOnlySpan<char> text, char ch)
    {
        return text.Length > 0 && text[0] == ch;
    }
    public static bool StartsWith(this ReadOnlySpan<char> text, Func<char, bool> predicate)
    {
        return text.Length > 0 && predicate(text[0]);
    }

    public static bool EndsWith(this ReadOnlySpan<char> text, char ch)
    {
        return text.Length > 0 && text[^1] == ch;
    }
    public static bool EndsWith(this ReadOnlySpan<char> text, Func<char, bool> predicate)
    {
        return text.Length > 0 && predicate(text[^1]);
    }

    public static int NextIndexOf(this ReadOnlySpan<char> text, char ch, int startIndex)
    {
        if ((uint)startIndex >= text.Length)
            return -1;
        var sliceIndex = text.Slice(startIndex).IndexOf(ch);
        if (sliceIndex == -1)
            return -1;
        return sliceIndex + startIndex;
    }

    public static int NextIndexOf(
        this ReadOnlySpan<char> text,
        ReadOnlySpan<char> searchText,
        int startIndex,
        StringComparison comparison = StringComparison.Ordinal
    )
    {
        if ((uint)startIndex >= text.Length)
            return -1;
        var sliceIndex = text.Slice(startIndex).IndexOf(searchText, comparison);
        if (sliceIndex == -1)
            return -1;
        return sliceIndex + startIndex;
    }

    public static int NextIndexOf(this Span<char> text, char ch, int startIndex) =>
        ((ReadOnlySpan<char>)text).NextIndexOf(ch, startIndex);

    public static int NextIndexOf(
        this Span<char> text,
        ReadOnlySpan<char> searchText,
        int startIndex,
        StringComparison comparison = StringComparison.Ordinal
    ) => ((ReadOnlySpan<char>)text).NextIndexOf(searchText, startIndex, comparison);

    public static int PreviousIndexOf(this ReadOnlySpan<char> text, char ch, int startIndex)
    {
        if ((uint)startIndex > text.Length)
            return -1;
        var sliceLastIndex = text.Slice(0, startIndex).LastIndexOf(ch);
        if (sliceLastIndex == -1)
            return -1;
        return sliceLastIndex;
    }

    public static int PreviousIndexOf(
        this ReadOnlySpan<char> text,
        ReadOnlySpan<char> searchText,
        int startIndex,
        StringComparison comparison = StringComparison.Ordinal
    )
    {
        if ((uint)startIndex > text.Length)
            return -1;
        int sliceLastIndex;
#if NET7_0_OR_GREATER
        sliceLastIndex = text.Slice(0, startIndex).LastIndexOf(searchText, comparison);
#else
        if (comparison == StringComparison.Ordinal)
        {
            sliceLastIndex = text.Slice(0, startIndex).LastIndexOf(searchText);
        }
        else
        {
            // Ugh
            sliceLastIndex = text.ToString()
                .LastIndexOf(searchText.ToString(), startIndex, comparison);
        }
#endif
        if (sliceLastIndex == -1)
            return -1;
        return sliceLastIndex;
    }

    public static int PreviousIndexOf(this Span<char> text, char ch, int startIndex) =>
        ((ReadOnlySpan<char>)text).PreviousIndexOf(ch, startIndex);

    public static int PreviousIndexOf(
        this Span<char> text,
        ReadOnlySpan<char> searchText,
        int startIndex,
        StringComparison comparison = StringComparison.Ordinal
    ) => ((ReadOnlySpan<char>)text).PreviousIndexOf(searchText, startIndex, comparison);

  
}
