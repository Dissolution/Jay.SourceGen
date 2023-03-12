using System.Runtime.CompilerServices;

namespace Jay.SourceGen.Extensions;

public static class NumberExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Clamp(this int value, int minimum, int maximum)
    {
        if (value < minimum)
            return minimum;
        if (value > maximum)
            return maximum;
        return value;
    }
}