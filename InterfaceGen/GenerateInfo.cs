using System.Collections.Immutable;
using System.Reflection;

namespace Jay.SourceGen.InterfaceGen;

public sealed class GenerateInfo
{
    public string? Name { get; set; } = null;
    public Visibility Visibility { get; set; } = Visibility.Public;
    public ObjType ObjType { get; set; } = ObjType.Class;
    public MemberKeywords MemberKeywords { get; set; } = MemberKeywords.Sealed;
    public INamedTypeSymbol TypeSymbol { get; set; }
    public ImmutableArray<INamedTypeSymbol> Interfaces { get; set; }
    public List<MemberSig> Members { get; } = new();

    public GenerateInfo()
    {

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
            if (!member.Type.HasFlag(memberType)) continue;
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
}
