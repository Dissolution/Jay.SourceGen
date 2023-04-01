using System.Collections.Immutable;

namespace Jay.SourceGen.InterfaceGen;

public sealed class GenerateInfo
{
    public INamedTypeSymbol InterfaceTypeSymbol { get; }
    
    public required string ImplementationTypeName { get; init; }


    public Visibility Visibility { get; set; } = Visibility.Public;
    public ObjType ObjType { get; set; } = ObjType.Class;
    public MemberKeywords MemberKeywords { get; set; } = MemberKeywords.Sealed;

    public ImmutableArray<INamedTypeSymbol> Interfaces { get; set; }
    public HashSet<MemberSig> Members { get; } = new();

    public GenerateInfo(INamedTypeSymbol interfaceTypeSymbol)
    {
        this.InterfaceTypeSymbol = interfaceTypeSymbol;
    }

    public void GetLocals(
        out string implementationTypeName, 
        out string implementationVariableName)
    {
        implementationTypeName = this.ImplementationTypeName;
        implementationVariableName = this.ImplementationTypeName.ToVariableName();
    }

    public bool HasInterface<TInterface>()
        where TInterface : class
    {
        return this.Interfaces.Any(isym => isym.GetFQN() == typeof(TInterface).FullName);
    }

    public bool HasMember(
        Instic instic,
        Visibility visibility,
        MemberType memberType,
        string? name = null,
        Func<ITypeSymbol, bool>? returnType = null,
        Func<ImmutableArray<IParameterSymbol>, bool>? paramTypes = null)
    {
        foreach (var member in Members)
        {
            if (!member.Instic.HasFlag(instic)) continue;
            if (!member.Visibility.HasFlag(visibility)) continue;
            if (!member.MemberType.HasFlag(memberType)) continue;
            if (!string.IsNullOrWhiteSpace(name))
            {
                if (!string.Equals(name, member.Name))
                    continue;
            }
            if (returnType != null)
            {
                if (!returnType(member.ReturnType)) continue;
            }
            if (paramTypes != null)
            {
                if (!paramTypes(member.ParamTypes)) continue;
            }
            return true;
        }
        return false;
    }

    public IEnumerable<MemberSig> MembersWithAttribute(string attributeFQN)
    {
        return this.Members.Where(m => m.Attributes.Any(attr => attr.AttributeClass?.GetFQN() == attributeFQN));
    }
}
