namespace Jay.SourceGen.InterfaceGen.CodeWriters;

public interface IInterfaceImplementationWriter
{
    bool CanImplement(INamedTypeSymbol interfaceSymbol);

    IEnumerable<string> GetNeededUsings();

    bool WriteImplementationSection(
        Instic instic,
        Visibility visibility,
        MemberType memberType,
        CodeBuilder codeBuilder,
        GenerateInfo generateInfo);
}
