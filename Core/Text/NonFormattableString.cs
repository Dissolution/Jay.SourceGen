namespace Jay.SourceGen.Text;

/// <summary>
/// Provides a way for methods capturing <see cref="FormattableString"/> to exist alongside methods that only care about <see cref="string"/>
/// </summary>
public readonly struct NonFormattableString : IEquatable<string>
{
    public static implicit operator NonFormattableString(string? str) => new NonFormattableString(str);
    public static implicit operator NonFormattableString(FormattableString _) => throw new InvalidOperationException();
    public static implicit operator NonFormattableString(ReadOnlySpan<char> _) => throw new InvalidOperationException();

    public static bool operator ==(NonFormattableString nfs, string? str) => nfs.Equals(str);
    public static bool operator !=(NonFormattableString nfs, string? str) => !nfs.Equals(str);


    private readonly string? _str;

    public ReadOnlySpan<char> Text => _str.AsSpan();
    public string String => _str ?? "";

    private NonFormattableString(string? str)
    {
        _str = str;
    }

    public bool Equals(string? str)
    {
        return string.Equals(_str, str);
    }

    public override bool Equals(object? obj)
    {
        return obj is string text && _str == text;
    }

    public override int GetHashCode()
    {
        if (_str is null) return 0;
        return _str.GetHashCode();
    }

    public override string ToString()
    {
        return _str ?? "";
    }
}