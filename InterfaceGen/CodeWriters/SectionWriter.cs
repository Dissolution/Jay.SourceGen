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

public interface IMemberImpl
{
    bool CanImpl(MemberSig memberSig);
    void Impl(ImplementationBuilder builder, MemberSig memberSig);
}

public interface IPropertyImplementer
{
    void Write(PropertySig propertySig, CodeBuilder codeBuilder, ImplementationBuilder implBuilder);
}

public interface IInterfaceImpl
{
    bool CanImpl(TypeSig interfaceType);
    void Impl(ImplementationBuilder builder);
}


public class ImplementationBuilder
{
    private readonly List<MemberSig> _members = new();

    public GenerateInfo GenerateInfo {get; }

    public ImplementationBuilder()
    {
        
    }


    public bool TryAddMember(MemberSig memberSig)
    {
        if (_members.Contains(memberSig)) return false;
        _members.Add(memberSig);
        return true;
    }

}