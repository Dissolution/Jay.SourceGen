using Jay.SourceGen.Text;

using System.Runtime.CompilerServices;
using System.Text;


namespace Jay.SourceGen.Coding;

partial class CodeBuilder
{
    /// <summary>
    /// The default <see cref="string"/> written for all <c>NewLine</c> operations <br/>
    /// <c>= "\r\n" [return + line feed]</c>
    /// </summary>
    /// <remarks>
    /// <see cref="Environment.NewLine"/> cannot be used with Source Generators, as it may be different on various compilation platforms. <br/>
    /// <c>'\n'</c> is recommended for compatability with Linux and git <br/>
    /// I've chosen to use <c>"\r\n"</c> (from Windows) as the default as I'm using Windows <br/>
    /// TODO: Discuss this?
    /// </remarks>
    public static string DefaultNewLine { get; set; } = "\r\n";

    /// <summary>
    /// The default indent used for all <c>Indent</c> operations <br/>
    /// <c>= "    " [4 spaces]</c>
    /// </summary>
    /// <remarks>
    /// spaces &gt; tabs
    /// </remarks>
    public static string DefaultIndent { get; set; } = "    ";
}


/// <summary>
/// A stack-based fluent text builder
/// </summary>
public sealed partial class CodeBuilder : IDisposable
{
    /// <summary>
    /// Rented <see cref="char"/><c>[]</c> from pool
    /// </summary>
    private char[] _charArray;

    /// <summary>
    /// The current position in <see cref="_chars"/> we're writing to
    /// </summary>
    private int _position;

    /// <summary>
    /// The current <see cref="string"/> written during a <see cref="NewLine"/> operation. <br/>
    /// This includes not only <see cref="DefaultNewLine"/>, but also the current indent
    /// </summary>
    private string _newLineIndent;

