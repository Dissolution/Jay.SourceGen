using System.Diagnostics.CodeAnalysis;
#if NET7_0_OR_GREATER
using System.Numerics;
using System.Reflection;
#endif

namespace Jay.SourceGen.InterfaceGen.Attributes;



// Requires a 'readonly struct' output!

public interface IEnum<TSelf> :
    IEquatable<TSelf>, IComparable<TSelf>, IFormattable
#if NET6_0_OR_GREATER
    , ISpanFormattable
#endif
#if NET7_0_OR_GREATER
    , IEqualityOperators<TSelf, TSelf, bool>
    , IComparisonOperators<TSelf, TSelf, bool>
    , IParsable<TSelf>, ISpanParsable<TSelf>
#endif
    where TSelf : struct, IEnum<TSelf>
{
#if NET7_0_OR_GREATER
    static abstract explicit operator TSelf(ulong enumValue);
    static abstract explicit operator ulong(TSelf value);

    static abstract IReadOnlyList<TSelf> Members { get; }

    static abstract bool TryParse(string? str, [NotNullWhen(true)] out TSelf? varName);

#endif
}


public readonly partial struct ImplementedEnum :
    IEqualityOperators<ImplementedEnum, ImplementedEnum, bool>
    , IComparisonOperators<ImplementedEnum, ImplementedEnum, bool>
        , IParsable<ImplementedEnum>, ISpanParsable<ImplementedEnum>
{
    public static bool operator ==(ImplementedEnum left, ImplementedEnum right) => left.__enumValue == right.__enumValue;
    public static bool operator !=(ImplementedEnum left, ImplementedEnum right) => left.__enumValue != right.__enumValue;

    public static bool operator <(ImplementedEnum left, ImplementedEnum right) => left.__enumValue < right.__enumValue;
    public static bool operator >(ImplementedEnum left, ImplementedEnum right) => left.__enumValue > right.__enumValue;
    public static bool operator <=(ImplementedEnum left, ImplementedEnum right) => left.__enumValue <= right.__enumValue;
    public static bool operator >=(ImplementedEnum left, ImplementedEnum right) => left.__enumValue >= right.__enumValue;

    /// <summary>
    /// These are the 
    /// </summary>
    private static readonly ImplementedEnum[] _members;

    public static ImplementedEnum Parse(ReadOnlySpan<char> text, IFormatProvider? provider = default)
    {
        throw new NotImplementedException();
    }

    public static ImplementedEnum Parse([NotNull] string? str, IFormatProvider? provider = default)
    {
        throw new NotImplementedException();
    }

    static bool ISpanParsable<ImplementedEnum>.TryParse(ReadOnlySpan<char> text, IFormatProvider? _, out ImplementedEnum implementedEnum) => TryParse(text, out implementedEnum);
    public static bool TryParse(ReadOnlySpan<char> text, [MaybeNullWhen(false)] out ImplementedEnum implementedEnum)
    {
        throw new NotImplementedException();
    }

    static bool IParsable<ImplementedEnum>.TryParse([NotNullWhen(true)] string? str, IFormatProvider? _, out ImplementedEnum implementedEnum) => TryParse(str, out implementedEnum);
    public static bool TryParse([NotNullWhen(true)] string? str, [MaybeNullWhen(false)] out ImplementedEnum implementedEnum)
    {
        throw new NotImplementedException();
    }




    
}


public readonly partial struct ImplementedEnum :
    IEquatable<ImplementedEnum>
    , IComparable<ImplementedEnum>
    , IFormattable
    , ISpanFormattable

{
    private readonly ulong __enumValue;


    public readonly string Name;

    public int CompareTo(ImplementedEnum implementedEnum)
    {
        return this.__enumValue.CompareTo(implementedEnum.__enumValue);
    }

    public bool Equals(ImplementedEnum implementedEnum)
    {
        return this.__enumValue == implementedEnum.__enumValue;
    }

    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        throw new NotImplementedException();
    }

    public string ToString(string? format, IFormatProvider? formatProvider = default)
    {
        switch (format)
        {
            case "G" or "g" or "D" or "d" or "X" or "x" or "F" or "f":
            {
                // These are what Enum supports
                throw new NotImplementedException();
            }
            default:
            {
                return this.Name;
            }
        }
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is ImplementedEnum implementedEnum) return this.Equals(implementedEnum);
        if (obj is ulong value) return this.__enumValue == value;
        return false;
    }

    public override int GetHashCode()
    {
        return __enumValue.GetHashCode();
    }

    public override string ToString()
    {
        return this.Name;
    }

}