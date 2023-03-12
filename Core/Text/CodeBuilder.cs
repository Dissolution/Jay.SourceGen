using Jay.Text.Extensions;

using System.Runtime.CompilerServices;
using System.Text;


namespace Jay.SourceGen.Text;


/// <summary>
/// A stack-based fluent text builder
/// </summary>
public sealed class CodeBuilder : IDisposable
{
    public static string DefaultNewLine { get; set; } = "\r\n";

    /// <summary>
    /// Rented <see cref="char"/><c>[]</c> from pool
    /// </summary>
    private char[] _charArray;

    /// <summary>
    /// The current position in <see cref="_chars"/> we're writing to
    /// </summary>
    private int _position;

    private string _newLineIndent;

    /// <summary>
    /// Gets the <see cref="System.Span{T}">Span&lt;char&gt;</see> that has been written
    /// </summary>
    public Span<char> Written
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _charArray.AsSpan(0, _position);
    }

    /// <summary>
    /// Gets the <see cref="System.Span{T}">Span&lt;char&gt;</see> available for writing
    /// <br/>
    /// <b>Caution</b>: If you write to <see cref="Available"/>, you must also update <see cref="Length"/>!
    /// </summary>
    public Span<char> Available
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _charArray.AsSpan(_position);
    }

    /// <summary>
    /// The current total capacity to store <see cref="char"/>s
    /// <br/>
    /// Can be increased with <see cref="M:GrowBy"/> and <see cref="M:GrowTo"/>
    /// </summary>
    internal int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _charArray.Length;
    }

    /// <summary>
    /// Gets or sets the number of <see cref="Written"/> <see cref="char"/>s
    /// </summary>
    /// <remarks>
    /// <c>set</c> values will be clamped between 0 and <see cref="Capacity"/>
    /// </remarks>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _position;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _position = value.Clamp(0, Capacity);
    }

    public ref char this[int index]
    {
        get
        {
            if ((uint)index < _position)
            {
                return ref _charArray[index];
            }
            throw new ArgumentOutOfRangeException(nameof(index), index,
                _position == 0
                    ? $"There are no characters to index"
                    : $"{nameof(index)} must be between 0 and {_position - 1}");
        }
    }

    public Span<char> this[Range range]
    {
        get
        {
            (int offset, int length) = range.GetOffsetAndLength(_position);
            if ((uint)offset + (uint)length <= _position)
            {
                return _charArray.AsSpan()[range];
            }
            throw new ArgumentOutOfRangeException(nameof(range), range,
                $"Range '{range}' did not fit in [0..{_position})");
        }
    }

    /// <summary>
    /// Construct a new <see cref="CodeBuilder"/> with default starting <see cref="Capacity"/>
    /// </summary>
    public CodeBuilder() : this(1024) { }

    /// <summary>
    /// Construct a new <see cref="CodeBuilder"/> with specified minimum starting <paramref name="minCapacity"/>
    /// </summary>
    /// <param name="minCapacity">
    /// The minimum possible starting <see cref="Capacity"/> <br/>
    /// Actual starting <see cref="Capacity"/> may be larger
    /// </param>
    public CodeBuilder(int minCapacity)
    {
        _charArray = ArrayPool<char>.Shared.Rent(Math.Max(minCapacity, 1024));
        _position = 0;
        _newLineIndent = "\r\n";
    }


    #region Grow
    /// <summary>
    /// Grow the size of <see cref="_charArray"/> (and thus <see cref="_chars"/>)
    /// to at least the specified <paramref name="minCapacity"/>.
    /// </summary>
    /// <param name="minCapacity">The minimum possible <see cref="Capacity"/> to grow to</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowCore(int minCapacity)
    {
        Debug.Assert(minCapacity > 1024);
        Debug.Assert(minCapacity > Capacity);

        // Get a new array at least minCapacity big
        char[] newArray = ArrayPool<char>.Shared.Rent(minCapacity);
        // Copy our written to it
        Written.CopyTo(newArray.AsSpan());

        // Store an array to return (we may not have one)
        char[]? toReturn = _charArray;
        // Set our newarray to our current array + span
        _charArray = newArray;

        // Return the borrowed array
        ArrayPool<char>.Shared.Return(toReturn, true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowTo(int minCapacity)
    {
        int curCapacity = Capacity;
        Debug.Assert(minCapacity > curCapacity);
        int newCapacity = (minCapacity + curCapacity);
        GrowCore(newCapacity);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void GrowBy(int adding)
    {
        if (adding > 0)
        {
            int curCapacity = Capacity;
            int newCapacity = (adding + curCapacity) * 2;
            GrowCore(newCapacity);
        }
    }
    #endregion

    protected void WriteFormatLine(ReadOnlySpan<char> format, object?[] args)
    {
        // Undocumented exclusive limits on the range for Argument Hole Index
        const int IndexLimit = 1_000_000; // Note:            0 <= ArgIndex < IndexLimit

        // Repeatedly find the next hole and process it.
        int pos = 0;
        char ch;
        while (true)
        {
            // Skip until either the end of the input or the first unescaped opening brace, whichever comes first.
            // Along the way we need to also unescape escaped closing braces.
            while (true)
            {
                // Find the next brace.  If there isn't one, the remainder of the input is text to be appended, and we're done.
                if (pos >= format.Length)
                {
                    return;
                }

                ReadOnlySpan<char> remainder = format.Slice(pos);
                int countUntilNextBrace = remainder.IndexOfAny('{', '}');
                if (countUntilNextBrace < 0)
                {
                    this.Write(remainder);
                    return;
                }

                // Append the text until the brace.
                this.Write(remainder.Slice(0, countUntilNextBrace));
                pos += countUntilNextBrace;

                // Get the brace.
                // It must be followed by another character, either a copy of itself in the case of being escaped,
                // or an arbitrary character that's part of the hole in the case of an opening brace.
                char brace = format[pos];
                ch = moveNext(format, ref pos);
                if (brace == ch)
                {
                    this.Write(ch);
                    pos++;
                    continue;
                }

                // This wasn't an escape, so it must be an opening brace.
                if (brace != '{')
                {
                    throw createFormatException(format, pos, "Missing opening brace");
                }

                // Proceed to parse the hole.
                break;
            }

            // We're now positioned just after the opening brace of an argument hole, which consists of
            // an opening brace, an index, and an optional format
            // preceded by a colon, with arbitrary amounts of spaces throughout.
            ReadOnlySpan<char> itemFormatSpan = default; // used if itemFormat is null

            // First up is the index parameter, which is of the form:
            //     at least on digit
            //     optional any number of spaces
            // We've already read the first digit into ch.
            Debug.Assert(format[pos - 1] == '{');
            Debug.Assert(ch != '{');
            int index = ch - '0';
            // Has to be between 0 and 9
            if ((uint)index >= 10u)
            {
                throw createFormatException(format, pos, "Invalid character in index");
            }

            // Common case is a single digit index followed by a closing brace.  If it's not a closing brace,
            // proceed to finish parsing the full hole format.
            ch = moveNext(format, ref pos);
            if (ch != '}')
            {
                // Continue consuming optional additional digits.
                while (ch.IsAsciiDigit() && index < IndexLimit)
                {
                    // Shift by power of 10
                    index = index * 10 + (ch - '0');
                    ch = moveNext(format, ref pos);
                }

                // Consume optional whitespace.
                while (ch == ' ')
                {
                    ch = moveNext(format, ref pos);
                }

                // We do not support alignment
                if (ch == ',')
                {
                    throw createFormatException(format, pos, "Alignment is not supported");
                }

                // The next character needs to either be a closing brace for the end of the hole,
                // or a colon indicating the start of the format.
                if (ch != '}')
                {
                    if (ch != ':')
                    {
                        // Unexpected character
                        throw createFormatException(format, pos, "Unexpected character");
                    }

                    // Search for the closing brace; everything in between is the format,
                    // but opening braces aren't allowed.
                    int startingPos = pos;
                    while (true)
                    {
                        ch = moveNext(format, ref pos);

                        if (ch == '}')
                        {
                            // Argument hole closed
                            break;
                        }

                        if (ch == '{')
                        {
                            // Braces inside the argument hole are not supported
                            throw createFormatException(format, pos, "Braces inside the argument hole are not supported");
                        }
                    }

                    startingPos++;
                    itemFormatSpan = format.Slice(startingPos, pos - startingPos);
                }
            }

            // Construct the output for this arg hole.
            Debug.Assert(format[pos] == '}');
            pos++;

            if ((uint)index >= (uint)args.Length)
            {
                throw createFormatException(format, pos, $"Invalid Format: Argument '{index}' does not exist");
            }

            string? itemFormat = null;
            if (itemFormatSpan.Length > 0)
                itemFormat = itemFormatSpan.ToString();

            object? arg = args[index];

            // Append this arg, allows for overridden behavior
            this.Format<object?>(arg, itemFormat);

            // Continue parsing the rest of the format string.
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static char moveNext(ReadOnlySpan<char> format, ref int pos)
        {
            pos++;
            if (pos < format.Length)
                return format[pos];
            throw createFormatException(format, pos, "Attempted to move past final character");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static FormatException createFormatException(ReadOnlySpan<char> format, int pos, string? details = null)
        {
            var message = new StringBuilder();
            message.Append("Invalid Format at position ");
            message.Append(pos);
            message.AppendLine();
            int start = pos - 16;
            if (start < 0)
                start = 0;
            int end = pos + 16;
            if (end > format.Length)
                end = format.Length - 1;
            message.Append(format[new Range(start, end)].ToString());
            if (details is not null)
            {
                message.AppendLine();
                message.Append("Details: ");
                message.Append(details);
            }
            return new FormatException(message.ToString());
        }
    }

    public Span<char> Allocate(int count)
    {
        if (count > 0)
        {
            // Start + End of alloaction
            var start = _position;
            // The end of the allocation
            var end = start + count;
            // Check for growth
            if (end > Capacity)
            {
                GrowTo(end);
            }
            // Move position
            _position = end;
            // return allocated Span
            return _charArray.AsSpan()[start..end];
        }
        return Span<char>.Empty;
    }

    public CodeBuilder NewLine()
    {
        TextHelper.CopyTo(DefaultNewLine, Allocate(DefaultNewLine.Length));
        return this;
    }

    public CodeBuilder Format<T>(
       [AllowNull] T value,
       string? format = default,
       IFormatProvider? provider = default
   )
    {
        switch (value)
        {
            case null:
            {
                return this;
            }
            case CBA cba:
            {
                var oldIndent = _newLineIndent;
                var currentIndent = this.CurrentNewLineIndent();
                _newLineIndent = currentIndent;
                cba(this);
                _newLineIndent = oldIndent;
                return this;
            }

            case string str:
            {
                this.Write(str);
                return this;
            }
            case IFormattable formattable:
            {
                this.Write(formattable.ToString(format, provider));
                return this;
            }
            case IEnumerable enumerable:
            {
                format ??= ",";
                return this.Delimit(
                    format,
                    enumerable.Cast<object?>(),
                    (w, v) => w.Format<object?>(v, default, provider)
                );
            }
            default:
            {
                this.Write<T>(value);
                return this;
            }
        }
    }

    public CodeBuilder Code(NonFormattableString code)
    {
        var lines = code.Text.TextSplit(DefaultNewLine).ToList();
        var e = lines.GetEnumerator();
        while (e.MoveNext())
        {
            if (e.Index > 0)
                NewLine();
            this.Write(e.Current);
        }
        return this;
    }

    public CodeBuilder Code(FormattableString code)
    {
        ReadOnlySpan<char> format = code.Format.AsSpan();
        object?[] formatArgs = code.GetArguments();
        var lines = format.TextSplit(DefaultNewLine).ToList();
        var e = lines.GetEnumerator();
        while (e.MoveNext())
        {
            if (e.Index > 0)
                NewLine();
            WriteFormatLine(e.Span, formatArgs);
        }
        return this;
    }

    public CodeBuilder CodeLine(NonFormattableString code) => Code(code).NewLine();
    public CodeBuilder CodeLine(FormattableString code) => Code(code).NewLine();

    public CodeBuilder IndentBlock(string indent, CBA indentBlock)
    {
        var oldIndent = _newLineIndent;
        // We might be on a new line, but not yet indented
        if (this.CurrentNewLineIndent() == oldIndent)
        {
            this.Write(indent);
        }

        var newIndent = oldIndent + indent;
        _newLineIndent = newIndent;
        indentBlock(this);
        _newLineIndent = oldIndent;
        // Did we do a newline that we need to decrease?
        if (Written.EndsWith(newIndent.AsSpan()))
        {
            this.Length -= newIndent.Length;
            this.Write(oldIndent);
        }
        return this;
    }

    public CodeBuilder EnsureOnStartOfNewLine()
    {
        if (!Written.EndsWith(_newLineIndent.AsSpan()))
        {
            return NewLine();
        }
        return this;
    }

    public CodeBuilder BracketBlock(CBA bracketBlock, string? indent = null)
    {
        indent ??= "    ";
        // Trim all trailing whitespace
        return this.TrimEnd()
            // Start a new line
            .NewLine()
            // Starting bracket
            .AppendLine('{')
            // Starts an indented block inside of that bracket
            .IndentBlock(indent, bracketBlock)
            // Be sure that we're not putting the end bracket at the end of text
            .EnsureOnStartOfNewLine()
            // Ending bracket
            .Append('}');
    }

    public CodeBuilder Directive(
        string directiveName,
        string? directiveValue,
        CBA directiveBlock,
        string? endDirective = null)
    {
        this.Append('#').Append(directiveName);
        if (!string.IsNullOrEmpty(directiveValue))
        {
            this.Append(' ').Append(directiveValue);
        }
        this.NewLine();
        directiveBlock(this);
        this.EnsureOnStartOfNewLine();
        if (endDirective is null)
        {
            endDirective = $"#end{directiveName}";
        }
        return this.AppendLine(endDirective);
    }


    #region Fluent CS File
    /// <summary>
    /// Adds the `// &lt;auto-generated/&gt; ` line, optionally expanding it to include a <paramref name="comment"/>
    /// </summary>
    public CodeBuilder AutoGeneratedHeader(string? comment = null)
    {
        if (comment is null)
        {
            return this.AppendLine("// <auto-generated/>");
        }

        this.AppendLine("// <auto-generated>");
        foreach (var line in comment.TextSplit(DefaultNewLine))
        {
            this.Append("// ").AppendLine(line);
        }
        this.AppendLine("// </auto-generated>");
        return this;
    }

    public CodeBuilder Nullable(bool enable = true)
    {
        return this
            .Append("#nullable ")
            .Append(enable ? "enable" : "disable")
            .NewLine();
    }

    /// <summary>
    /// Writes a `using <paramref name="nameSpace"/>;` line
    /// </summary>
    public CodeBuilder Using(string nameSpace)
    {
        ReadOnlySpan<char> ns = nameSpace
            .AsSpan()
            .TrimStart()
            .TrimStart("using ".AsSpan())
            .TrimEnd()
            .TrimEnd(';');
        if (ns.Length > 0)
        {
            return this.Append("using ").Append(ns).AppendLine(';');
        }
        return this;
    }

    /// <summary>
    /// Writes multiple <c>using</c> <paramref name="namespaces"/>
    /// </summary>
    public CodeBuilder Using(params string[] namespaces)
    {
        foreach (var nameSpace in namespaces)
        {
            Using(nameSpace);
        }
        return this;
    }

    public CodeBuilder Namespace(string? nameSpace)
    {
        ReadOnlySpan<char> ns = nameSpace.AsSpan().Trim();
        if (ns.Length == 0)
        {
            return this;
        }
        return this.Append("namespace ").Append(ns).AppendLine(';').NewLine();
    }

    public CodeBuilder Namespace(string nameSpace,
        CBA namespaceBlock)
    {
        ReadOnlySpan<char> ns = nameSpace.AsSpan().Trim();
        if (ns.Length == 0)
        {
            throw new ArgumentException("Invalid namespace", nameof(nameSpace));
        }
        return this.Append("namespace ").AppendLine(ns).BracketBlock(namespaceBlock).NewLine();
    }


    /// <summary>
    /// Writes the given <paramref name="comment"/> as a comment line / lines
    /// </summary>
    public CodeBuilder Comment(string? comment)
    {
        /* Most of the time, this is probably a single line.
         * But we do want to watch out for newline characters to turn
         * this into a multi-line comment */

        var comments = comment.TextSplit(DefaultNewLine)
            .ToList();

        // Null or empty comment is blank
        if (comments.Count == 0)
        {
            return this.AppendLine("// ");
        }
        // Only a single comment?
        if (comments.Count == 1)
        {
            // Single line
            return this.Append("// ").AppendLine(comments.Text(0));
        }

        // Multiple comments
        this.Append("/* ").AppendLine(comments.Text(0));
        for (var i = 1; i < comments.Count; i++)
        {
            this.Append(" * ").AppendLine(comments.Text(i));
        }
        return this.AppendLine(" */");
    }

    public CodeBuilder Comment(string? comment, CommentType commentType)
    {
        var splitEnumerable = comment.TextSplit(DefaultNewLine);
        if (commentType == CommentType.SingleLine)
        {
            foreach (var line in splitEnumerable)
            {
                this.Append("// ").AppendLine(line);
            }
        }
        else if (commentType == CommentType.XML)
        {
            foreach (var line in splitEnumerable)
            {
                this.Append("/// ").AppendLine(line);
            }
        }
        else
        {
            var comments = splitEnumerable.ToList();

            // Null or empty comment is blank
            if (comments.Count == 0)
            {
                return this.AppendLine("/* */");
            }
            // Only a single comment?
            if (comments.Count == 1)
            {
                // Single line
                return this.Append("/* ").Append(comments.Text(0)).AppendLine(" */");
            }

            // Multiple comments
            this.Append("/* ").AppendLine(comments.Text(0));
            for (var i = 1; i < comments.Count; i++)
            {
                this.Append(" * ").AppendLine(comments.Text(i));
            }
            return this.AppendLine(" */");
        }

        return this;
    }
    #endregion


    public CodeBuilder If(
        bool predicateResult,
        CBA? ifTrue,
        CBA? ifFalse = null
    )
    {
        return If(() => predicateResult, ifTrue, ifFalse);
    }

    public CodeBuilder If(
        Func<bool> predicate,
        CBA? ifTrue,
        CBA? ifFalse = null
    )
    {
        if (predicate())
        {
            ifTrue?.Invoke(this);
        }
        else
        {
            ifFalse?.Invoke(this);
        }
        return this;
    }

    public bool TryCopyTo(Span<char> destination)
    {
        return Written.TryCopyTo(destination);
    }

    public void Clear()
    {
        Length = 0;
    }

    public void Dispose()
    {
        // Get a possible array to return
        char[]? toReturn = _charArray;
        // clear
        _charArray = null!;
        _position = 0;
        if (toReturn is not null)
        {
            ArrayPool<char>.Shared.Return(toReturn, true);
        }
    }

    public bool Equals(ReadOnlySpan<char> text)
    {
        return TextHelper.Equals(Written, text);
    }

    public bool Equals(ReadOnlySpan<char> text, StringComparison comparison)
    {
        return TextHelper.Equals(Written, text, comparison);
    }

    public bool Equals(string? str)
    {
        return TextHelper.Equals(Written, str);
    }

    public bool Equals(string? str, StringComparison comparison)
    {
        return TextHelper.Equals(Written, str, comparison);
    }

    public override bool Equals(object? obj)
    {
        if (obj is CodeBuilder codeBuilder)
            return TextHelper.Equals(Written, codeBuilder.Written);
        if (obj is string str) return TextHelper.Equals(Written, str);
        return false;
    }

    [DoesNotReturn]
    public override int GetHashCode() => throw new NotSupportedException();

    public string ToStringAndDispose()
    {
        // Get our string
        string str = this.ToString();
        this.Dispose();
        // return the string
        return str;
    }

    public string ToStringAndClear()
    {
        // Get our string
        string str = this.ToString();
        Length = 0;
        return str;
    }

    public override string ToString()
    {
        return Written.ToString();
    }
}