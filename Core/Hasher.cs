using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Jay.SourceGen;


public partial struct Hasher
{
    private static readonly uint s_seed = GenerateGlobalSeed();

    private const uint Prime1 = 2_654_435_761U;
    private const uint Prime2 = 2_246_822_519U;
    private const uint Prime3 = 3_266_489_917U;
    private const uint Prime4 = 0_668_265_263U;
    private const uint Prime5 = 0_374_761_393U;

    internal static int EmptyHashCode;

    static Hasher()
    {
        var hasher = new Hasher();
        EmptyHashCode = hasher.ToHashCode();
    }

    private static unsafe uint GenerateGlobalSeed()
    {
        using (var rng = new RNGCryptoServiceProvider())
        {
            byte[] buffer = new byte[sizeof(uint)];
            rng.GetBytes(buffer);
            return BitConverter.ToUInt32(buffer, 0);
        }
    }

    /// <summary>
    /// Rotates the specified value left by the specified number of bits.
    /// Similar in behavior to the x86 instruction ROL.
    /// </summary>
    /// <param name="value">The value to rotate.</param>
    /// <param name="offset">The number of bits to rotate by.
    /// Any value outside the range [0..31] is treated as congruent mod 32.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint RotateLeft(uint value, int offset)
        => (value << offset) | (value >> (32 - offset));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Initialize(out uint v1, out uint v2, out uint v3, out uint v4)
    {
        v1 = s_seed + Prime1 + Prime2;
        v2 = s_seed + Prime2;
        v3 = s_seed;
        v4 = s_seed - Prime1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Round(uint hash, uint input)
    {
        return RotateLeft(hash + input * Prime2, 13) * Prime1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint QueueRound(uint hash, uint queuedValue)
    {
        return RotateLeft(hash + queuedValue * Prime3, 17) * Prime4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint MixState(uint v1, uint v2, uint v3, uint v4)
    {
        return RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
    }

    private static uint MixEmptyState()
    {
        return s_seed + Prime5;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint MixFinal(uint hash)
    {
        hash ^= hash >> 15;
        hash *= Prime2;
        hash ^= hash >> 13;
        hash *= Prime3;
        hash ^= hash >> 16;
        return hash;
    }


    public static int Create<T>(T? value)
    {
        uint hc1 = (uint)(value?.GetHashCode() ?? 0);
        uint hash = MixEmptyState();
        hash += 4;
        hash = QueueRound(hash, hc1);
        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Create<T1, T2>(T1? value1, T2? value2)
    {
        uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
        uint hc2 = (uint)(value2?.GetHashCode() ?? 0);

        uint hash = MixEmptyState();
        hash += 8;

        hash = QueueRound(hash, hc1);
        hash = QueueRound(hash, hc2);

        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Create<T1, T2, T3>(T1? value1, T2? value2, T3? value3)
    {
        uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
        uint hc2 = (uint)(value2?.GetHashCode() ?? 0);
        uint hc3 = (uint)(value3?.GetHashCode() ?? 0);

        uint hash = MixEmptyState();
        hash += 12;

        hash = QueueRound(hash, hc1);
        hash = QueueRound(hash, hc2);
        hash = QueueRound(hash, hc3);

        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Create<T1, T2, T3, T4>(T1? value1, T2? value2, T3? value3, T4? value4)
    {
        uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
        uint hc2 = (uint)(value2?.GetHashCode() ?? 0);
        uint hc3 = (uint)(value3?.GetHashCode() ?? 0);
        uint hc4 = (uint)(value4?.GetHashCode() ?? 0);

        Initialize(out uint v1, out uint v2, out uint v3, out uint v4);

        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        uint hash = MixState(v1, v2, v3, v4);
        hash += 16;

        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Create<T1, T2, T3, T4, T5>(T1? value1, T2? value2, T3? value3, T4? value4, T5? value5)
    {
        uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
        uint hc2 = (uint)(value2?.GetHashCode() ?? 0);
        uint hc3 = (uint)(value3?.GetHashCode() ?? 0);
        uint hc4 = (uint)(value4?.GetHashCode() ?? 0);
        uint hc5 = (uint)(value5?.GetHashCode() ?? 0);

        Initialize(out uint v1, out uint v2, out uint v3, out uint v4);

        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        uint hash = MixState(v1, v2, v3, v4);
        hash += 20;

        hash = QueueRound(hash, hc5);

        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Create<T1, T2, T3, T4, T5, T6>(T1? value1, T2? value2, T3? value3, T4? value4, T5? value5, T6? value6)
    {
        uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
        uint hc2 = (uint)(value2?.GetHashCode() ?? 0);
        uint hc3 = (uint)(value3?.GetHashCode() ?? 0);
        uint hc4 = (uint)(value4?.GetHashCode() ?? 0);
        uint hc5 = (uint)(value5?.GetHashCode() ?? 0);
        uint hc6 = (uint)(value6?.GetHashCode() ?? 0);

        Initialize(out uint v1, out uint v2, out uint v3, out uint v4);

        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        uint hash = MixState(v1, v2, v3, v4);
        hash += 24;

        hash = QueueRound(hash, hc5);
        hash = QueueRound(hash, hc6);

        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Create<T1, T2, T3, T4, T5, T6, T7>(T1? value1, T2? value2, T3? value3, T4? value4, T5? value5, T6? value6, T7? value7)
    {
        uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
        uint hc2 = (uint)(value2?.GetHashCode() ?? 0);
        uint hc3 = (uint)(value3?.GetHashCode() ?? 0);
        uint hc4 = (uint)(value4?.GetHashCode() ?? 0);
        uint hc5 = (uint)(value5?.GetHashCode() ?? 0);
        uint hc6 = (uint)(value6?.GetHashCode() ?? 0);
        uint hc7 = (uint)(value7?.GetHashCode() ?? 0);

        Initialize(out uint v1, out uint v2, out uint v3, out uint v4);

        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        uint hash = MixState(v1, v2, v3, v4);
        hash += 28;

        hash = QueueRound(hash, hc5);
        hash = QueueRound(hash, hc6);
        hash = QueueRound(hash, hc7);

        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Create<T1, T2, T3, T4, T5, T6, T7, T8>(T1? value1, T2? value2, T3? value3, T4? value4, T5? value5, T6? value6, T7? value7, T8? value8)
    {
        uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
        uint hc2 = (uint)(value2?.GetHashCode() ?? 0);
        uint hc3 = (uint)(value3?.GetHashCode() ?? 0);
        uint hc4 = (uint)(value4?.GetHashCode() ?? 0);
        uint hc5 = (uint)(value5?.GetHashCode() ?? 0);
        uint hc6 = (uint)(value6?.GetHashCode() ?? 0);
        uint hc7 = (uint)(value7?.GetHashCode() ?? 0);
        uint hc8 = (uint)(value8?.GetHashCode() ?? 0);

        Initialize(out uint v1, out uint v2, out uint v3, out uint v4);

        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        v1 = Round(v1, hc5);
        v2 = Round(v2, hc6);
        v3 = Round(v3, hc7);
        v4 = Round(v4, hc8);

        uint hash = MixState(v1, v2, v3, v4);
        hash += 32;

        hash = MixFinal(hash);
        return (int)hash;
    }

    public static int Create<T>(params T?[]? values)
    {
        if (values is null) return 0;
        int len = values.Length;
        switch (len)
        {
            case 0:
                return EmptyHashCode;
            case 1:
                return Create(values[0]);
            case 2:
                return Create(values[0], values[1]);
            case 3:
                return Create(values[0], values[1], values[2]);
            case 4:
                return Create(values[0], values[1], values[2], values[3]);
            case 5:
                return Create(values[0], values[1], values[2], values[3], values[4]);
            case 6:
                return Create(values[0], values[1], values[2], values[3], values[4], values[5]);
            case 7:
                return Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6]);
            case 8:
                return Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);
            default:
            {
                var hasher = new Hasher();
                for (var i = 0; i < len; i++)
                {
                    hasher.Add<T>(values[i]);
                }
                return hasher.ToHashCode();
            }
        }
    }

    public static int Create<T>(T[]? values, IEqualityComparer<T>? valueComparer)
    {
        if (values is null) return 0;
        var hasher = new Hasher();
        for (var i = 0; i < values.Length; i++)
        {
            hasher.Add<T>(values[i], valueComparer);
        }
        return hasher.ToHashCode();
    }

    public static int Create<T>(ReadOnlySpan<T> values)
    {
        int len = values.Length;
        switch (len)
        {
            case 0:
                return EmptyHashCode;
            case 1:
                return Create(values[0]);
            case 2:
                return Create(values[0], values[1]);
            case 3:
                return Create(values[0], values[1], values[2]);
            case 4:
                return Create(values[0], values[1], values[2], values[3]);
            case 5:
                return Create(values[0], values[1], values[2], values[3], values[4]);
            case 6:
                return Create(values[0], values[1], values[2], values[3], values[4], values[5]);
            case 7:
                return Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6]);
            case 8:
                return Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);
            default:
            {
                var hasher = new Hasher();
                for (var i = 0; i < len; i++)
                {
                    hasher.Add<T>(values[i]);
                }
                return hasher.ToHashCode();
            }
        }
    }

    public static int Create<T>(ReadOnlySpan<T> values, IEqualityComparer<T>? valueComparer)
    {
        int len = values.Length;
        var hasher = new Hasher();
        for (var i = 0; i < len; i++)
        {
            hasher.Add<T>(values[i], valueComparer);
        }
        return hasher.ToHashCode();
    }

    public static int Create<T>(IEnumerable<T>? values)
    {
        if (values is null) return 0;
        if (values is IList<T> list)
        {
            var hasher = new Hasher();
            for (var i = 0; i < list.Count; i++)
            {
                hasher.Add<T>(list[i]);
            }
            return hasher.ToHashCode();
        }
        else
        {
            var hasher = new Hasher();
            foreach (var value in values)
            {
                hasher.Add<T>(value);
            }
            return hasher.ToHashCode();
        }
    }

    public static int Create<T>(IEnumerable<T>? values, IEqualityComparer<T>? valueComparer)
    {
        if (values is null) return 0;
        if (values is IList<T> list)
        {
            var hasher = new Hasher();
            for (var i = 0; i < list.Count; i++)
            {
                hasher.Add<T>(list[i], valueComparer);
            }
            return hasher.ToHashCode();
        }
        else
        {
            var hasher = new Hasher();
            foreach (var value in values)
            {
                hasher.Add<T>(value, valueComparer);
            }
            return hasher.ToHashCode();
        }
    }
}

public partial struct Hasher
{
    private uint _v1, _v2, _v3, _v4;
    private uint _queue1, _queue2, _queue3;
    private uint _length;

    private void AddHashCode(int value)
    {
        uint val = (uint)value;

        // Storing the value of _length locally shaves of quite a few bytes
        // in the resulting machine code.
        uint previousLength = _length++;
        uint position = previousLength % 4;

        // Switch can't be inlined.

        if (position == 0)
            _queue1 = val;
        else if (position == 1)
            _queue2 = val;
        else if (position == 2)
            _queue3 = val;
        else // position == 3
        {
            if (previousLength == 3)
                Initialize(out _v1, out _v2, out _v3, out _v4);

            _v1 = Round(_v1, _queue1);
            _v2 = Round(_v2, _queue2);
            _v3 = Round(_v3, _queue3);
            _v4 = Round(_v4, val);
        }
    }

    public void Add<T>(T? value)
    {
        AddHashCode(value?.GetHashCode() ?? 0);
    }

    public void Add<T>(T value, IEqualityComparer<T>? comparer)
    {
        AddHashCode(value is null ? 0 : (comparer?.GetHashCode(value) ?? value.GetHashCode()));
    }

     public void AddAll<T>(T[]? values)
    {
        if (values is not null)
        {
            for (int i = 0; i < values.Length; i++)
            {
                this.Add<T>(values[i]);
            }
        }
    }

    public void AddAll<T>(IEnumerable<T>? values)
    {
        if (values is not null)
        {
            foreach (var value in values)
            {
                this.Add<T>(value);
            }
        }
    }

    public int ToHashCode()
    {
        // Storing the value of _length locally shaves of quite a few bytes
        // in the resulting machine code.
        uint length = _length;

        // position refers to the *next* queue position in this method, so
        // position == 1 means that _queue1 is populated; _queue2 would have
        // been populated on the next call to Add.
        uint position = length % 4;

        // If the length is less than 4, _v1 to _v4 don't contain anything
        // yet. xxHash32 treats this differently.

        uint hash = length < 4 ? MixEmptyState() : MixState(_v1, _v2, _v3, _v4);

        // _length is incremented once per Add(Int32) and is therefore 4
        // times too small (xxHash length is in bytes, not ints).

        hash += length * 4;

        // Mix what remains in the queue

        // Switch can't be inlined right now, so use as few branches as
        // possible by manually excluding impossible scenarios (position > 1
        // is always false if position is not > 0).
        if (position > 0)
        {
            hash = QueueRound(hash, _queue1);
            if (position > 1)
            {
                hash = QueueRound(hash, _queue2);
                if (position > 2)
                    hash = QueueRound(hash, _queue3);
            }
        }

        hash = MixFinal(hash);
        return (int)hash;
    }

#pragma warning disable 0809
    [Obsolete("HashCode is a mutable struct and should not be compared with other HashCodes. Use ToHashCode to retrieve the computed hash code.", error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode() => throw new NotSupportedException(SR.HashCode_HashCodeNotSupported);

    [Obsolete("HashCode is a mutable struct and should not be compared with other HashCodes.", error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool Equals(object? obj) => throw new NotSupportedException(SR.HashCode_EqualityNotSupported);
#pragma warning restore 0809
}