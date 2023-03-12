
namespace Jay.SourceGen;

public sealed class AttributeArgsCollection : IReadOnlyCollection<KeyValuePair<string, object>>
{
    public static AttributeArgsCollection Empty { get; } = new();

    private readonly Dictionary<string, object?> _args;

    public int Count => _args.Count;

    private AttributeArgsCollection()
    {
        _args = new Dictionary<string, object?>(0, StringComparer.OrdinalIgnoreCase);
    }

    public AttributeArgsCollection(AttributeData attributeData)
    {
        var args = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var ctorArgs = attributeData.ConstructorArguments;
        if (ctorArgs.Length > 0)
        {
            var ctorParams = attributeData.AttributeConstructor?.Parameters ?? ImmutableArray<IParameterSymbol>.Empty;
            Debug.Assert(ctorArgs.Length == ctorParams.Length);
            int count = ctorArgs.Length;
            for (var i = 0; i < count; i++)
            {
                string name = ctorParams[i].Name;
                Debug.Assert(args.ContainsKey(name) == false);
                object? value = ctorArgs[i].GetObjectValue();
                args[name] = value;
            }
        }

        var namedArgs = attributeData.NamedArguments;
        if (namedArgs.Length > 0)
        {
            int count = namedArgs.Length;
            for (var i = 0; i < count; i++)
            {
                var arg = namedArgs[i];
                string name = arg.Key;
                Debug.Assert(args.ContainsKey(name) == false);
                object? value = arg.Value.GetObjectValue();
                args[name] = value;
            }
        }

        _args = args;
    }

    public bool TryGetValue(
        [AllowNull, NotNullWhen(true)] string? name,
        [MaybeNullWhen(false)] out object? value)
    {
        if (string.IsNullOrEmpty(name))
        {
            value = null;
            return false;
        }
        return _args.TryGetValue(name!, out value);
    }

    public bool TryGetValue<TValue>(
        [AllowNull, NotNullWhen(true)] string? name,
        [MaybeNullWhen(false)] out TValue? value)
    {
        if (TryGetValue(name, out object? objValue))
        {
            if (objValue is TValue)
            {
                value = (TValue)objValue;
                return true;
            }
        }
        value = default;
        return false;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _args.GetEnumerator();
}