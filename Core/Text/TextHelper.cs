using System.Runtime.CompilerServices;

using static InlineIL.IL;

// ReSharper disable InvokeAsExtensionMethod
// ^ I want to be sure I'm calling the very specific version of a method

// ReSharper disable EntityNameCapturedOnly.Global

namespace Jay.SourceGen.Text;

public static class TextHelper
{
    public const string AsciiDigits = "0123456789";
    public const string AsciiLetterUppers = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public const string AsciiLetterLowers = "abcdefghijklmnopqrstuvwxyz";


    /// <summary>
    /// The offset between an uppercase ascii letter and its lowercase equivalent
    /// </summary>
    internal const int UppercaseOffset = 'a' - 'A';

    /// <summary>
    /// Unsafe / Unchecked Methods -- Nothing here has bounds checks!
    /// </summary>
    internal static class Unsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void CopyBlock(char* sourcePtr, ref char destPtr, int charCount)
        {
            Emit.Ldarg(nameof(destPtr));
            Emit.Ldarg(nameof(sourcePtr));
            Emit.Ldarg(nameof(charCount));
            Emit.Sizeof<char>();
            Emit.Mul();
            Emit.Cpblk();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CopyBlock(in char sourcePtr, ref char destPtr, int charCount)
        {
            Emit.Ldarg(nameof(destPtr));
            Emit.Ldarg(nameof(sourcePtr));
            Emit.Ldarg(nameof(charCount));
            Emit.Sizeof<char>();
            Emit.Mul();
            Emit.Cpblk();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CopyTo(ReadOnlySpan<char> source, Span<char> dest, int sourceLen)
        {
            CopyBlock(
                in source.GetPinnableReference(),
                ref dest.GetPinnableReference(),
                sourceLen);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CopyTo(string source, Span<char> dest, int sourceLen)
        {
            unsafe
            {
                fixed (char* ptr = source)
                {
                    CopyBlock(
                        ptr,
                        ref dest.GetPinnableReference(),
                        sourceLen);
                }
            }
        }
    }

    public static void CopyTo(ReadOnlySpan<char> source, Span<char> dest)
    {
        if (!TryCopyTo(source, dest))
            throw new ArgumentException($"Destination 'char[{dest.Length}]' cannot contain Source 'char[{source.Length}]'", nameof(dest));
    }

    public static void CopyTo(string? source, Span<char> dest)
    {
        if (!TryCopyTo(source, dest))
            throw new ArgumentException($"Destination 'char[{dest.Length}]' cannot contain Source 'char[{source!.Length}]'", nameof(dest));
    }

    public static bool TryCopyTo(ReadOnlySpan<char> source, Span<char> dest)
    {
        var sourceLen = source.Length;
        if (sourceLen == 0) return true;
        if (sourceLen > dest.Length) return false;
        Unsafe.CopyTo(source, dest, sourceLen);
        return true;
    }

    public static bool TryCopyTo(string? source, Span<char> dest)
    {
        if (source is null) return true;
        var sourceLen = source.Length;
        if (sourceLen == 0) return true;
        if (sourceLen > dest.Length) return false;
        Unsafe.CopyTo(source, dest, sourceLen);
        return true;
    }

    #region Equals

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<char> xSpan, ReadOnlySpan<char> ySpan)
    {
        return MemoryExtensions.SequenceEqual(xSpan, ySpan);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<char> xSpan, char[]? yChars)
    {
        return MemoryExtensions.SequenceEqual(xSpan, yChars.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<char> xSpan, string? yStr)
    {
        return MemoryExtensions.SequenceEqual(xSpan, yStr.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(char[]? xChars, ReadOnlySpan<char> ySpan)
    {
        return MemoryExtensions.SequenceEqual(xChars.AsSpan(), ySpan);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(char[]? xChars, char[]? yChars)
    {
        return MemoryExtensions.SequenceEqual<char>(xChars.AsSpan(), yChars.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(char[]? xChars, string? yStr)
    {
        return MemoryExtensions.SequenceEqual(xChars.AsSpan(), yStr.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? xStr, ReadOnlySpan<char> ySpan)
    {
        return MemoryExtensions.SequenceEqual(xStr.AsSpan(), ySpan);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? xStr, char[]? yChars)
    {
        return MemoryExtensions.SequenceEqual(xStr.AsSpan(), yChars.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? xStr, string? yStr)
    {
        return string.Equals(xStr, yStr, StringComparison.Ordinal);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<char> xSpan, ReadOnlySpan<char> ySpan, StringComparison comparison)
    {
        return MemoryExtensions.Equals(xSpan, ySpan, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<char> xSpan, char[]? yChars, StringComparison comparison)
    {
        return MemoryExtensions.Equals(xSpan, yChars.AsSpan(), comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<char> xSpan, string? yStr, StringComparison comparison)
    {
        return MemoryExtensions.Equals(xSpan, yStr.AsSpan(), comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(char[]? xChars, ReadOnlySpan<char> ySpan, StringComparison comparison)
    {
        return MemoryExtensions.Equals(xChars.AsSpan(), ySpan, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(char[]? xChars, char[]? yChars, StringComparison comparison)
    {
        return MemoryExtensions.Equals(xChars.AsSpan(), yChars.AsSpan(), comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(char[]? xChars, string? yStr, StringComparison comparison)
    {
        return MemoryExtensions.Equals(xChars.AsSpan(), yStr.AsSpan(), comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? xStr, ReadOnlySpan<char> ySpan, StringComparison comparison)
    {
        return MemoryExtensions.Equals(xStr.AsSpan(), ySpan, comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? xStr, char[]? yChars, StringComparison comparison)
    {
        return MemoryExtensions.Equals(xStr.AsSpan(), yChars.AsSpan(), comparison);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(string? xStr, string? yStr, StringComparison comparison)
    {
        return string.Equals(xStr, yStr, comparison);
    }

    #endregion
}