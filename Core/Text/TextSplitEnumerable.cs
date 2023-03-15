namespace Jay.SourceGen.Text;

public readonly ref struct TextSplitEnumerable // : IEnumerable<ReadOnlySpan<char>>, IEnumerable
{
    public readonly ReadOnlySpan<char> InputText;
    public readonly ReadOnlySpan<char> Separator;
    public readonly TextSplitOptions SplitOptions;
    public readonly StringComparison StringComparison;

    public TextSplitEnumerable(
        ReadOnlySpan<char> inputText,
        ReadOnlySpan<char> separator,
        TextSplitOptions splitOptions = TextSplitOptions.None,
        StringComparison stringComparison = StringComparison.Ordinal
    )
    {
        InputText = inputText;
        Separator = separator;
        SplitOptions = splitOptions;
        StringComparison = stringComparison;
    }

    /// <inheritdoc cref="IEnumerable{T}"/>
    public TextSplitEnumerator GetEnumerator()
    {
        return new TextSplitEnumerator(this);
    }
}
