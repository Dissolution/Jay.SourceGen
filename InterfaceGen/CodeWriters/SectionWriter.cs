namespace Jay.SourceGen.InterfaceGen.CodeWriters;

public abstract class SectionWriter : IInterfaceImplementationWriter
{
    private readonly Dictionary<(Instic, Visibility, MemberType), CBA<GenerateInfo>> _sectionActions = new();

    public virtual IEnumerable<string> GetNeededUsings()
    {
        yield break;
    }

    protected SectionWriter()
    {

    }

    protected void AddSectionWrite(Instic instic, Visibility visibility, MemberType memberType, CBA<GenerateInfo> sectionWrite)
    {
        _sectionActions[(instic, visibility, memberType)] = sectionWrite;
    }

    public abstract bool CanImplement(INamedTypeSymbol interfaceSymbol);

    public virtual bool WriteImplementationSection(Instic instic, Visibility visibility, MemberType memberType, CodeBuilder codeBuilder, GenerateInfo generateInfo)
    {
        if (_sectionActions.TryGetValue((instic, visibility, memberType), out CBA<GenerateInfo>? value))
        {
            value?.Invoke(codeBuilder, generateInfo);
            return true;
        }
        return false;
    }
}
