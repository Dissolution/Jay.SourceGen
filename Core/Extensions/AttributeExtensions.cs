namespace Jay.SourceGen.Extensions;

public static class AttributeExtensions
{
    public static AttributeArgsCollection GetArgs(this AttributeData attributeData)
    {
        return new AttributeArgsCollection(attributeData);
    }

    public static string? GetFullTypeName(this AttributeData attributeData)
    {
        return attributeData.AttributeClass?.GetFQN();
    }

    public static IReadOnlyList<AttributeData> GetAttributes(this AttributeListSyntax attributes, Compilation compilation)
    {
        // Collect pertinent syntax trees from these attributes
        var acceptedTrees = new HashSet<SyntaxTree>();
        foreach (var attribute in attributes.Attributes)
            acceptedTrees.Add(attribute.SyntaxTree);

        var parentSymbol = attributes.Parent!.GetDeclaredSymbol(compilation)!;
        var parentAttributes = parentSymbol.GetAttributes();
        var ret = new List<AttributeData>();
        foreach (var attribute in parentAttributes)
        {
            if (acceptedTrees.Contains(attribute.ApplicationSyntaxReference!.SyntaxTree))
                ret.Add(attribute);
        }

        return ret;
    }
    
    public static AttributeData? FindByFQN(this ImmutableArray<AttributeData> attributes, string? attributeFQN)
    {
        for (int i = 0; i < attributes.Length; i++)
        {
            string? attrFQN = attributes[i].AttributeClass?.GetFQN();
            if (string.Equals(attrFQN, attributeFQN, StringComparison.Ordinal))
                return attributes[i];
        }
        return null;
    }
}
