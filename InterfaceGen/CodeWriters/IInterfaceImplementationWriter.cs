using Jay.SourceGen.Text;

namespace Jay.SourceGen.InterfaceGen.CodeWriters;

public interface IInterfaceImplementationWriter
{
    bool CanImplement(INamedTypeSymbol interfaceSymbol);

    bool WriteImplementationSection(
        Instic instic,
        Visibility visibility,
        MemberType memberType,
        CodeBuilder codeBuilder,
        GenerateInfo generateInfo);
}
