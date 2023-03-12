namespace Jay.SourceGen.Text;

public static class AppendExtensions
{
    public static CodeBuilder Append(this CodeBuilder codeBuilder, char ch)
    {
        codeBuilder.Allocate(1)[0] = ch;
        return codeBuilder;
    }
    public static CodeBuilder Append(this CodeBuilder codeBuilder, string? str)
    {
        if (str is not null)
        {
            TextHelper.CopyTo(str, codeBuilder.Allocate(str.Length));
        }
        return codeBuilder;
    }
    public static CodeBuilder Append(this CodeBuilder codeBuilder, scoped ReadOnlySpan<char> text)
    {
        TextHelper.CopyTo(text, codeBuilder.Allocate(text.Length));
        return codeBuilder;
    }

    public static CodeBuilder Append<T>(this CodeBuilder codeBuilder, T? value)
    {
        string? str;
        if (value is IFormattable)
        {
            str = ((IFormattable)value).ToString(default, default);
        }
        else
        {
            str = value?.ToString();
        }
        if (str is not null)
        {
            TextHelper.CopyTo(str, codeBuilder.Allocate(str.Length));
        }

        return codeBuilder;
    }

    public static CodeBuilder Append<T>(this CodeBuilder codeBuilder,
        T? value,
        string? format,
        IFormatProvider? provider = default)
    {
        string? str;
        if (value is IFormattable)
        {
            str = ((IFormattable)value).ToString(format, provider);
        }
        else
        {
            str = value?.ToString();
        }
        if (str is not null)
        {
            TextHelper.CopyTo(str, codeBuilder.Allocate(str.Length));
        }

        return codeBuilder;
    }

    public static CodeBuilder AppendLine(this CodeBuilder codeBuilder) 
        => Append(codeBuilder, "\r\n");

    public static CodeBuilder AppendLine(this CodeBuilder codeBuilder, char ch)
        => codeBuilder.Append(ch).AppendLine();

    public static CodeBuilder AppendLine(this CodeBuilder codeBuilder, string? str)
        => codeBuilder.Append(str).AppendLine();

    public static CodeBuilder AppendLine(this CodeBuilder codeBuilder, scoped ReadOnlySpan<char> text)
        => codeBuilder.Append(text).AppendLine();

    public static CodeBuilder AppendLine<T>(this CodeBuilder codeBuilder, T? value)
        => codeBuilder.Append<T>(value).AppendLine();

    public static CodeBuilder AppendLine<T>(this CodeBuilder codeBuilder, T? value,
        string? format, IFormatProvider? provider = default)
        => codeBuilder.Append<T>(value, format, provider).AppendLine();
}