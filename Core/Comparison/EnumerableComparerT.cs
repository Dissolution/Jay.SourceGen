#nullable enable
namespace Jay.SourceGen.Comparison;

public sealed class FuncEqualityComparer<T> : IEqualityComparer<T>
{
    private readonly Func<T, T, bool> _equals;
    private readonly Func<T, int> _getHashCode;

    public FuncEqualityComparer(Func<T, T, bool> equals, Func<T, int> getHashCode)
    {
        _equals = equals;
        _getHashCode = getHashCode;
    }

    public bool Equals(T? left, T? right)
    {
        if (left is null) return right is null;
        if (right is null) return false;
        return _equals(left, right);
    }

    public int GetHashCode(T? value)
    {
        if (value is null) return 0;
        return _getHashCode(value);
    }
}

public sealed class EnumerableEqualityComparer<T> :
    IEqualityComparer<IEnumerable<T>>,
    IEqualityComparer<T[]>,
    IEqualityComparer<ImmutableArray<T>>
{
    private const int HASH_SEED = 486_187_739;
    private const int HASH_MUL = 31;

    public static EnumerableEqualityComparer<T> Default { get; } = new();

    private readonly IEqualityComparer<T> _valueComparer;

    public EnumerableEqualityComparer(IEqualityComparer<T>? valueComparer = default)
    {
        _valueComparer = valueComparer ?? EqualityComparer<T>.Default;
    }

    public bool Equals(IEnumerable<T>? x, IEnumerable<T>? y)
    {
        throw new NotImplementedException();
    }

    public bool Equals(T[]? x, T[]? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        int xLen = x.Length;
        if (y.Length != xLen) return false;
        for (var i = 0; i < xLen; i++)
        {
            if (!_valueComparer.Equals(x[i], y[i])) return false;
        }
        return true;
    }

    public bool Equals(ImmutableArray<T> x, ImmutableArray<T> y)
    {
        if (x.IsDefaultOrEmpty) return y.IsDefaultOrEmpty;
        if (y.IsDefaultOrEmpty) return false;
        int xLen = x.Length;
        if (y.Length != xLen) return false;
        for (var i = 0; i < xLen; i++)
        {
            if (!_valueComparer.Equals(x[i], y[i])) return false;
        }
        return true;
    }

    public int GetHashCode(IEnumerable<T>? values)
    {
        throw new NotImplementedException();
    }

    public int GetHashCode(T[]? values)
    {
        if (values is null) return 0;
        int len = values.Length;
        int hash = HASH_SEED;
        for (var i = 0; i < len; i++)
        {
            hash = (hash*HASH_MUL) + (values[i]?.GetHashCode() ?? 0);
        }
        return hash;
    }

    public int GetHashCode(ImmutableArray<T> values)
    {
        if (values.IsDefaultOrEmpty) return 0;
        int len = values.Length;
        int hash = HASH_SEED;
        for (var i = 0; i < len; i++)
        {
            hash = (hash*HASH_MUL) + (values[i]?.GetHashCode() ?? 0);
        }
        return hash;
    }
}
