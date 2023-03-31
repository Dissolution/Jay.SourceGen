namespace Jay.SourceGen.Extensions;

public static class TypeSymbolExtensions
{
    public static string? GetNamespace(this ITypeSymbol typeSymbol)
    {
        var nsSymbol = typeSymbol.ContainingNamespace;
        string? nameSpace;
        if (nsSymbol.IsGlobalNamespace)
        {
            //nameSpace = nsSymbol.ContainingModule.ContainingSymbol.Name;
            nameSpace = null;
        }
        else
        {
            nameSpace = nsSymbol.Name;
        }
        return nameSpace;
    }

    public static string GetFQN(this ITypeSymbol typeSymbol)
    {
        var symbolDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        return typeSymbol.ToDisplayString(symbolDisplayFormat);
    }
    public static string GetFQNamespace(this ITypeSymbol typeSymbol)
    {
        var nsSymbol = typeSymbol.ContainingNamespace;
        var ns = nsSymbol.ToString();
        return ns;
    }

    public static bool CanBeNull(this ITypeSymbol typeSymbol)
    {
        return !typeSymbol.IsValueType;
    }

    public static bool HasInterface<TInterface>(this ITypeSymbol type)
       where TInterface : class
    {
        var interfaceType = typeof(TInterface);
        if (!interfaceType.IsInterface)
            throw new ArgumentException("The generic type must be an Interface type", nameof(TInterface));
        var interfaceFQN = interfaceType.FullName;

        return type.AllInterfaces
            .Any(ti => ti.GetFQN() == interfaceFQN);
    }
}
