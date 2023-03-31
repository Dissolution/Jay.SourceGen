using System.Runtime.CompilerServices;

namespace Jay.SourceGen.Extensions;

public static class CharExtensions
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> AsSpan(this in char ch)
    {
        unsafe
        {
            fixed (char* chPtr = &ch)
            {
                return new ReadOnlySpan<char>(chPtr, 1);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiDigit(this char ch) => (uint)(ch - '0') <= '9' - '0';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiLetterLower(this char ch) => ch is >= 'a' and <= 'z';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiLetterUpper(this char ch) => ch is >= 'A' and <= 'Z';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAscii(this char ch) => (ushort)ch < 128;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiLetter(this char ch) => (ch is >= 'a' and <= 'z') || (ch is >= 'A' and <= 'Z');
}