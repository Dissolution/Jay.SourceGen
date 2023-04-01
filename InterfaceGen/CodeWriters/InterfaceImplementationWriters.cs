namespace Jay.SourceGen.InterfaceGen.CodeWriters;

public static class InterfaceImplementationWriters
{
    private static readonly List<IInterfaceImplementationWriter> _writers;

    static InterfaceImplementationWriters()
    {
        _writers = new()
        {
            new FormattableWriter(),
            new EquatableWriter(),
            new ComparableWriter(),
        };
    }

    public static IInterfaceImplementationWriter? GetWriter(INamedTypeSymbol interfaceSymbol)
    {
        foreach (var writer in _writers)
        {
            if (writer.CanImplement(interfaceSymbol))
                return writer;
        }
        return null;
    }
}
