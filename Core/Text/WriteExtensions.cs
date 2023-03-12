namespace Jay.SourceGen.Text;

public static class WriteExtensions
{
    public static void Write(this CodeBuilder textBuilder, char ch)
        => textBuilder.Allocate(1)[0] = ch;
    public static void Write(this CodeBuilder textBuilder, string? str)
        => Write(textBuilder, str.AsSpan());
    public static void Write(this CodeBuilder textBuilder, scoped ReadOnlySpan<char> text)
        => TextHelper.CopyTo(text, textBuilder.Allocate(text.Length));
    
    public static void Write<T>(this CodeBuilder textBuilder, T? value)
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
            TextHelper.CopyTo(str, textBuilder.Allocate(str.Length));
        }
    }
    
    public static void Write<T>(this CodeBuilder textBuilder,
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
            TextHelper.CopyTo(str, textBuilder.Allocate(str.Length));
        }
    }
    
 

    public static void WriteLine(this CodeBuilder textBuilder)
    {
        Write(textBuilder, "\r\n");
    }
}