using System;
using System.Collections.Generic;
using System.Text;

namespace Jay.SourceGen.Enums;

public static class EnumInfo<TEnum>
    where TEnum : struct, Enum
{
    private static readonly TEnum[] _flags;

    static EnumInfo()
    {
        _flags = (TEnum[])Enum.GetValues(typeof(TEnum));
    }

    public static bool IsDefault(TEnum @enum)
    {
        return EqualityComparer<TEnum>.Default.Equals(@enum, default);
    }

    public static IEnumerable<TEnum> GetFlags(TEnum @enum)
    {
        var flags = _flags;
        var len = flags.Length;
        for (var i = 0; i < len; i++)
        {
            var flag = flags[i];
            if (IsDefault(flag)) continue;
            if (@enum.HasFlag(flag))
                yield return flag;
        }
    }
}

public static class EnumExtensions
{
    public static IEnumerable<TEnum> GetFlags<TEnum>(this TEnum @enum)
        where TEnum : struct, Enum
    {
        return EnumInfo<TEnum>.GetFlags(@enum);
    }
}
