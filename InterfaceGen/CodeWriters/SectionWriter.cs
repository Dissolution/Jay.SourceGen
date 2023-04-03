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

//public interface IMemberImpl
//{
//    bool CanImpl(MemberSig memberSig);
//    void Impl(ImplementationBuilder builder, MemberSig memberSig);
//}

//public interface IPropertyImplementer
//{
//    void Write(PropertySig propertySig, CodeBuilder codeBuilder, ImplementationBuilder implBuilder);
//}

//public interface IInterfaceImpl
//{
//    bool CanImpl(TypeSig interfaceType);
//    void Impl(ImplementationBuilder builder);
//}


//public interface IMemberImplementer
//{

//}

//public interface IImplementer<TMember> : IMemberImplementer
//    where TMember : MemberSig
//{
//    void ImplMember(TMember member, CodeBuilder code, ImplementationBuilder implBuilder);
//}

//public interface IMemberWriter
//{

//}

//public interface IWriter<TMember> : IMemberWriter
//    where TMember : MemberSig
//{
//    void WriteMember(CodeBuilder code, ImplementationBuilder implBuilder);
//}


//public class ImplementationBuilder : IDisposable
//{
//    private readonly List<(MemberSig, IMemberImpl)> _members = new();

//    protected propImpl _propertyImplementer;
//    protected dynamic _eventImplementer;
    

//    public GenerateInfo GenerateInfo {get; }

//    public ImplementationBuilder(GenerateInfo generateInfo)
//    {
//        this.GenerateInfo = generateInfo;
//    }



//}