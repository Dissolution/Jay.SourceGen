
namespace Jay.SourceGen;

public struct SymbolAttributeData
{
    public static SymbolAttributeData Empty { get; } = new();

    private readonly ImmutableArray<AttributeData> _attributeData;

    public SymbolAttributeData(ImmutableArray<AttributeData> attributeData)
    {
        _attributeData = attributeData;
    }

    public bool HasAttribute(string attributeFQN)
    {
        return _attributeData.Any(attr =>
            {
                return string.Equals(attr.AttributeClass?.GetFQN(), attributeFQN);
            });
    }

    public bool TryGetAttributeData(string attributeFQN, [NotNullWhen(true)] out AttributeData? attributeData)
    {
        foreach (var attrData in _attributeData)
        {
            if (string.Equals(attrData.AttributeClass?.GetFQN(), attributeFQN))
            {
                attributeData = attrData;
                return true;
            }
        }
        attributeData = default;
        return false;
    }

     public bool TryGetAttributeArg(string attributeFQN, [NotNullWhen(true)] out AttributeArgsCollection? attributeArgs)
    {
        foreach (var attrData in _attributeData)
        {
            if (string.Equals(attrData.AttributeClass?.GetFQN(), attributeFQN))
            {
                attributeArgs = new(attrData);
                return true;
            }
        }
        attributeArgs = default;
        return false;
    }
}