    /// <summary>
    /// Gets the <see cref="Span{T}">Span&lt;char&gt;</see> that has been written
    /// </summary>
    public Span<char> Written
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _charArray.AsSpan(0, _position);
    }

    /// <summary>
    /// Gets the <see cref="Span{T}">Span&lt;char&gt;</see> available for writing
    /// <br/>
    /// <b>Caution</b>: If you write to <see cref="Available"/>, you must also update <see cref="Length"/>!
    /// </summary>
    internal Span<char> Available
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
        internal set => _position = value.Clamp(0, Capacity);
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
                    : $"{nameof(index)} must be within [0..{_position})");
        }
    }

    public Span<char> this[Range range]
    {
        get
        {
            (int offset, int length) = range.GetOffsetAndLength(_position);
            if ((uint)offset + (uint)length <= _position)
            {
                return _charArray.AsSpan(range);
            }
            throw new ArgumentOutOfRangeException(nameof(range), range,
                $"Range '{range}' must be within [0..{_position})");
        }
    }

    public Span<char> CurrentLine
    {
        get
        {
            var written = Written;
            var finalNWIndex = written.LastIndexOf(DefaultNewLine.AsSpan());
            if (finalNWIndex == -1)
                return written;
            return written.Slice(finalNWIndex);
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
        // Start with no indent
        _newLineIndent = DefaultNewLine;
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
        Debug.Assert(minCapacity >= _position);

        // Get a new array at least minCapacity big
        char[] newArray = ArrayPool<char>.Shared.Rent(minCapacity);
        // Copy our written to it
        TextHelper.Unsafe.CopyTo(_charArray, newArray, _position);

        // We need to return our current array
        char[] toReturn = _charArray;

        // Store the new array as our current (_position does not change)
        _charArray = newArray;

        // Return and clear the old array
        ArrayPool<char>.Shared.Return(toReturn, true);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void GrowTo(int minCapacity)
    {
        if (minCapacity > Capacity)
        {
            // Give them exactly what they've asked for
            GrowCore(minCapacity);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void GrowBy(int adding)
    {
        // So long as they have an amount, we will grow
        if (adding > 0)
        {
            // If we have to add, we want to go big
            int newCapacity = (adding + Capacity) * 2;
            GrowCore(newCapacity);
        }
    }
    #endregion

    public Span<char> Allocate(int count)
    {
        if (count > 0)
        {
            // start of allocation
            var start = _position;
            // The end of the allocation
            var end = start + count;
            // Check for growth
            if (end > Capacity)
            {
                GrowBy(count); // Prefer growby
            }
            // Move position
            _position = end;

            // return allocated Span
            return _charArray.AsSpan(start..end);
        }
        // They requested nothing
        return Span<char>.Empty;
    }

    public CodeBuilder Append(char ch)
    {
        Allocate(1)[0] = ch;
        return this;
    }
    public CodeBuilder Append(string? str)
    {
        if (str is not null)
        {
            var len = str.Length;
            if (len > 0)
            {
                TextHelper.Unsafe.CopyTo(str, Allocate(len), len);
            }
        }
        return this;
    }
    public CodeBuilder Append(scoped ReadOnlySpan<char> text)
    {
        var len = text.Length;
        if (len > 0)
        {
            TextHelper.Unsafe.CopyTo(text, Allocate(len), len);
        }
        return this;
    }

    public CodeBuilder AppendLine(char ch) => Append(ch).NewLine();
    public CodeBuilder AppendLine(string? str) => Append(str).NewLine();
    public CodeBuilder AppendLine(scoped ReadOnlySpan<char> text) => Append(text).NewLine();


    private void WriteFormatLine(ReadOnlySpan<char> format, object?[] args)
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
                    Append(remainder);
                    return;
                }

                // Append the text until the brace.
                Append(remainder.Slice(0, countUntilNextBrace));
                pos += countUntilNextBrace;

                // Get the brace.
                // It must be followed by another character, either a copy of itself in the case of being escaped,
                // or an arbitrary character that's part of the hole in the case of an opening brace.
                char brace = format[pos];
                ch = moveNext(format, ref pos);
                if (brace == ch)
                {
                    Append(ch);
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
            Value(arg, itemFormat);

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


    public CodeBuilder NewLine()
    {
        return Append(_newLineIndent);
    }
    public CodeBuilder NewLines(int count)
    {
        for (var i = 0; i < count; i++)
        {
            Append(_newLineIndent);
        }
        return this;
    }

    internal CodeBuilder IndentAwareAppend(CBA codeBuilderAction)
    {
        var oldIndent = _newLineIndent;
        var currentIndent = CurrentLine.ToString();
        _newLineIndent = currentIndent;
        codeBuilderAction(this);
        _newLineIndent = oldIndent;
        return this;
    }

    public CodeBuilder Value<T>(
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
                return IndentAwareAppend(cba);
            }
            case string str:
            {
                return Append(str);
            }
            case IFormattable formattable:
            {
                return Append(formattable.ToString(format, provider));
            }
            case IEnumerable enumerable:
            {
                format ??= ",";
                return Delimit(
                    format,
                    enumerable.Cast<object?>(),
                    (w, v) => w.Value(v, default, provider)
                );
            }
            case Delegate del:
            {
                // Check for CBA compat
                var method = del.Method;
                if (method.ReturnType != typeof(void)) break;
                var methodParams = method.GetParameters();
                if (methodParams.Length != 1 || methodParams[0].ParameterType != typeof(CodeBuilder)) break;

                // Convert into CBA
                CBA? cba = Delegate.CreateDelegate(typeof(CBA), del.Target, del.Method) as CBA;
                if (cba is not null)
                {
                    return IndentAwareAppend(cba);
                }
                // Cannot cast, fallthrough
                break;
            }
            default:
            {
                break;
            }
        }

        var valueType = value.GetType();
        return Append(value.ToString());
    }

    public CodeBuilder Code(NonFormattableString code)
    {
        var lines = code.Text.TextSplit(DefaultNewLine);
        var e = lines.GetEnumerator();
        if (!e.MoveNext()) return this;
        Append(e.Current);
        while (e.MoveNext())
        {
            NewLine().Append(e.Current);
        }
        return this;
    }

    public CodeBuilder Code(FormattableString code)
    {
        ReadOnlySpan<char> format = code.Format.AsSpan();
        object?[] formatArgs = code.GetArguments();
        var lines = format.TextSplit(DefaultNewLine);
        var e = lines.GetEnumerator();
        if (!e.MoveNext()) return this;
        WriteFormatLine(e.CharSpan, formatArgs);
        while (e.MoveNext())
        {
            NewLine();
            WriteFormatLine(e.CharSpan, formatArgs);
        }
        return this;
    }

    public CodeBuilder CodeLine(NonFormattableString code) => Code(code).NewLine();
    public CodeBuilder CodeBlock(FormattableString code) => Code(code).NewLine();

    #region Remove + Trim
    public bool TryRemove(int index)
    {
        if ((uint)index >= _position) return false;
        var written = Written;
        var right = written.Slice(index + 1);
        var dest = written.Slice(index);
        TextHelper.Unsafe.CopyTo(right, dest, right.Length);
        _position -= 1;
        return true;
    }

    public bool TryRemove(int index, out char ch)
    {
        if ((uint)index >= _position)
        {
            ch = default;
            return false;
        }
        var written = Written;
        ch = written[index];
        var right = written.Slice(index + 1);
        var dest = written.Slice(index);
        TextHelper.Unsafe.CopyTo(right, dest, right.Length);
        _position -= 1;
        return true;
    }

    public bool TryRemove(int start, int length)
    {
        if ((uint)start + (uint)length > _position) return false;
        if (length > 0)
        {
            var written = Written;
            var right = written.Slice(start + length);
            var dest = written.Slice(start);
            TextHelper.Unsafe.CopyTo(right, dest, right.Length);
            _position -= length;
        }
        return true;
    }

    public bool TryRemove(int start, int length, [NotNullWhen(true)] out string? slice)
    {
        if ((uint)start + (uint)length > _position)
        {
            slice = null;
            return false;
        }
        if (length > 0)
        {
            var written = Written;
            slice = written.Slice(start, length).ToString();
            var right = written.Slice(start + length);
            var dest = written.Slice(start);
            TextHelper.Unsafe.CopyTo(right, dest, right.Length);
            _position -= length;
        }
        else
        {
            slice = "";
        }
        return true;
    }

    public bool TryRemove(Range range)
    {
        (int offset, int length) = range.GetOffsetAndLength(_position);
        if ((uint)offset + (uint)length > (uint)_position) return false;
        if (length > 0)
        {
            var written = Written;
            var right = written.Slice(offset + length);
            var dest = written.Slice(offset);
            TextHelper.Unsafe.CopyTo(right, dest, right.Length);
            _position -= length;
        }
        return true;
    }

    public bool TryRemove(Range range, [NotNullWhen(true)] out string? slice)
    {
        (int offset, int length) = range.GetOffsetAndLength(_position);
        if ((uint)offset + (uint)length > _position)
        {
            slice = null;
            return false;
        }
        if (length > 0)
        {
            var written = Written;
            slice = written.Slice(offset, length).ToString();
            var right = written.Slice(offset + length);
            var dest = written.Slice(offset);
            TextHelper.Unsafe.CopyTo(right, dest, right.Length);
            _position -= length;
        }
        else
        {
            slice = "";
        }
        return true;
    }

    public CodeBuilder TrimStart()
    {
        var written = Written;
        int len = _position;
        int i = 0;
        while (i < len && char.IsWhiteSpace(written[i]))
            i++;
        if (i > 0)
        {
            TextHelper.CopyTo(written[i..], written);
            _position -= i;
        }
        return this;
    }

    public CodeBuilder TrimEnd()
    {
        var written = Written;
        int len = _position;
        int e = _position - 1;
        while (e >= 0 && char.IsWhiteSpace(written[e]))
            e--;
        if (e < _position - 1)
        {
            _position = e + 1;
        }
        return this;
    }



    #endregion

    #region Enumerate
    public CodeBuilder Enumerate<T>(
       IEnumerable<T>? values,
       CBIA<T>? perValueAction)
    {
        if (values is null || perValueAction is null) return this;
        using var e = values.GetEnumerator();
        int index = 0;
        if (!e.MoveNext()) return this;
        perValueAction?.Invoke(this, e.Current, index);
        while (e.MoveNext())
        {
            index++;
            perValueAction?.Invoke(this, e.Current, index);
        }
        return this;
    }

    public CodeBuilder Enumerate<T>(
        IEnumerable<T>? values,
        CBA<T>? perValueAction)
    {
        if (values is null || perValueAction is null) return this;
        using var e = values.GetEnumerator();
        if (!e.MoveNext()) return this;
        perValueAction?.Invoke(this, e.Current);
        while (e.MoveNext())
        {
            perValueAction?.Invoke(this, e.Current);
        }
        return this;
    }


    public CodeBuilder EnumerateAppend<T>(IEnumerable<T>? values) => Enumerate(values, static (cb, v) => cb.Value(v));
    #endregion

    #region Delimit

    public CodeBuilder Delimit<T>(
        CBA? delimitAction,
        IEnumerable<T>? values,
        CBIA<T>? perValueAction)
    {
        if (values is null || delimitAction is null && perValueAction is null) return this;
        using var e = values.GetEnumerator();
        int index = 0;
        if (!e.MoveNext()) return this;
        perValueAction?.Invoke(this, e.Current, index);
        while (e.MoveNext())
        {
            delimitAction?.Invoke(this);
            index++;
            perValueAction?.Invoke(this, e.Current, index);
        }
        return this;
    }

    public CodeBuilder Delimit<T>(
        CBA? delimitAction,
        IEnumerable<T>? values,
        CBA<T>? perValueAction)
    {
        if (values is null || delimitAction is null && perValueAction is null) return this;
        using var e = values.GetEnumerator();
        if (!e.MoveNext()) return this;
        perValueAction?.Invoke(this, e.Current);
        while (e.MoveNext())
        {
            delimitAction?.Invoke(this);
            perValueAction?.Invoke(this, e.Current);
        }
        return this;
    }

    public CodeBuilder Delimit<T>(
       string? delimiter,
       IEnumerable<T>? values,
       CBA<T>? perValueAction)
    {
        if (string.IsNullOrEmpty(delimiter))
            return Enumerate(values, perValueAction);
        return Delimit(b => b.Code(delimiter), values, perValueAction);
    }


    //public CodeBuilder WrapDelimit<T>(string? delimiter, IEnumerable<T>? values, CBA<T>? perValueAction)
    //{
    //    CBA delimitAction = b => b.Code(delimiter);
    //    List<CBA> actions = new();
    //    using 
    //}


    public CodeBuilder DelimitAppend<T>(CBA? delimitAction, IEnumerable<T>? values)
    {
        return Delimit(delimitAction, values, static (b, v) => b.Value(v));
    }

    public CodeBuilder DelimitAppend<T>(string? delimiter, IEnumerable<T>? values)
    {
        return Delimit(delimiter, values, static (b, v) => b.Value(v));
    }

    public CodeBuilder LineDelimit<T>(IEnumerable<T>? values, CBA<T>? delimitedValueAction)
    {
        return Delimit(static b => b.NewLine(), values, delimitedValueAction);
    }

    public CodeBuilder LineDelimitAppend<T>(IEnumerable<T>? values)
    {
        return Delimit(static b => b.NewLine(), values, static (cb, v) => cb.Value(v));
    }
    #endregion

    public CodeBuilder IndentBlock(CBA indentBlock)
    {
        return IndentBlock(DefaultIndent, indentBlock);
    }

    public CodeBuilder IndentBlock(string indent, CBA indentBlock)
    {
        var oldIndent = _newLineIndent;
        // We might be on a new line, but not yet indented
        if (TextHelper.Equals(CurrentLine, oldIndent))
        {
            Append(indent);
        }

        var newIndent = oldIndent + indent;
        _newLineIndent = newIndent;
        indentBlock(this);
        _newLineIndent = oldIndent;
        // Did we do a newline that we need to decrease?
        if (Written.EndsWith(newIndent.AsSpan()))
        {
            _position -= newIndent.Length;
            Append(oldIndent);
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
        indent ??= DefaultIndent;
        // Trim all trailing whitespace
        return TrimEnd()
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
        Append('#').Append(directiveName);
        if (!string.IsNullOrEmpty(directiveValue))
        {
            Append(' ').Append(directiveValue);
        }
        NewLine();
        directiveBlock(this);
        EnsureOnStartOfNewLine();
        if (endDirective is null)
        {
            endDirective = $"#end{directiveName}";
        }
        return AppendLine(endDirective);
    }


    #region Fluent CS File
    /// <summary>
    /// Adds the `// &lt;auto-generated/&gt; ` line, optionally expanding it to include a <paramref name="comment"/>
    /// </summary>
    public CodeBuilder AutoGeneratedHeader(string? comment = null)
    {
        if (comment is null)
        {
            return AppendLine("// <auto-generated/>");
        }

        AppendLine("// <auto-generated>");
        foreach (var line in comment.TextSplit(DefaultNewLine))
        {
            Append("// ").AppendLine(line);
        }
        AppendLine("// </auto-generated>");
        return this;
    }

    public CodeBuilder Nullable(bool enable = true)
    {
        return Append("#nullable ")
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
            return Append("using ").Append(ns).AppendLine(';');
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
        return Append("namespace ").Append(ns).AppendLine(';').NewLine();
    }

    public CodeBuilder Namespace(string nameSpace,
        CBA namespaceBlock)
    {
        ReadOnlySpan<char> ns = nameSpace.AsSpan().Trim();
        if (ns.Length == 0)
        {
            throw new ArgumentException("Invalid namespace", nameof(nameSpace));
        }
        return Append("namespace ").AppendLine(ns).BracketBlock(namespaceBlock).NewLine();
    }


    /// <summary>
    /// Writes the given <paramref name="comment"/> as a comment line / lines
    /// </summary>
    public CodeBuilder Comment(string? comment)
    {
        /* Most of the time, this is probably a single line.
         * But we do want to watch out for newline characters to turn
         * this into a multi-line comment */

        var comments = comment.TextSplit(DefaultNewLine).GetEnumerator();
        if (!comments.MoveNext())
        {
            // Null or empty comment is blank
            return AppendLine("// ");
        }
        var cmnt = comments.CharSpan;
        if (!comments.MoveNext())
        {
            // Only a single comment
            return Append("// ").AppendLine(cmnt);
        }

        // Multiple comments
        Append("/* ").AppendLine(cmnt);
        Append(" * ").AppendLine(comments.CharSpan);
        while (comments.MoveNext())
        {
            Append(" * ").AppendLine(comments.CharSpan);
        }
        return AppendLine(" */");
    }

    public CodeBuilder Comment(string? comment, CommentType commentType)
    {
        var splitEnumerable = comment.TextSplit(DefaultNewLine);
        switch (commentType)
        {
            case CommentType.SingleLine:
            {
                foreach (var line in splitEnumerable)
                {
                    Append("// ").AppendLine(line);
                }
                break;
            }
            case CommentType.XML:
            {
                foreach (var line in splitEnumerable)
                {
                    Append("/// ").AppendLine(line);
                }
                break;
            }
            case CommentType.MultiLine:
            {
                var comments = comment.TextSplit(DefaultNewLine).GetEnumerator();
                if (!comments.MoveNext())
                {
                    // Null or empty comment is blank
                    return AppendLine("/* */");
                }
                var cmnt = comments.CharSpan;
                if (!comments.MoveNext())
                {
                    // Only a single comment
                    return Append("/* ").Append(cmnt).AppendLine(" */");
                }

                // Multiple comments
                Append("/* ").AppendLine(cmnt);
                Append(" * ").AppendLine(comments.CharSpan);
                while (comments.MoveNext())
                {
                    Append(" * ").AppendLine(comments.CharSpan);
                }
                return AppendLine(" */");
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(commentType));
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
        if (predicateResult)
        {
            ifTrue?.Invoke(this);
        }
        else
        {
            ifFalse?.Invoke(this);
        }
        return this;
    }

    public CodeBuilder AppendIf(bool predicateResult, string? strIfTrue, string? strIfFalse = default)
    {
        if (predicateResult)
        {
            return Append(strIfTrue);
        }
        else
        {
            return Append(strIfFalse);
        }
    }
    public CodeBuilder AppendIf(bool predicateResult, ReadOnlySpan<char> textIfTrue, ReadOnlySpan<char> textIfFalse = default)
    {
        if (predicateResult)
        {
            return Append(textIfTrue);
        }
        else
        {
            return Append(textIfFalse);
        }
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
        string str = ToString();
        Dispose();
        // return the string
        return str;
    }

    public string ToStringAndClear()
    {
        // Get our string
        string str = ToString();
        Length = 0;
        return str;
    }

    public override string ToString()
    {
        return Written.ToString();
    }
}