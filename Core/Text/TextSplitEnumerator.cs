using System.Runtime.CompilerServices;

namespace Jay.SourceGen.Text;

public ref struct TextSplitEnumerator //: IEnumerator<ReadOnlySpan<char>>, IEnumerator
{
    private int _position = 0;
    private ReadOnlySpan<char> _currentTextSlice = default;
    private Range _currentRange = default;

    private readonly ReadOnlySpan<char> _inputText;
    private readonly ReadOnlySpan<char> _separator;
    private readonly TextSplitOptions _splitOptions;
    private readonly StringComparison _stringComparison;

    /// <inheritdoc cref="IEnumerator{T}"/>
    public ReadOnlySpan<char> Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _currentTextSlice;
    }

    public ReadOnlySpan<char> CharSpan => _currentTextSlice;
    public string String => _currentTextSlice.ToString();
    public Range Range => _currentRange;

    public TextSplitEnumerator(TextSplitEnumerable splitEnumerable)
    {
        _inputText = splitEnumerable.InputText;
        _separator = splitEnumerable.Separator;
        _splitOptions = splitEnumerable.SplitOptions;
        _stringComparison = splitEnumerable.StringComparison;
    }

    /// <inheritdoc cref="IEnumerator"/>
    public bool MoveNext()
    {
        int inputTextLen = _inputText.Length;
        // inclusive start index
        int sliceStart;
        // exclusive end index
        int sliceEnd;

        while (true)
        {
            sliceStart = _position;

            // After the end = done enumerating
            if (sliceStart > inputTextLen)
            {
                _currentTextSlice = default; // clear after enumeration ends
                _currentRange = default;
                return false;
            }
            // If our position is at the end, we might need to yield the last bit
            else if (sliceStart == inputTextLen)
            {
                // Finish enumeration                 
                _position = sliceStart + 1;
                if (!_splitOptions.HasFlag(TextSplitOptions.RemoveEmptyLines))
                {
                    // Empty
                    _currentTextSlice = ReadOnlySpan<char>.Empty;
                    _currentRange = new Range(start: sliceStart, end: sliceStart);
                    return true;
                }
                else
                {
                    // clear
                    _currentTextSlice = default;
                    _currentRange = default;
                    return false;
                }
            }
            else
            {
                // Scan for next separator
                var separatorIndex = _inputText.NextIndexOf(
                    _separator,
                    _position,
                    _stringComparison
                );
                // None found or an empty separator yield the original
                if (separatorIndex == -1 || _separator.Length == 0)
                {
                    // End of slice is end of text
                    sliceEnd = _inputText.Length;
                    // We're done enumerating
                    _position = sliceEnd + 1;
                }
                else
                {
                    // This slice ends where the separator starts
                    sliceEnd = separatorIndex;
                    // We'll start again where the separator ends
                    _position = sliceEnd + _separator.Length;
                }

                // Respect StringSplitOptions
                if (_splitOptions.HasFlag(TextSplitOptions.TrimLines))
                {
                    // Copied from ReadOnlySpan<char>.Trim()
                    for (; sliceStart < sliceEnd; sliceStart++)
                    {
                        if (!char.IsWhiteSpace(_inputText[sliceStart]))
                        {
                            break;
                        }
                    }

                    for (; sliceEnd > sliceStart; sliceEnd--)
                    {
                        if (!char.IsWhiteSpace(_inputText[(sliceEnd - 1)]))
                        {
                            break;
                        }
                    }
                }

                _currentRange = new Range(
                    /* inclusive */start: sliceStart,
                    /* exclusive */end: sliceEnd
                );

                // Respect StringSplitOptions
                _currentTextSlice = _inputText[_currentRange];
                if (_currentTextSlice.Length > 0 || !_splitOptions.HasFlag(TextSplitOptions.RemoveEmptyLines))
                {
                    // This is a valid return slice
                    return true;
                }
                // We're not going to return this slice, told not to

                // _position has been updated, start the next scan
            }
        }
    }

    /// <inheritdoc cref="IEnumerator"/>
    public void Reset()
    {
        _position = 0;
        _currentTextSlice = default;
    }
}
