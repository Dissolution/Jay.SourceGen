using System.Runtime.CompilerServices;

namespace Jay.SourceGen.Extensions;

public static class ArrayExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> AsSpan<T>(this T[] array, Range range)
    {
        (int offset, int length) = range.GetOffsetAndLength(array.Length);
        return new Span<T>(array, offset, length);
    }
}
