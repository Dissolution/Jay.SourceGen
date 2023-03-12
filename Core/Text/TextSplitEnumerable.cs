using System.Runtime.CompilerServices;

namespace Jay.SourceGen.Text;

[Flags]
public enum TextSplitOptions
{
    None = 0,
    RemoveEmptyLines = 1 << 0,
    TrimLines = 1 << 1,
}

public readonly ref struct TextSplitList
{
    private readonly ReadOnlySpan<char> _inputText;
    private readonly IReadOnlyList<Range> _ranges;

    public int Count => _ranges.Count;

    internal TextSplitList(ReadOnlySpan<char> inputText, IReadOnlyList<Range> ranges)
    {
        _inputText = inputText;
        _ranges = ranges;
    }

    public Range Range(int index)
    {
        if ((uint)index < _ranges.Count)
        {
            return _ranges[index];
        }
        throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be between 0 and {Count - 1}");
    }

    public ReadOnlySpan<char> Text(int index)
    {
        if ((uint)index < _ranges.Count)
        {
            return _inputText[_ranges[index]];
        }
        throw new ArgumentOutOfRangeException(nameof(index), index, $"Index must be between 0 and {Count - 1}");
    }

    /// <inheritdoc cref="IEnumerable{T}"/>
    public TextSplitListEnumerator GetEnumerator() => new(this);

    public ref struct TextSplitListEnumerator //: IEnumerator<ReadOnlySpan<char>>, IEnumerator
    {
        private readonly ReadOnlySpan<char> _inputText;
        private readonly IReadOnlyList<Range> _ranges;
        private int _position;

        private ReadOnlySpan<char> _currentSpan;
        private Range _currentRange;

        /// <inheritdoc cref="IEnumerator{T}"/>
        public ReadOnlySpan<char> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _currentSpan;
        }

        public int Index => _position;
        public ReadOnlySpan<char> Span => _currentSpan;
        public string String => _currentSpan.ToString();
        public Range Range => _currentRange;

        public TextSplitListEnumerator(TextSplitList textSplitList)
        {
            _inputText = textSplitList._inputText;
            _ranges = textSplitList._ranges;
            _position = -1;
            _currentRange = default;
            _currentSpan = default;
        }

        /// <inheritdoc cref="IEnumerator"/>
        public bool MoveNext()
        {
            var index = _position + 1;
            _position = index;
            if (index < _ranges.Count)
            {
                _currentRange = _ranges[index];
                _currentSpan = _inputText[_currentRange];
                return true;
            }
            else
            {
                _currentRange = default;
                _currentSpan = default;
                return false;
            }
        }

        /// <inheritdoc cref="IEnumerator"/>
        public void Reset()
        {
            _position = -1;
            _currentRange = default;
            _currentSpan = default;
        }
    }
}

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

    public IReadOnlyList<string> ListStrings()
    {
        var e = GetEnumerator();
        var strings = new List<string>();
        while (e.MoveNext())
        {
            strings.Add(e.String);
        }
        return strings;
    }

    public TextSplitList ToList()
    {
        List<Range> ranges = new();
        var e = GetEnumerator();
        while (e.MoveNext())
        {
            ranges.Add(e.Range);
        }
        return new TextSplitList(InputText, ranges);
    }


    /// <inheritdoc cref="IEnumerable{T}"/>
    public TextSplitEnumerator GetEnumerator()
    {
        return new TextSplitEnumerator(this);
    }

    public ref struct TextSplitEnumerator //: IEnumerator<ReadOnlySpan<char>>, IEnumerator
    {
        private int _position = 0;
        private ReadOnlySpan<char> _currentTextSlice = default;
        private Range _currentRange = default;

        public readonly ReadOnlySpan<char> InputText;
        public readonly ReadOnlySpan<char> Separator;
        public readonly TextSplitOptions SplitOptions;
        public readonly StringComparison StringComparison;

        /// <inheritdoc cref="IEnumerator{T}"/>
        public ReadOnlySpan<char> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _currentTextSlice;
        }

        public ReadOnlySpan<char> Text => _currentTextSlice;
        public string String => _currentTextSlice.ToString();
        public Range Range => _currentRange;

        public TextSplitEnumerator(TextSplitEnumerable splitEnumerable)
        {
            InputText = splitEnumerable.InputText;
            Separator = splitEnumerable.Separator;
            SplitOptions = splitEnumerable.SplitOptions;
            StringComparison = splitEnumerable.StringComparison;
        }

        /// <inheritdoc cref="IEnumerator"/>
        public bool MoveNext()
        {
            int inputTextLen = InputText.Length;
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
                    if (!SplitOptions.HasFlag(TextSplitOptions.RemoveEmptyLines))
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
                    var separatorIndex = InputText.NextIndexOf(
                        Separator,
                        _position,
                        StringComparison
                    );
                    // None found or an empty separator yield the original
                    if (separatorIndex == -1 || Separator.Length == 0)
                    {
                        // End of slice is end of text
                        sliceEnd = InputText.Length;
                        // We're done enumerating
                        _position = sliceEnd + 1;
                    }
                    else
                    {
                        // This slice ends where the separator starts
                        sliceEnd = separatorIndex;
                        // We'll start again where the separator ends
                        _position = sliceEnd + Separator.Length;
                    }

#if NET6_0_OR_GREATER
                    // Respect StringSplitOptions
                    if (SplitOptions.HasFlag(TextSplitOptions.TrimLines))
                    {
                        // Copied from ReadOnlySpan<char>.Trim()
                        for (; sliceStart < sliceEnd; sliceStart++)
                        {
                            if (!char.IsWhiteSpace(InputText[sliceStart]))
                            {
                                break;
                            }
                        }

                        for (; sliceEnd > sliceStart; sliceEnd--)
                        {
                            if (!char.IsWhiteSpace(InputText[(sliceEnd - 1)]))
                            {
                                break;
                            }
                        }
                    }
#endif

                    _currentRange = new Range(
                        /* inclusive */start: sliceStart,
                        /* exclusive */end: sliceEnd
                    );

                    // Respect StringSplitOptions
                    _currentTextSlice = InputText[_currentRange];
                    if (_currentTextSlice.Length > 0 || !SplitOptions.HasFlag(TextSplitOptions.RemoveEmptyLines))
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
}
